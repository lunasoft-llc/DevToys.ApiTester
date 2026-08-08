using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DevToys.ApiTester.Core;

public sealed class ApiClient(HttpClient httpClient)
{
    public async Task<ApiResponse> SendAsync(ApiRequest request, CancellationToken cancellationToken = default)
    {
        Uri uri = BuildUri(request.Url, request.Query);
        using var message = new HttpRequestMessage(new HttpMethod(request.Method), uri);
        if (!string.IsNullOrEmpty(request.Body))
        {
            string mediaType = request.Headers.TryGetValue("Content-Type", out string? contentType) ? contentType.Split(';')[0] : "application/json";
            message.Content = new StringContent(request.Body, Encoding.UTF8, mediaType);
        }
        foreach (var header in request.Headers)
        {
            if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)) continue;
            if (!message.Headers.TryAddWithoutValidation(header.Key, header.Value)) message.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        if (request.AuthType == ApiAuthType.Bearer) message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.AuthValue);
        else if (request.AuthType == ApiAuthType.Basic)
            message.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{request.AuthValue}:{request.BasicPassword}")));

        var timer = Stopwatch.StartNew();
        using HttpResponseMessage response = await httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        byte[] bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        timer.Stop();
        string body = Encoding.UTF8.GetString(bytes);
        var headers = response.Headers.Concat(response.Content.Headers).ToDictionary(x => x.Key, x => string.Join(", ", x.Value), StringComparer.OrdinalIgnoreCase);
        return new ApiResponse((int)response.StatusCode, response.ReasonPhrase ?? "", timer.Elapsed, bytes.LongLength, body, Pretty(body), headers);
    }

    private static Uri BuildUri(string url, IReadOnlyDictionary<string, string> query)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
            throw new ArgumentException("Enter a valid absolute HTTP/HTTPS URL.", nameof(url));
        if (query.Count == 0) return uri;
        string suffix = string.Join("&", query.Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}"));
        return new UriBuilder(uri) { Query = string.IsNullOrEmpty(uri.Query) ? suffix : uri.Query.TrimStart('?') + "&" + suffix }.Uri;
    }

    private static string Pretty(string body)
    {
        try { using JsonDocument json = JsonDocument.Parse(body); return JsonSerializer.Serialize(json, new JsonSerializerOptions { WriteIndented = true }); }
        catch (JsonException) { return body; }
    }
}
