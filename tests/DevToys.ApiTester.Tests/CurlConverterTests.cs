using DevToys.ApiTester.Core;

namespace DevToys.ApiTester.Tests;

public sealed class CurlConverterTests
{
    [Fact]
    public void Parse_Curl_PopulatesRequest()
    {
        ApiRequest request = CurlConverter.Parse("curl -X POST 'https://example.com/users' -H 'Content-Type: application/json' -H 'Authorization: Bearer abc' --data-raw '{\"name\":\"Luna\"}'");
        Assert.Equal("POST", request.Method); Assert.Equal("https://example.com/users", request.Url);
        Assert.Equal(ApiAuthType.Bearer, request.AuthType); Assert.Equal("abc", request.AuthValue);
        Assert.Equal("application/json", request.Headers["Content-Type"]); Assert.Contains("Luna", request.Body);
    }

    [Fact]
    public void Parse_DataWithoutMethod_DefaultsToPost()
        => Assert.Equal("POST", CurlConverter.Parse("curl https://example.com -d 'hello'").Method);
}
