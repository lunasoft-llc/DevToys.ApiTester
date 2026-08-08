using System.Net;
using DevToys.ApiTester.Core;

namespace DevToys.ApiTester.Tests;

public sealed class ApiClientTests
{
    [Fact]
    public async Task SendAsync_ReturnsPrettyJsonAndMetrics()
    {
        var handler = new StubHandler(new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent("{\"ok\":true}") });
        var client = new ApiClient(new HttpClient(handler));
        ApiResponse response = await client.SendAsync(new ApiRequest("POST", "https://example.com", new Dictionary<string,string>{{"q","a b"}}, new Dictionary<string,string>(), "{}", ApiAuthType.Bearer, "token"));
        Assert.Equal(201, response.StatusCode); Assert.Contains(Environment.NewLine, response.PrettyBody);
        Assert.Equal("Bearer", handler.Request!.Headers.Authorization!.Scheme); Assert.Contains("q=a%20b", handler.Request.RequestUri!.Query);
    }

    private sealed class StubHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) { Request = request; return Task.FromResult(response); }
    }
}
