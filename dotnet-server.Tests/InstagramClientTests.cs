using System.Net;
using System.Text;
using dotnet_server._Integrations;
using Microsoft.Extensions.Options;
using Xunit;

public class InstagramClientTests
{
    [Fact]
    public async Task MapsImagesReelsAndCarouselChildrenWithoutExposingTokenInUrl()
    {
        var handler = new Handler("""
        {"data":[
          {"id":"photo","media_type":"IMAGE","media_url":"https://cdn.example/photo.jpg","caption":"Healed tattoo","permalink":"https://www.instagram.com/p/photo/","timestamp":"2026-10-01T12:00:00+0000"},
          {"id":"reel","media_type":"VIDEO","media_product_type":"REELS","media_url":"https://cdn.example/reel.mp4","thumbnail_url":"https://cdn.example/reel.jpg","timestamp":"2026-10-02T12:00:00+0000"},
          {"id":"album","media_type":"CAROUSEL_ALBUM","caption":"Two pieces","permalink":"https://www.instagram.com/p/album/","timestamp":"2026-10-03T12:00:00+0000","children":{"data":[
            {"id":"child1","media_type":"IMAGE","media_url":"https://cdn.example/1.jpg"},
            {"id":"child2","media_type":"VIDEO","media_url":"https://cdn.example/2.mp4","thumbnail_url":"https://cdn.example/2.jpg"}]}}
        ]}
        """);
        var items = await Client(handler).FetchAsync(default);
        Assert.Equal(4, items.Count);
        Assert.Equal("Healed tattoo", items[0].Caption);
        Assert.Equal("VIDEO", items[1].MediaType);
        Assert.True(items[1].IsReel);
        Assert.False(items[3].IsReel);
        Assert.Equal("https://cdn.example/reel.jpg", items[1].ThumbnailUrl);
        Assert.Equal("child1", items[2].Id);
        Assert.Equal("Two pieces", items[2].Caption);
        Assert.Equal("https://www.instagram.com/p/album/", items[3].Permalink);
        Assert.DoesNotContain("private-test-token", handler.Paths.Single());
        Assert.Equal("Bearer private-test-token", handler.Auth.Single());
    }

    [Fact]
    public async Task PaginatesUsingTrustedHostAndEncodedCursor()
    {
        var handler = new Handler("""{"data":[],"paging":{"next":"https://untrusted.example/?access_token=secret","cursors":{"after":"a+b/="}}}""", """{"data":[]}""");
        await Client(handler).FetchAsync(default);
        Assert.Equal(2, handler.Paths.Count);
        Assert.All(handler.Hosts, host => Assert.Equal("graph.instagram.com", host));
        Assert.Contains("after=a%2Bb%2F%3D", handler.Paths[1]);
    }

    [Fact]
    public async Task RejectsNonHttpsMedia()
    {
        var handler = new Handler("""{"data":[{"id":"bad","media_type":"IMAGE","media_url":"javascript:alert(1)","timestamp":"2026-10-01T12:00:00Z"}]}""");
        Assert.Empty(await Client(handler).FetchAsync(default));
    }

    [Fact]
    public async Task UpstreamErrorsDoNotIncludeResponseBodyOrCredentials()
    {
        var handler = new Handler("private-test-token") { Status = HttpStatusCode.Unauthorized };
        var error = await Assert.ThrowsAsync<HttpRequestException>(() => Client(handler).FetchAsync(default));
        Assert.Contains("401", error.Message);
        Assert.DoesNotContain("private-test-token", error.ToString());
    }

    [Fact]
    public async Task MissingTokenMakesNoNetworkRequest()
    {
        var handler = new Handler();
        var client = new InstagramClient(new HttpClient(handler), Options.Create(new InstagramOptions()));
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.FetchAsync(default));
        Assert.Empty(handler.Paths);
    }

    private static InstagramClient Client(Handler handler) => new(new HttpClient(handler) { BaseAddress = new Uri("https://graph.instagram.com/") },
        Options.Create(new InstagramOptions { AccessToken = "private-test-token" }));

    private class Handler(params string[] replies) : HttpMessageHandler
    {
        public List<string> Paths { get; } = [];
        public List<string> Hosts { get; } = [];
        public List<string> Auth { get; } = [];
        public HttpStatusCode Status { get; init; } = HttpStatusCode.OK;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Paths.Add(request.RequestUri!.PathAndQuery);
            Hosts.Add(request.RequestUri.Host);
            Auth.Add(request.Headers.Authorization?.ToString() ?? "");
            return Task.FromResult(new HttpResponseMessage(Status) { Content = new StringContent(replies[Paths.Count - 1], Encoding.UTF8, "application/json") });
        }
    }
}
