using System.Text;
using System.Text.Json;
using System.Security.Authentication;
using dotnet_server._Models;
using Microsoft.Extensions.Options;

namespace dotnet_server._Integrations;

public class QuoLeadMessagingClient(HttpClient httpClient, ILogger<QuoLeadMessagingClient> logger, IOptions<QuoApiOptions> options)
    : IQuoLeadMessagingClient
{
    private readonly QuoApiOptions _options = options.Value;

    public async Task NotifyNewLeadAsync(Consultation consultation, CancellationToken cancellationToken)
    {
        await SendNewLeadAsync(consultation, cancellationToken);
    }

    public async Task<QuoMessageDispatchResult> SendNewLeadAsync(Consultation consultation, CancellationToken cancellationToken)
    {
        var endpoint = NormalizeSmsPath(_options.SmsPath);
        var baseUrl = httpClient.BaseAddress?.ToString() ?? _options.BaseUrl;

        if (!_options.Enabled)
        {
            logger.LogInformation("Quo notifications disabled. Skipping consultation {ConsultationId}.", consultation.Id);
            return new QuoMessageDispatchResult(false, null, null, "Quo notifications disabled.", endpoint, baseUrl);
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey) || string.IsNullOrWhiteSpace(_options.BaseUrl) || string.IsNullOrWhiteSpace(_options.From))
        {
            logger.LogWarning("Quo enabled but BaseUrl/ApiKey/From missing. ConsultationId={ConsultationId}", consultation.Id);
            return new QuoMessageDispatchResult(false, null, null, "Quo configuration is missing BaseUrl, ApiKey, or From.", endpoint, baseUrl);
        }

        var payload = new Dictionary<string, object?>
        {
            ["content"] = BuildMessage(consultation),
            ["to"] = new[] { consultation.PhoneNumber }
        };

        payload["from"] = _options.From!.Trim();

        if (!string.IsNullOrWhiteSpace(_options.UserId))
        {
            payload["userId"] = _options.UserId;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        var trimmedApiKey = _options.ApiKey.Trim();
        request.Headers.TryAddWithoutValidation("Authorization", trimmedApiKey);

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Quo SMS failed. Status={StatusCode} ConsultationId={ConsultationId} Body={Body}", (int)response.StatusCode, consultation.Id, body);
                return new QuoMessageDispatchResult(false, (int)response.StatusCode, body, null, endpoint, baseUrl);
            }

            logger.LogInformation("Quo SMS sent for consultation {ConsultationId}. Status={StatusCode} Body={Body}", consultation.Id, (int)response.StatusCode, body);
            return new QuoMessageDispatchResult(true, (int)response.StatusCode, body, null, endpoint, baseUrl);
        }
        catch (HttpRequestException ex) when (ex.InnerException is AuthenticationException)
        {
            var error = BuildDetailedError(ex);
            logger.LogWarning(ex,
                "Quo SMS TLS handshake failed for consultation {ConsultationId}. Verify QuoApi:BaseUrl uses a TLS 1.2+ HTTPS endpoint.",
                consultation.Id);

            return new QuoMessageDispatchResult(false, null, null, error, endpoint, baseUrl);
        }
        catch (HttpRequestException ex)
        {
            var error = BuildDetailedError(ex);
            logger.LogWarning(ex, "Quo SMS request failed for consultation {ConsultationId}.", consultation.Id);
            return new QuoMessageDispatchResult(false, null, null, error, endpoint, baseUrl);
        }
    }

    private static string BuildDetailedError(HttpRequestException ex)
    {
        return ex.InnerException is null
            ? ex.Message
            : $"{ex.Message} | Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}";
    }

    private string BuildMessage(Consultation consultation)
    {
        if (!string.IsNullOrWhiteSpace(_options.MessageTemplate))
        {
            return _options.MessageTemplate
                .Replace("{name}", consultation.Name, StringComparison.OrdinalIgnoreCase)
                .Replace("{timeline}", consultation.Timeline, StringComparison.OrdinalIgnoreCase);
        }

        return "Hey! I'm one of Wo Hu's booking managers, thanks for reaching out!\n\nWhat were you interested in getting done? :)";
    }

    private static string NormalizeSmsPath(string? smsPath)
    {
        var value = string.IsNullOrWhiteSpace(smsPath) ? "messages" : smsPath.Trim();
        return value.TrimStart('/');
    }
}
