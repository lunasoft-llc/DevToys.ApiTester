namespace DevToys.ApiTester.Core;

public enum ApiAuthType { None, Bearer, Basic }

public sealed record ApiRequest(
    string Method,
    string Url,
    IReadOnlyDictionary<string, string> Query,
    IReadOnlyDictionary<string, string> Headers,
    string Body,
    ApiAuthType AuthType,
    string AuthValue,
    string BasicPassword = "");

public sealed record ApiResponse(
    int StatusCode,
    string ReasonPhrase,
    TimeSpan Elapsed,
    long Size,
    string Body,
    string PrettyBody,
    IReadOnlyDictionary<string, string> Headers);

public sealed record RequestHistoryItem(DateTimeOffset SentAt, ApiRequest Request, ApiResponse Response);
