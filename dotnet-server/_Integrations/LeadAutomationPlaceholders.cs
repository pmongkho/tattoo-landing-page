using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using dotnet_server._Models;

namespace dotnet_server._Integrations;

public interface IQuoLeadMessagingClient
{
    Task NotifyNewLeadAsync(Consultation consultation, CancellationToken cancellationToken);
}

public interface ISquareBookingClient
{
    Task PrepareBookingWorkflowAsync(Consultation consultation, CancellationToken cancellationToken);
}

public class SquareBookingPlaceholder(
    HttpClient httpClient,
    ILogger<SquareBookingPlaceholder> logger,
    IOptions<SquareApiOptions> options)
    : ISquareBookingClient
{
    private readonly SquareApiOptions _options = options.Value;

    public async Task PrepareBookingWorkflowAsync(Consultation consultation, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.AccessToken)
            || string.IsNullOrWhiteSpace(_options.LocationId)
            || string.IsNullOrWhiteSpace(_options.CustomerId)
            || string.IsNullOrWhiteSpace(_options.TeamMemberId)
            || string.IsNullOrWhiteSpace(_options.ServiceVariationId))
        {
            logger.LogWarning("Square enabled but required settings are missing. ConsultationId={ConsultationId}", consultation.Id);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var bookingPayload = new
        {
            idempotency_key = Guid.NewGuid().ToString(),
            booking = new
            {
                customer_id = _options.CustomerId,
                location_id = _options.LocationId,
                location_type = "BUSINESS_LOCATION",
                start_at = now.AddDays(1).ToString("O"),
                appointment_segments = new[]
                {
                    new
                    {
                        team_member_id = _options.TeamMemberId,
                        duration_minutes = _options.AppointmentDurationMinutes,
                        service_variation_id = _options.ServiceVariationId
                    }
                }
            }
        };

        var bookingOk = await PostToSquareAsync("/v2/bookings", bookingPayload, cancellationToken);
        if (!bookingOk)
        {
            return;
        }

        var invoicePayload = new
        {
            idempotency_key = Guid.NewGuid().ToString(),
            invoice = new
            {
                delivery_method = "SMS",
                description = "deposit",
                location_id = _options.LocationId,
                payment_requests = new[]
                {
                    new
                    {
                        fixed_amount_requested_money = new
                        {
                            amount = _options.DepositAmount,
                            currency = "USD"
                        }
                    }
                },
                primary_recipient = new
                {
                    customer_id = _options.CustomerId
                },
                title = "deposit"
            }
        };

        await PostToSquareAsync("/v2/invoices", invoicePayload, cancellationToken);
    }

    private async Task<bool> PostToSquareAsync(string endpoint, object payload, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);
        request.Headers.TryAddWithoutValidation("Square-Version", "2026-01-22");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogWarning("Square request failed. Endpoint={Endpoint} Status={StatusCode} Body={Body}", endpoint, (int)response.StatusCode, body);
        return false;
    }
}
