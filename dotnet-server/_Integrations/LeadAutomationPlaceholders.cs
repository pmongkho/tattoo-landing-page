using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using dotnet_server._Models;
using Microsoft.Extensions.Options;

namespace dotnet_server._Integrations;

public interface IQuoLeadMessagingClient
{
    Task NotifyNewLeadAsync(Consultation consultation, CancellationToken cancellationToken);
    Task<QuoMessageDispatchResult> SendNewLeadAsync(Consultation consultation, CancellationToken cancellationToken);
}

public record QuoMessageDispatchResult(
    bool Sent,
    int? StatusCode,
    string? ResponseBody,
    string? Error,
    string Endpoint,
    string? BaseUrl);

public interface ISquareCustomerClient
{
    Task CreateCustomerFromConsultationAsync(Consultation consultation, CancellationToken cancellationToken);
}

public class SquareCustomerClient(
    HttpClient httpClient,
    ILogger<SquareCustomerClient> logger,
    IOptions<SquareOptions> options) : ISquareCustomerClient
{
    private readonly SquareOptions _options = options.Value;

    public async Task CreateCustomerFromConsultationAsync(Consultation consultation, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.AccessToken))
        {
            logger.LogInformation("Square customer create skipped because AccessToken is not configured. ConsultationId={ConsultationId}", consultation.Id);
            return;
        }

        var (givenName, familyName) = SplitName(consultation.Name);
        if (string.IsNullOrWhiteSpace(givenName) || string.IsNullOrWhiteSpace(familyName))
        {
            logger.LogWarning("Square customer create skipped because name could not be split into first/last. ConsultationId={ConsultationId}", consultation.Id);
            return;
        }

        var payload = new
        {
            given_name = givenName,
            family_name = familyName,
            phone_number = consultation.PhoneNumber
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/v2/customers")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);
        request.Headers.TryAddWithoutValidation("Square-Version", "2026-01-22");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Square customer create failed. ConsultationId={ConsultationId} Status={StatusCode} Body={Body}", consultation.Id, (int)response.StatusCode, body);
            return;
        }

        logger.LogInformation("Square customer created for consultation {ConsultationId}.", consultation.Id);
    }

    private static (string givenName, string familyName) SplitName(string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2) return (string.Empty, string.Empty);
        return (parts[0], parts[^1]);
    }
}
