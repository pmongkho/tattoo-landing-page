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
        if (!_options.Enabled)
        {
            logger.LogInformation("Quo notifications disabled. Skipping consultation {ConsultationId}.", consultation.Id);
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey) || string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            logger.LogWarning("Quo enabled but BaseUrl/ApiKey missing. ConsultationId={ConsultationId}", consultation.Id);
            return;
        }
        
        var endpoint = NormalizeSmsPath(_options.SmsPath);
        var payload = new Dictionary<string, object?>
        {
            ["content"] = BuildMessage(consultation),
            ["to"] = new[] { consultation.PhoneNumber }
        };

        if (!string.IsNullOrWhiteSpace(_options.From))
        {
            payload["from"] = _options.From;
        }

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
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("Quo SMS failed. Status={StatusCode} ConsultationId={ConsultationId} Body={Body}", (int)response.StatusCode, consultation.Id, body);
                return;
            }

            logger.LogInformation("Quo SMS sent for consultation {ConsultationId}.", consultation.Id);
        }
        catch (HttpRequestException ex) when (ex.InnerException is AuthenticationException)
        {
            logger.LogWarning(ex,
                "Quo SMS TLS handshake failed for consultation {ConsultationId}. Verify QuoApi:BaseUrl uses a TLS 1.2+ HTTPS endpoint.",
                consultation.Id);
        }
    }

    private string BuildMessage(Consultation consultation)
    {
        if (!string.IsNullOrWhiteSpace(_options.MessageTemplate))
        {
            return _options.MessageTemplate
                .Replace("{name}", consultation.Name, StringComparison.OrdinalIgnoreCase)
                .Replace("{timeline}", consultation.Timeline, StringComparison.OrdinalIgnoreCase);
        }

        return "Hey! I'm one of Wo Hu's booking managers, thanks for reaching out!\n\nWhat were you looking to get done with him?";
    }

    private static string NormalizeSmsPath(string? smsPath)
    {
        var value = string.IsNullOrWhiteSpace(smsPath) ? "messages" : smsPath.Trim();
        return value.TrimStart('/');
    }
}
