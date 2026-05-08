namespace dotnet_server._Integrations;

public class SquareOptions
{
    public const string SectionName = "Square";
    public string AccessToken { get; set; } = string.Empty;
    public string Environment { get; set; } = "Sandbox";
    public string LocationId { get; set; } = string.Empty;
    public string WebhookSignatureKey { get; set; } = string.Empty;
    public string WebhookNotificationUrl { get; set; } = string.Empty;
    public int DepositAmountCents { get; set; } = 10000;
    public int DepositDueHours { get; set; } = 24;

    public string BaseUrl => string.Equals(Environment, "Sandbox", StringComparison.OrdinalIgnoreCase)
        ? "https://connect.squareupsandbox.com"
        : "https://connect.squareup.com";
}
