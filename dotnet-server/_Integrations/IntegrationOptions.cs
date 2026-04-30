namespace dotnet_server._Integrations;

public class QuoApiOptions
{
    public const string SectionName = "QuoApi";
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string SmsPath { get; set; } = "/v1/messages";
    public string PhoneNumberId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string MessageTemplate { get; set; } = string.Empty;
}

public class SquareApiOptions
{
    public const string SectionName = "SquareApi";
    public string BaseUrl { get; set; } = "https://connect.squareup.com";
    public string AccessToken { get; set; } = string.Empty;
    public string LocationId { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string TeamMemberId { get; set; } = string.Empty;
    public string ServiceVariationId { get; set; } = string.Empty;
    public int AppointmentDurationMinutes { get; set; } = 60;
    public int DepositAmount { get; set; } = 50;
}
