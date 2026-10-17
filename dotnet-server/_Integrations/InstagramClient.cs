using System.Net.Http.Headers;
using System.Text.Json;
using dotnet_server._Models;
using Microsoft.Extensions.Options;

namespace dotnet_server._Integrations;

public class InstagramOptions
{
    public string AccessToken { get; set; } = "";
    public string AdminKey { get; set; } = "";
    public string ApiVersion { get; set; } = "v22.0";
    public int SyncMinutes { get; set; } = 30;
}

public class InstagramClient(HttpClient http, IOptions<InstagramOptions> options)
{
    public async Task<IReadOnlyList<InstagramMedia>> FetchAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(options.Value.AccessToken))
            throw new InvalidOperationException("Instagram is not configured.");
        var version = options.Value.ApiVersion;
        if (!System.Text.RegularExpressions.Regex.IsMatch(version, @"^v\d+\.\d+$"))
            throw new InvalidOperationException("Invalid Instagram API version.");
        var result = new List<InstagramMedia>();
        string? after = null;
        // Bounded pagination: synchronize up to 500 recent posts. Never follow a remote next URL.
        for (var page = 0; page < 5; page++)
        {
            var path = $"{version}/me/media?fields=id,caption,media_type,media_product_type,media_url,thumbnail_url,permalink,timestamp,children{{id,media_type,media_url,thumbnail_url}}&limit=100";
            if (after is not null) path += "&after=" + Uri.EscapeDataString(after);
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.AccessToken);
            using var response = await http.SendAsync(request, ct);
            // Do not log Meta response bodies or request URLs: they can contain credentials.
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Instagram returned HTTP {(int)response.StatusCode}.");
            using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            foreach (var post in json.RootElement.GetProperty("data").EnumerateArray())
            {
                var caption = Text(post, "caption");
                var permalink = Text(post, "permalink");
                var published = DateTimeOffset.Parse(Text(post, "timestamp"), System.Globalization.CultureInfo.InvariantCulture);
                if (Text(post, "media_type") == "CAROUSEL_ALBUM" && post.TryGetProperty("children", out var children))
                    foreach (var child in children.GetProperty("data").EnumerateArray())
                        Add(child, caption, permalink, published);
                else Add(post, caption, permalink, published, Text(post, "media_product_type") == "REELS");
            }
            after = null;
            if (json.RootElement.TryGetProperty("paging", out var paging) && paging.TryGetProperty("next", out _) &&
                paging.TryGetProperty("cursors", out var cursors) && cursors.TryGetProperty("after", out var cursor))
                after = cursor.GetString();
            if (string.IsNullOrEmpty(after)) break;
        }
        return result;

        void Add(JsonElement media, string caption, string permalink, DateTimeOffset published, bool isReel = false)
        {
            var url = Text(media, "media_url");
            var id = Text(media, "id");
            if (string.IsNullOrEmpty(id) || !IsHttps(url)) return;
            var thumbnail = Text(media, "thumbnail_url");
            result.Add(new InstagramMedia { Id = id, MediaUrl = url,
                ThumbnailUrl = IsHttps(thumbnail) ? thumbnail : null,
                IsReel = isReel, MediaType = Text(media, "media_type"), Caption = caption,
                Permalink = IsHttps(permalink) ? permalink : "", PublishedAt = published });
        }
    }
    private static string Text(JsonElement value, string key) => value.TryGetProperty(key, out var property) ? property.GetString() ?? "" : "";
    private static bool IsHttps(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == "https";
}
