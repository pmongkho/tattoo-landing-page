namespace dotnet_server._Models.Square;

public enum BookingDepositStatus
{
    Requested,
    Accepted,
    InvoiceSent,
    Paid,
    Canceled,
    Overdue
}

public class BookingDeposit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SquareBookingId { get; set; } = string.Empty;
    public string? SquareCustomerId { get; set; }
    public string? SquareLocationId { get; set; }
    public string? SquareOrderId { get; set; }
    public string? SquareInvoiceId { get; set; }
    public DateTimeOffset? AppointmentStartAt { get; set; }
    public int DepositAmountCents { get; set; }
    public BookingDepositStatus Status { get; set; } = BookingDepositStatus.Requested;
    public string? InvoicePublicUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset? DueAt { get; set; }
}
