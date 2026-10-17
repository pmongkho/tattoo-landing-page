using System.Net;
using System.Text;
using System.Text.Json;
using dotnet_server._Integrations;
using dotnet_server._Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

public class QuoLeadMessagingClientTests
{
    private static readonly Consultation Lead = new()
    {
        Name = "Jane Mary Doe", PhoneNumber = "+12025550148", Timeline = "Next month"
    };

    [Fact]
    public async Task CreatesContactBeforeSendingMessage()
    {
        var handler = new StubHandler(Reply("{\"data\":[],\"nextPageToken\":null}"), Reply("{}", HttpStatusCode.Created), Reply("{}"));
        var result = await Client(handler).SendNewLeadAsync(Lead, default);

        Assert.True(result.Sent);
        Assert.Equal(new[] { "GET /v1/contacts?maxResults=50", "POST /v1/contacts", "POST /v1/messages" }, handler.Requests.Select(r => $"{r.Method} {r.Path}"));
        using var contact = JsonDocument.Parse(handler.Requests[1].Body!);
        var fields = contact.RootElement.GetProperty("defaultFields");
        Assert.Equal("Jane", fields.GetProperty("firstName").GetString());
        Assert.Equal("Mary Doe", fields.GetProperty("lastName").GetString());
        Assert.Equal(Lead.PhoneNumber, fields.GetProperty("phoneNumbers")[0].GetProperty("value").GetString());
        using var message = JsonDocument.Parse(handler.Requests[2].Body!);
        Assert.Equal(Lead.PhoneNumber, message.RootElement.GetProperty("to")[0].GetString());
        Assert.Contains(Lead.Name, message.RootElement.GetProperty("content").GetString());
        Assert.All(handler.Requests, r => Assert.Equal("test-key", r.Authorization));
    }

    [Fact]
    public async Task ReusesContactMatchingNormalizedNameAndPhone()
    {
        var handler = new StubHandler(ContactPage(" jane  mary ", "DOE", "(202) 555-0148"), Reply("{}"));
        var result = await Client(handler).SendNewLeadAsync(Lead, default);
        Assert.True(result.Sent);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal("/v1/messages", handler.Requests[1].Path);
    }

    [Theory]
    [InlineData("Someone", "Else", "+12025550148")]
    [InlineData("Jane Mary", "Doe", "+12025550149")]
    public async Task DoesNotReuseContactUnlessBothNameAndPhoneMatch(string first, string last, string phone)
    {
        var handler = new StubHandler(ContactPage(first, last, phone), Reply("{}", HttpStatusCode.Created), Reply("{}"));
        Assert.True((await Client(handler).SendNewLeadAsync(Lead, default)).Sent);
        Assert.Equal("/v1/contacts", handler.Requests[1].Path);
        Assert.Equal(HttpMethod.Post, handler.Requests[1].Method);
    }

    [Fact]
    public async Task SearchesAllPagesBeforeCreatingContact()
    {
        var handler = new StubHandler(Reply("{\"data\":[],\"nextPageToken\":\"a+b/=\"}"), ContactPage("Jane", "Mary Doe", Lead.PhoneNumber), Reply("{}"));
        Assert.True((await Client(handler).SendNewLeadAsync(Lead, default)).Sent);
        Assert.Equal("/v1/contacts?maxResults=50&pageToken=a%2Bb%2F%3D", handler.Requests[1].Path);
        Assert.Equal("/v1/messages", handler.Requests[2].Path);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ContactFailurePreventsMessage(bool failCreation)
    {
        var failure = Reply("{}", HttpStatusCode.Forbidden);
        var handler = failCreation ? new StubHandler(Reply("{\"data\":[]}"), failure) : new StubHandler(failure);
        var result = await Client(handler).SendNewLeadAsync(Lead, default);
        Assert.False(result.Sent);
        Assert.Equal(403, result.StatusCode);
        Assert.Contains(failCreation ? "creation failed" : "lookup failed", result.Error);
        Assert.DoesNotContain(handler.Requests, r => r.Path == "/v1/messages");
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("{\"data\":{}}")]
    public async Task InvalidLookupResponseDoesNotCreateOrMessage(string body)
    {
        var handler = new StubHandler(Reply(body));
        Assert.False((await Client(handler).SendNewLeadAsync(Lead, default)).Sent);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task RepeatedPaginationTokenDoesNotCreateOrMessage()
    {
        var handler = new StubHandler(Reply("{\"data\":[],\"nextPageToken\":\"same\"}"), Reply("{\"data\":[],\"nextPageToken\":\"same\"}"));
        Assert.False((await Client(handler).SendNewLeadAsync(Lead, default)).Sent);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task DisabledIntegrationDoesNotCallQuo()
    {
        var handler = new StubHandler();
        Assert.False((await Client(handler, enabled: false).SendNewLeadAsync(Lead, default)).Sent);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task MessageFailureIsReportedAfterContactSync()
    {
        var handler = new StubHandler(ContactPage("Jane", "Mary Doe", Lead.PhoneNumber), Reply("{}", HttpStatusCode.TooManyRequests));
        var result = await Client(handler).SendNewLeadAsync(Lead, default);
        Assert.False(result.Sent);
        Assert.Equal(429, result.StatusCode);
    }

    [Fact]
    public async Task ApiVersionPrefixIsSharedWithContactsEndpoint()
    {
        var handler = new StubHandler(ContactPage("Jane", "Mary Doe", Lead.PhoneNumber), Reply("{}"));
        Assert.True((await Client(handler, baseUrl: "https://api.example.test/", smsPath: "/v1/messages").SendNewLeadAsync(Lead, default)).Sent);
        Assert.Equal("/v1/contacts?maxResults=50", handler.Requests[0].Path);
    }

    private static QuoLeadMessagingClient Client(StubHandler handler, bool enabled = true, string baseUrl = "https://api.example.test/v1/", string smsPath = "messages")
        => new(new HttpClient(handler) { BaseAddress = new Uri(baseUrl) }, NullLogger<QuoLeadMessagingClient>.Instance,
            Options.Create(new QuoApiOptions { Enabled = enabled, BaseUrl = baseUrl, ApiKey = "test-key", From = "+12025550199", SmsPath = smsPath }));

    private static HttpResponseMessage ContactPage(string first, string last, string phone)
        => Reply(JsonSerializer.Serialize(new { data = new[] { new { id = "contact-123", defaultFields = new { firstName = first, lastName = last, phoneNumbers = new[] { new { name = "Mobile", value = phone } } } } }, nextPageToken = (string?)null }));

    private static HttpResponseMessage Reply(string body, HttpStatusCode status = HttpStatusCode.OK)
        => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private sealed class StubHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new(responses);
        public List<(HttpMethod Method, string Path, string? Body, string? Authorization)> Requests { get; } = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add((request.Method, request.RequestUri!.PathAndQuery,
                request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken),
                request.Headers.TryGetValues("Authorization", out var auth) ? auth.Single() : null));
            return _responses.Dequeue();
        }
    }
}
