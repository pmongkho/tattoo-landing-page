using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using dotnet_server._Data;
using dotnet_server._Integrations;
using dotnet_server._Models.Square;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace dotnet_server._Services;

public interface ISquareDepositService
{
    Task HandleBookingCreatedAsync(JsonElement payload, CancellationToken cancellationToken);
    Task HandleBookingUpdatedAsync(JsonElement payload, CancellationToken cancellationToken);
    Task CreateDepositInvoiceForBookingAsync(string bookingId, CancellationToken cancellationToken);
    Task MarkDepositPaidAsync(JsonElement payload, CancellationToken cancellationToken);
    Task MarkDepositCanceledAsync(JsonElement payload, CancellationToken cancellationToken);
    Task CheckAcceptedBookingsWithoutInvoicesAsync(CancellationToken cancellationToken);
    Task MarkOverdueDepositsAsync(CancellationToken cancellationToken);
}

public class SquareDepositService(
    AppDbContext db,
    IHttpClientFactory httpClientFactory,
    IOptions<SquareOptions> options,
    ILogger<SquareDepositService> logger) : ISquareDepositService
{
    private readonly SquareOptions _options = options.Value;

    public Task HandleBookingCreatedAsync(JsonElement payload, CancellationToken cancellationToken)
        => UpsertBookingFromEventAsync(payload, cancellationToken);

    public async Task HandleBookingUpdatedAsync(JsonElement payload, CancellationToken cancellationToken)
    {
        await UpsertBookingFromEventAsync(payload, cancellationToken);
        var booking = payload.GetProperty("data").GetProperty("object").GetProperty("booking");
        var status = booking.TryGetProperty("status", out var statusNode) ? statusNode.GetString() : null;
        var bookingId = booking.GetProperty("id").GetString();
        if (string.Equals(status, "ACCEPTED", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(bookingId))
        {
            await CreateDepositInvoiceForBookingAsync(bookingId, cancellationToken);
        }
    }

    public async Task CreateDepositInvoiceForBookingAsync(string bookingId, CancellationToken cancellationToken)
    {
        var deposit = await db.BookingDeposits.SingleOrDefaultAsync(x => x.SquareBookingId == bookingId, cancellationToken);
        if (deposit is null || deposit.Status == BookingDepositStatus.Canceled || deposit.Status == BookingDepositStatus.Paid || !string.IsNullOrWhiteSpace(deposit.SquareInvoiceId))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(deposit.SquareCustomerId))
        {
            logger.LogWarning("Booking {BookingId} has no customer_id; skipping invoice creation.", bookingId);
            return;
        }

        var client = httpClientFactory.CreateClient("SquareApi");
        var orderId = await CreateOrderAsync(client, deposit, cancellationToken);
        if (string.IsNullOrWhiteSpace(orderId)) return;

        var invoice = await CreateAndPublishInvoiceAsync(client, deposit, orderId, cancellationToken);
        if (invoice.invoiceId is null) return;

        deposit.SquareOrderId = orderId;
        deposit.SquareInvoiceId = invoice.invoiceId;
        deposit.InvoicePublicUrl = invoice.publicUrl;
        deposit.Status = BookingDepositStatus.InvoiceSent;
        deposit.DueAt = DateTimeOffset.UtcNow.AddHours(_options.DepositDueHours);
        deposit.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkDepositPaidAsync(JsonElement payload, CancellationToken cancellationToken)
    {
        if (!TryGetInvoiceId(payload, out var invoiceId)) return;
        var deposit = await db.BookingDeposits.SingleOrDefaultAsync(x => x.SquareInvoiceId == invoiceId, cancellationToken);
        if (deposit is null) return;
        deposit.Status = BookingDepositStatus.Paid;
        deposit.PaidAt = DateTimeOffset.UtcNow;
        deposit.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkDepositCanceledAsync(JsonElement payload, CancellationToken cancellationToken)
    {
        if (!TryGetInvoiceId(payload, out var invoiceId)) return;
        var deposit = await db.BookingDeposits.SingleOrDefaultAsync(x => x.SquareInvoiceId == invoiceId, cancellationToken);
        if (deposit is null) return;
        deposit.Status = BookingDepositStatus.Canceled;
        deposit.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task CheckAcceptedBookingsWithoutInvoicesAsync(CancellationToken cancellationToken)
    {
        var acceptedWithoutInvoice = await db.BookingDeposits
            .Where(x => x.Status == BookingDepositStatus.Accepted && x.SquareInvoiceId == null)
            .Select(x => x.SquareBookingId)
            .ToListAsync(cancellationToken);

        foreach (var bookingId in acceptedWithoutInvoice)
        {
            await CreateDepositInvoiceForBookingAsync(bookingId, cancellationToken);
        }
    }

    public async Task MarkOverdueDepositsAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var overdue = await db.BookingDeposits
            .Where(x => x.Status == BookingDepositStatus.InvoiceSent && x.DueAt != null && x.DueAt < now)
            .ToListAsync(cancellationToken);

        if (overdue.Count == 0) return;
        foreach (var item in overdue)
        {
            item.Status = BookingDepositStatus.Overdue;
            item.UpdatedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertBookingFromEventAsync(JsonElement payload, CancellationToken cancellationToken)
    {
        var booking = payload.GetProperty("data").GetProperty("object").GetProperty("booking");
        var bookingId = booking.GetProperty("id").GetString();
        if (string.IsNullOrWhiteSpace(bookingId)) return;

        var customerId = booking.TryGetProperty("customer_id", out var c) ? c.GetString() : null;
        var locationId = booking.TryGetProperty("location_id", out var l) ? l.GetString() : null;
        var status = booking.TryGetProperty("status", out var s) ? s.GetString() : null;
        var startAt = booking.TryGetProperty("start_at", out var st) && DateTimeOffset.TryParse(st.GetString(), out var startParsed) ? startParsed : (DateTimeOffset?)null;

        var deposit = await db.BookingDeposits.SingleOrDefaultAsync(x => x.SquareBookingId == bookingId, cancellationToken);
        if (deposit is null)
        {
            deposit = new BookingDeposit
            {
                SquareBookingId = bookingId,
                DepositAmountCents = _options.DepositAmountCents
            };
            db.BookingDeposits.Add(deposit);
        }

        deposit.SquareCustomerId = customerId;
        deposit.SquareLocationId = locationId;
        deposit.AppointmentStartAt = startAt;
        deposit.Status = string.Equals(status, "ACCEPTED", StringComparison.OrdinalIgnoreCase)
            ? BookingDepositStatus.Accepted
            : string.Equals(status, "CANCELLED", StringComparison.OrdinalIgnoreCase)
                ? BookingDepositStatus.Canceled
                : BookingDepositStatus.Requested;
        deposit.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<string?> CreateOrderAsync(HttpClient client, BookingDeposit deposit, CancellationToken cancellationToken)
    {
        var note = $"booking_id={deposit.SquareBookingId}; appointment={deposit.AppointmentStartAt:O}";
        var payload = new
        {
            idempotency_key = $"booking-{deposit.SquareBookingId}-deposit-order",
            order = new
            {
                location_id = deposit.SquareLocationId ?? _options.LocationId,
                customer_id = deposit.SquareCustomerId,
                line_items = new[] { new { name = "Tattoo Appointment Deposit", quantity = "1", base_price_money = new { amount = deposit.DepositAmountCents, currency = "USD" }, note } }
            }
        };

        var response = await PostAsync(client, "/v2/orders", payload, cancellationToken);
        return response.RootElement.GetProperty("order").GetProperty("id").GetString();
    }

    private async Task<(string? invoiceId, string? publicUrl)> CreateAndPublishInvoiceAsync(HttpClient client, BookingDeposit deposit, string orderId, CancellationToken cancellationToken)
    {
        var invoicePayload = new
        {
            idempotency_key = $"booking-{deposit.SquareBookingId}-deposit-invoice",
            invoice = new
            {
                location_id = deposit.SquareLocationId ?? _options.LocationId,
                order_id = orderId,
                primary_recipient = new { customer_id = deposit.SquareCustomerId },
                payment_requests = new[] { new { request_type = "BALANCE", due_date = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(_options.DepositDueHours)), fixed_amount_requested_money = new { amount = deposit.DepositAmountCents, currency = "USD" } } },
                title = "Tattoo Appointment Deposit",
                description = "Tattoo appointment deposit. This deposit secures your appointment and goes toward the final tattoo price.",
                delivery_method = "EMAIL"
            }
        };

        var createRes = await PostAsync(client, "/v2/invoices", invoicePayload, cancellationToken);
        var invoiceId = createRes.RootElement.GetProperty("invoice").GetProperty("id").GetString();
        if (string.IsNullOrWhiteSpace(invoiceId)) return (null, null);

        await PostAsync(client, $"/v2/invoices/{invoiceId}/publish", new { version = createRes.RootElement.GetProperty("invoice").GetProperty("version").GetInt32() }, cancellationToken);
        var publicUrl = createRes.RootElement.GetProperty("invoice").TryGetProperty("public_url", out var urlNode) ? urlNode.GetString() : null;
        return (invoiceId, publicUrl);
    }

    private async Task<JsonDocument> PostAsync(HttpClient client, string endpoint, object payload, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);
        request.Headers.TryAddWithoutValidation("Square-Version", "2026-01-22");
        var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(body);
    }

    private static bool TryGetInvoiceId(JsonElement payload, out string? invoiceId)
    {
        invoiceId = null;
        if (!payload.TryGetProperty("data", out var data) || !data.TryGetProperty("object", out var obj)) return false;
        if (obj.TryGetProperty("invoice", out var invoice) && invoice.TryGetProperty("id", out var idNode))
        {
            invoiceId = idNode.GetString();
            return !string.IsNullOrWhiteSpace(invoiceId);
        }

        return false;
    }
}
