using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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

        var endpoint = string.IsNullOrWhiteSpace(_options.SmsPath) ? "/messages" : _options.SmsPath;
        var payload = new
        {
            to = consultation.PhoneNumber,
            message = BuildMessage(consultation),
            metadata = new
            {
                source = "tattoo-landing-page",
                consultationId = consultation.Id,
                consultation.Name,
                consultation.Timeline
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("Quo SMS failed. Status={StatusCode} ConsultationId={ConsultationId} Body={Body}", (int)response.StatusCode, consultation.Id, body);
            return;
        }

        logger.LogInformation("Quo SMS sent for consultation {ConsultationId}.", consultation.Id);
    }

    private string BuildMessage(Consultation consultation)
    {
        if (!string.IsNullOrWhiteSpace(_options.MessageTemplate))
        {
            return _options.MessageTemplate
                .Replace("{name}", consultation.Name, StringComparison.OrdinalIgnoreCase)
                .Replace("{timeline}", consultation.Timeline, StringComparison.OrdinalIgnoreCase);
        }

        var businessName = string.IsNullOrWhiteSpace(_options.BusinessName) ? "Afterlife Tattoo" : _options.BusinessName;
        return $"Hi {consultation.Name}, thanks for submitting your consultation to {businessName}. We'll text you soon to confirm details.";
    }
}
