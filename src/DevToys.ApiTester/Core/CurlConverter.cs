using System.Text;
using System.Text.RegularExpressions;

namespace DevToys.ApiTester.Core;

public static partial class CurlConverter
{
    [GeneratedRegex("(?:^|\\s)(?:-X|--request)\\s+(?:\\\"(?<v>[^\\\"]+)\\\"|'(?<v>[^']+)'|(?<v>\\S+))", RegexOptions.IgnoreCase)]
    private static partial Regex MethodRegex();
    [GeneratedRegex("(?:^|\\s)(?:-H|--header)\\s+(?:\\\"(?<v>[^\\\"]*)\\\"|'(?<v>[^']*)')", RegexOptions.IgnoreCase)]
    private static partial Regex HeaderRegex();
    [GeneratedRegex("(?:^|\\s)(?:-d|--data|--data-raw|--data-binary)\\s+(?:\\\"(?<v>(?:\\\\.|[^\\\"])*)\\\"|'(?<v>[^']*)')", RegexOptions.IgnoreCase)]
    private static partial Regex DataRegex();
    [GeneratedRegex("(?:^|\\s)(?:-u|--user)\\s+(?:\\\"(?<v>[^\\\"]*)\\\"|'(?<v>[^']*)'|(?<v>\\S+))", RegexOptions.IgnoreCase)]
    private static partial Regex UserRegex();
    [GeneratedRegex("https?://[^\\s'\\\"]+", RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegex();

    public static ApiRequest Parse(string curl)
    {
        if (!curl.TrimStart().StartsWith("curl", StringComparison.OrdinalIgnoreCase))
            throw new FormatException("Input must start with curl.");
        string normalized = curl.Replace("\\\r\n", " ").Replace("\\\n", " ");
        string url = UrlRegex().Match(normalized).Value;
        if (url.Length == 0) throw new FormatException("cURL URL was not found.");
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in HeaderRegex().Matches(normalized))
        {
            string value = match.Groups["v"].Value;
            int colon = value.IndexOf(':');
            if (colon > 0) headers[value[..colon].Trim()] = value[(colon + 1)..].Trim();
        }
        string body = Unescape(DataRegex().Match(normalized).Groups["v"].Value);
        string method = MethodRegex().Match(normalized).Groups["v"].Value.ToUpperInvariant();
        if (method.Length == 0) method = body.Length > 0 ? "POST" : "GET";
        ApiAuthType authType = ApiAuthType.None;
        string authValue = ""; string password = "";
        if (headers.Remove("Authorization", out string? auth))
        {
            if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) { authType = ApiAuthType.Bearer; authValue = auth[7..]; }
            else headers["Authorization"] = auth;
        }
        Match user = UserRegex().Match(normalized);
        if (user.Success)
        {
            string[] parts = user.Groups["v"].Value.Split(':', 2);
            authType = ApiAuthType.Basic; authValue = parts[0]; password = parts.ElementAtOrDefault(1) ?? "";
        }
        return new ApiRequest(method, url, new Dictionary<string, string>(), headers, body, authType, authValue, password);
    }

    public static string Export(ApiRequest request)
    {
        var value = new StringBuilder("curl -X ").Append(request.Method).Append(" '").Append(request.Url).Append('\'');
        foreach (var header in request.Headers) value.Append(" -H '").Append(header.Key).Append(": ").Append(Escape(header.Value)).Append('\'');
        if (request.AuthType == ApiAuthType.Bearer) value.Append(" -H 'Authorization: Bearer ").Append(Escape(request.AuthValue)).Append('\'');
        if (request.AuthType == ApiAuthType.Basic) value.Append(" -u '").Append(Escape(request.AuthValue)).Append(':').Append(Escape(request.BasicPassword)).Append('\'');
        if (!string.IsNullOrEmpty(request.Body)) value.Append(" --data-raw '").Append(Escape(request.Body)).Append('\'');
        return value.ToString();
    }

    private static string Escape(string value) => value.Replace("'", "'\\''");
    private static string Unescape(string value) => value.Replace("\\\"", "\"").Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t");
}
