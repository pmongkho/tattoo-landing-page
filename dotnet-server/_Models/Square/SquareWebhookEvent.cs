namespace dotnet_server._Models.Square;

public class SquareWebhookEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SquareEventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt { get; set; }
}
