namespace dotnet_server._Integrations;

public class QuoApiOptions
{
    public const string SectionName = "QuoApi";
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}

public class SquareApiOptions
{
    public const string SectionName = "SquareApi";
    public string BaseUrl { get; set; } = "https://connect.squareup.com";
    public string AccessToken { get; set; } = string.Empty;
    public string LocationId { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}
