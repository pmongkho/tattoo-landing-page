using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using dotnet_server._Data;
using dotnet_server._Integrations;
using dotnet_server._Models.Square;
using dotnet_server._Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace dotnet_server._Controllers;

[ApiController]
[Route("api/square/webhooks")]
public class SquareWebhookController(
    AppDbContext db,
    ISquareDepositService depositService,
    IOptions<SquareOptions> options,
    ILogger<SquareWebhookController> logger) : ControllerBase
{
    private readonly SquareOptions _options = options.Value;

    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);

        var signatureHeader = Request.Headers["x-square-hmacsha256-signature"].ToString();
        if (!IsValidSignature(rawBody, signatureHeader)) return Unauthorized();

        using var doc = JsonDocument.Parse(rawBody);
        var root = doc.RootElement;
        var eventId = root.GetProperty("event_id").GetString();
        var eventType = root.GetProperty("type").GetString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(eventId)) return Ok();

        if (await db.SquareWebhookEvents.AnyAsync(x => x.SquareEventId == eventId, cancellationToken))
        {
            return Ok();
        }

        var eventRow = new SquareWebhookEvent { SquareEventId = eventId, EventType = eventType };
        db.SquareWebhookEvents.Add(eventRow);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            switch (eventType)
            {
                case "booking.created":
                    await depositService.HandleBookingCreatedAsync(root, cancellationToken);
                    break;
                case "booking.updated":
                    await depositService.HandleBookingUpdatedAsync(root, cancellationToken);
                    break;
                case "invoice.payment_made":
                    await depositService.MarkDepositPaidAsync(root, cancellationToken);
                    break;
                case "invoice.canceled":
                    await depositService.MarkDepositCanceledAsync(root, cancellationToken);
                    break;
            }

            eventRow.ProcessedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed processing square webhook event {EventId}", eventId);
        }

        return Ok();
    }

    private bool IsValidSignature(string rawBody, string signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader)
            || string.IsNullOrWhiteSpace(_options.WebhookSignatureKey)
            || string.IsNullOrWhiteSpace(_options.WebhookNotificationUrl))
        {
            return false;
        }

        var data = Encoding.UTF8.GetBytes(_options.WebhookNotificationUrl + rawBody);
        var keyBytes = Encoding.UTF8.GetBytes(_options.WebhookSignatureKey);
        using var hmac = new HMACSHA256(keyBytes);
        var hash = Convert.ToBase64String(hmac.ComputeHash(data));
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(hash), Encoding.UTF8.GetBytes(signatureHeader));
    }
}
