namespace dotnet_server._Models;

public class InstagramMedia
{
    public string Id { get; set; } = "";
    public string MediaUrl { get; set; } = "";
    public string? ThumbnailUrl { get; set; }
    public string MediaType { get; set; } = "IMAGE";
    public bool IsReel { get; set; }
    public string Caption { get; set; } = "";
    public string Permalink { get; set; } = "";
    public DateTimeOffset PublishedAt { get; set; }
    public DateTimeOffset SyncedAt { get; set; }
    public bool Visible { get; set; } = true;
    public bool Pinned { get; set; }
    public int DisplayOrder { get; set; }
}
