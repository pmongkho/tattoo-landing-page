using System.Text;
using System.Text.Json;
using System.Security.Authentication;
using dotnet_server._Models;
using Microsoft.Extensions.Options;
using dotnet_server._Utils;

namespace dotnet_server._Integrations;

public class QuoLeadMessagingClient(HttpClient httpClient, ILogger<QuoLeadMessagingClient> logger, IOptions<QuoApiOptions> options)
    : IQuoLeadMessagingClient
{
    private readonly QuoApiOptions _options = options.Value;
    // Serialize lookup/create within this process so simultaneous submissions do not create duplicates.
    private static readonly SemaphoreSlim ContactGate = new(1, 1);

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
            await EnsureContactAsync(consultation, endpoint, cancellationToken);
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
        catch (QuoContactException ex)
        {
            logger.LogWarning("Quo contact sync failed for consultation {ConsultationId}. Status={StatusCode}",
                consultation.Id, ex.StatusCode);
            return new QuoMessageDispatchResult(false, ex.StatusCode, null, ex.Message, endpoint, baseUrl);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Quo contact response was invalid for consultation {ConsultationId}.", consultation.Id);
            return new QuoMessageDispatchResult(false, null, null, "Quo returned an invalid contact response; message not sent.", endpoint, baseUrl);
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

    private async Task EnsureContactAsync(Consultation consultation, string smsEndpoint, CancellationToken cancellationToken)
    {
        // Share the configured API version prefix (e.g. messages or v1/messages).
        var slash = smsEndpoint.LastIndexOf('/');
        var contactsEndpoint = slash < 0 ? "contacts" : $"{smsEndpoint[..(slash + 1)]}contacts";
        if (!PhoneNumberNormalizer.TryNormalizeUsPhone(consultation.PhoneNumber, out var phone))
            throw new QuoContactException("Invalid consultation phone number; message not sent.");

        await ContactGate.WaitAsync(cancellationToken);
        try
        {
            string? pageToken = null;
            var visitedTokens = new HashSet<string>(StringComparer.Ordinal);
            do
            {
                var url = $"{contactsEndpoint}?maxResults=50";
                if (!string.IsNullOrEmpty(pageToken)) url += $"&pageToken={Uri.EscapeDataString(pageToken)}";
                using var request = CreateContactRequest(HttpMethod.Get, url);
                using var response = await httpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                    throw new QuoContactException("Quo contact lookup failed; message not sent.", (int)response.StatusCode);

                using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
                var root = document.RootElement;
                if (!root.TryGetProperty("data", out var contacts) || contacts.ValueKind != JsonValueKind.Array)
                    throw new QuoContactException("Quo returned an invalid contact list; message not sent.");

                foreach (var contact in contacts.EnumerateArray())
                {
                    if (!contact.TryGetProperty("defaultFields", out var fields) || fields.ValueKind != JsonValueKind.Object)
                        throw new QuoContactException("Quo returned invalid contact fields; message not sent.");
                    var fullName = $"{ReadString(fields, "firstName")} {ReadString(fields, "lastName")}";
                    if (!string.Equals(NormalizeName(fullName), NormalizeName(consultation.Name), StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (!fields.TryGetProperty("phoneNumbers", out var numbers) || numbers.ValueKind != JsonValueKind.Array)
                        continue;
                    foreach (var number in numbers.EnumerateArray())
                    {
                        if (number.ValueKind == JsonValueKind.Object &&
                            PhoneNumberNormalizer.TryNormalizeUsPhone(ReadString(number, "value"), out var existingPhone) &&
                            existingPhone == phone)
                            return;
                    }
                }

                pageToken = ReadString(root, "nextPageToken");
                if (!string.IsNullOrEmpty(pageToken) && !visitedTokens.Add(pageToken))
                    throw new QuoContactException("Quo returned a repeated contact page; message not sent.");
            } while (!string.IsNullOrEmpty(pageToken));

            var nameParts = NormalizeName(consultation.Name).Split(' ', 2);
            var payload = new
            {
                defaultFields = new
                {
                    firstName = nameParts[0],
                    lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty,
                    phoneNumbers = new[] { new { name = "Mobile", value = phone } }
                }
            };
            using var createRequest = CreateContactRequest(HttpMethod.Post, contactsEndpoint);
            createRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            using var createResponse = await httpClient.SendAsync(createRequest, cancellationToken);
            if (!createResponse.IsSuccessStatusCode)
                throw new QuoContactException("Quo contact creation failed; message not sent.", (int)createResponse.StatusCode);
        }
        finally
        {
            ContactGate.Release();
        }
    }

    private HttpRequestMessage CreateContactRequest(HttpMethod method, string endpoint)
    {
        var request = new HttpRequestMessage(method, endpoint);
        request.Headers.TryAddWithoutValidation("Authorization", _options.ApiKey.Trim());
        return request;
    }

    private static string? ReadString(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static string NormalizeName(string name)
        => string.Join(' ', name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private sealed class QuoContactException(string message, int? statusCode = null) : Exception(message)
    {
        public int? StatusCode { get; } = statusCode;
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

        return $"Hey {consultation.Name}! I'm one of Wo Hu's booking managers. Thanks for reaching out!\n\nPlease send over any reference pictures you have and let me know where on your body you'd like the tattoo, and we can get started.";
    }

    private static string NormalizeSmsPath(string? smsPath)
    {
        var value = string.IsNullOrWhiteSpace(smsPath) ? "messages" : smsPath.Trim();
        return value.TrimStart('/');
    }
}
