# API Tester for DevToys

A fast, local-first HTTP client built directly into DevToys 2. Send everyday API requests, inspect responses, and import cURL commands without opening a separate application.

> Preview release: the extension currently targets DevToys Preview 2.0.9.

## Features

- GET, POST, PUT, PATCH, and DELETE requests
- Query parameters and request headers
- JSON and plain-text request bodies
- Bearer Token and Basic Authentication
- Response status, duration, size, headers, and body
- Automatic JSON formatting with raw-text fallback
- In-memory history for the latest 50 requests
- cURL import and export
- DevToys clipboard Smart Detection for cURL commands

## Install

### DevToys

1. Download the latest `.nupkg` from [GitHub Releases](https://github.com/lunasoft-llc/DevToys.ApiTester/releases).
2. Open DevToys and go to **Manage extensions**.
3. Select the downloaded package and restart DevToys when prompted.

The package will also be available from [NuGet](https://www.nuget.org/packages/LUNASOFT.DevToys.ApiTester) after the first public NuGet release.

### Compatibility

| Extension | DevToys | Status |
|---|---|---|
| `0.1.x-preview` | Preview `2.0.9` | Supported |

## Quick start

1. Select an HTTP method and enter a request URL.
2. Configure **Params**, **Headers**, **Auth**, or **Body** as needed.
3. Select **Send** and inspect the response body, headers, and timing.

To import a request, paste a command such as:

```bash
curl -X POST 'https://httpbin.org/anything' \
  -H 'Content-Type: application/json' \
  -H 'Authorization: Bearer demo-token' \
  --data-raw '{"name":"Luna"}'
```

Open the **cURL** tab, select **Import cURL**, then send the populated request.

## Privacy

API Tester has no telemetry and no cloud synchronization. Requests are sent directly from your machine to the URL you specify. Request history is kept only in memory and disappears when the tool is closed.

Avoid sending credentials to endpoints you do not trust. This extension intentionally behaves as a network client and can transmit headers, tokens, and body content to the selected endpoint.

## Build from source

Prerequisites: .NET 8 SDK and DevToys 2.

```powershell
dotnet restore ApiTester.slnx
dotnet build ApiTester.slnx -c Release
dotnet test ApiTester.slnx -c Release
dotnet pack src/DevToys.ApiTester/DevToys.ApiTester.csproj -c Release
```

The package is created under `src/DevToys.ApiTester/bin/Release/`.

## Roadmap

- Environment variables such as `{{baseUrl}}` and `{{token}}`
- Persistent collections and history
- Save a JSONPath response value as a variable
- Request chaining
- Additional cURL syntax coverage

## Contributing

Issues and pull requests are welcome. Run the Release build and test suite before submitting changes.

## License

Licensed under the [MIT License](LICENSE).
