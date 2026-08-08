using System.ComponentModel.Composition;
using DevToys.Api;
using DevToys.ApiTester.Core;
using static DevToys.Api.GUI;

namespace DevToys.ApiTester.Gui;

[Export(typeof(IGuiTool))]
[Name("API Tester")]
[ToolDisplayInformation(
    IconFontName = "FluentSystemIcons", IconGlyph = '\uE1F4',
    GroupName = PredefinedCommonToolGroupNames.Testers,
    ResourceManagerAssemblyIdentifier = nameof(ApiTesterResourceAssemblyIdentifier),
    ResourceManagerBaseName = "DevToys.ApiTester.Strings.ApiTester",
    ShortDisplayTitleResourceName = "ShortDisplayTitle", LongDisplayTitleResourceName = "LongDisplayTitle",
    DescriptionResourceName = "Description", AccessibleNameResourceName = "AccessibleName",
    SearchKeywordsResourceName = "SearchKeywords")]
[AcceptedDataTypeName(PredefinedCommonDataTypeNames.Text)]
[NoCompactOverlaySupport]
internal sealed class ApiTesterGuiTool : IGuiTool, IDisposable
{
    private enum LayoutRows { Command, Workspace }
    private enum LayoutColumns { Main }
    private enum CommandRows { Only }
    private enum CommandColumns { Method, Url, Send }
    private enum PaneRows { Header, Content }
    private enum PaneColumns { Main }
    private enum ResponseHeaderColumns { Status, Tabs }
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(60) };
    private readonly ApiClient _client;
    private readonly List<RequestHistoryItem> _history = [];
    private readonly IUISelectDropDownList _method = SelectDropDownList("api-method");
    private readonly IUISingleLineTextInput _url = SingleLineTextInput("api-url");
    private readonly IUIMultiLineTextInput _query = MultiLineTextInput("api-query");
    private readonly IUIMultiLineTextInput _headers = MultiLineTextInput("api-headers");
    private readonly IUIMultiLineTextInput _body = MultiLineTextInput("api-body", "json");
    private readonly IUISelectDropDownList _auth = SelectDropDownList("api-auth");
    private readonly IUISingleLineTextInput _authValue = SingleLineTextInput("api-auth-value");
    private readonly IUISingleLineTextInput _password = SingleLineTextInput("api-password");
    private readonly IUIMultiLineTextInput _curl = MultiLineTextInput("api-curl", "shell");
    private readonly IUIMultiLineTextInput _response = MultiLineTextInput("api-response", "json");
    private readonly IUIMultiLineTextInput _responseHeaders = MultiLineTextInput("api-response-headers");
    private readonly IUILabel _responseSummary = Label().Text("Ready");
    private readonly IUIDataGrid _historyGrid = DataGrid("api-history");
    private readonly IUIButton _send = Button("api-send");
    private readonly IUIButton[] _requestTabs;
    private readonly IUIButton[] _responseTabs;
    private readonly IUIStack _queryPanel = Stack().Vertical();
    private readonly IUIStack _headersPanel = Stack().Vertical();
    private readonly IUIStack _authPanel = Stack().Vertical();
    private readonly IUIStack _bodyPanel = Stack().Vertical();
    private readonly IUIStack _curlPanel = Stack().Vertical();
    private CancellationTokenSource? _sendCancellation;

    public ApiTesterGuiTool()
    {
        _client = new ApiClient(_httpClient);
        _method.WithItems("GET", "POST", "PUT", "PATCH", "DELETE").Select(0);
        _auth.WithItems("None", "Bearer Token", "Basic Auth").OnItemSelected(UpdateAuthFields).Select(0);
        string[] requestNames = ["Params", "Headers", "Auth", "Body", "cURL"];
        _requestTabs = requestNames.Select((name, index) => Button($"request-tab-{index}").Text(name).OnClick(() => SelectRequestSection(index))).ToArray();
        string[] responseNames = ["Body", "Headers", "History"];
        _responseTabs = responseNames.Select((name, index) => Button($"response-tab-{index}").Text(name).OnClick(() => SelectResponseSection(index))).ToArray();
        SelectRequestSection(0);
        SelectResponseSection(0);
    }

    public UIToolView View
    {
        get
        {
            _queryPanel.WithChildren(_query.Title("Query params — key=value, one per line").Extendable());
            _headersPanel.WithChildren(_headers.Title("Headers — Name: value, one per line").Extendable()).Hide();
            _authPanel.MediumSpacing().WithChildren(
                _auth.Title("Authentication"), _authValue.Title("Token / username"), _password.Title("Basic password")).Hide();
            _bodyPanel.WithChildren(_body.Title("JSON / text body").Extendable()).Hide();
            _curlPanel.MediumSpacing().WithChildren(
                _curl.Title("Paste or generate cURL").Extendable(),
                Stack().Horizontal().MediumSpacing().WithChildren(
                    Button().Text("Import cURL").OnClick(ImportCurlAsync), Button().Text("Generate cURL").OnClick(GenerateCurlAsync))).Hide();

            return new UIToolView(false,
                Grid().RowMediumSpacing()
                    .Rows((LayoutRows.Command, Auto), (LayoutRows.Workspace, new UIGridLength(1, UIGridUnitType.Fraction)))
                    .Columns((LayoutColumns.Main, new UIGridLength(1, UIGridUnitType.Fraction)))
                    .Cells(
                        Cell(LayoutRows.Command, LayoutColumns.Main,
                            Grid().ColumnMediumSpacing()
                                .Rows((CommandRows.Only, Auto))
                                .Columns((CommandColumns.Method, Auto), (CommandColumns.Url, new UIGridLength(1, UIGridUnitType.Fraction)), (CommandColumns.Send, Auto))
                                .Cells(Cell(CommandRows.Only, CommandColumns.Method, _method.Title("Method")), Cell(CommandRows.Only, CommandColumns.Url, _url.Title("Request URL")),
                                    Cell(CommandRows.Only, CommandColumns.Send, _send.Text("Send").AccentAppearance().AlignVertically(UIVerticalAlignment.Bottom).OnClick(SendAsync)))),
                        Cell(LayoutRows.Workspace, LayoutColumns.Main,
                            SplitGrid().Vertical()
                                .WithLeftPaneChild(
                                    Grid().RowMediumSpacing()
                                        .Rows((PaneRows.Header, Auto), (PaneRows.Content, new UIGridLength(1, UIGridUnitType.Fraction)))
                                        .Columns((PaneColumns.Main, new UIGridLength(1, UIGridUnitType.Fraction)))
                                        .Cells(
                                            Cell(PaneRows.Header, PaneColumns.Main, Stack().Horizontal().SmallSpacing().WithChildren(_requestTabs)),
                                            Cell(PaneRows.Content, PaneColumns.Main, _queryPanel), Cell(PaneRows.Content, PaneColumns.Main, _headersPanel),
                                            Cell(PaneRows.Content, PaneColumns.Main, _authPanel), Cell(PaneRows.Content, PaneColumns.Main, _bodyPanel),
                                            Cell(PaneRows.Content, PaneColumns.Main, _curlPanel)))
                                .WithRightPaneChild(
                                    Grid().RowMediumSpacing()
                                        .Rows((PaneRows.Header, Auto), (PaneRows.Content, new UIGridLength(1, UIGridUnitType.Fraction)))
                                        .Columns((PaneColumns.Main, new UIGridLength(1, UIGridUnitType.Fraction)))
                                        .Cells(
                                            Cell(PaneRows.Header, PaneColumns.Main,
                                                Grid().Columns((ResponseHeaderColumns.Status, new UIGridLength(1, UIGridUnitType.Fraction)), (ResponseHeaderColumns.Tabs, Auto))
                                                    .Rows((PaneRows.Header, Auto))
                                                    .Cells(Cell(PaneRows.Header, ResponseHeaderColumns.Status, _responseSummary.Style(UILabelStyle.Subtitle)),
                                                        Cell(PaneRows.Header, ResponseHeaderColumns.Tabs, Stack().Horizontal().SmallSpacing().WithChildren(_responseTabs)))),
                                            Cell(PaneRows.Content, PaneColumns.Main, _response.Title("Response body").ReadOnly().Extendable()),
                                            Cell(PaneRows.Content, PaneColumns.Main, _responseHeaders.Title("Response headers").ReadOnly().Extendable().Hide()),
                                            Cell(PaneRows.Content, PaneColumns.Main, _historyGrid.Title("Request history").WithColumns("Time", "Method", "URL", "Status", "Duration").OnRowSelected(LoadHistory).Hide()))))));
        }
    }

    public void OnDataReceived(string dataTypeName, object? parsedData)
    {
        if (dataTypeName == PredefinedCommonDataTypeNames.Text && parsedData is string text && text.TrimStart().StartsWith("curl", StringComparison.OrdinalIgnoreCase))
        { _curl.Text(text); ImportCurl(); }
    }

    public void Dispose() { _sendCancellation?.Cancel(); _sendCancellation?.Dispose(); _httpClient.Dispose(); }

    private void SelectRequestSection(int selectedIndex)
    {
        IUIElement[] panels = [_queryPanel, _headersPanel, _authPanel, _bodyPanel, _curlPanel];
        for (int i = 0; i < panels.Length; i++)
        {
            if (i == selectedIndex) { panels[i].Show(); if (_requestTabs is not null) _requestTabs[i].AccentAppearance(); }
            else { panels[i].Hide(); if (_requestTabs is not null) _requestTabs[i].NeutralAppearance(); }
        }
    }

    private void SelectResponseSection(int selectedIndex)
    {
        IUIElement[] panels = [_response, _responseHeaders, _historyGrid];
        for (int i = 0; i < panels.Length; i++)
        {
            if (i == selectedIndex) { panels[i].Show(); if (_responseTabs is not null) _responseTabs[i].AccentAppearance(); }
            else { panels[i].Hide(); if (_responseTabs is not null) _responseTabs[i].NeutralAppearance(); }
        }
    }

    private void UpdateAuthFields(IUIDropDownListItem? selected)
    {
        int selectedIndex = Array.IndexOf(_auth.Items ?? [], selected);
        if (selectedIndex == (int)ApiAuthType.None) { _authValue.Hide(); _password.Hide(); }
        else if (selectedIndex == (int)ApiAuthType.Bearer) { _authValue.Show(); _password.Hide(); }
        else { _authValue.Show(); _password.Show(); }
    }

    private async ValueTask SendAsync()
    {
        _sendCancellation?.Cancel(); _sendCancellation?.Dispose(); _sendCancellation = new CancellationTokenSource();
        _send.Disable(); _responseSummary.Text("Sending…");
        try
        {
            ApiRequest request = CurrentRequest();
            ApiResponse response = await _client.SendAsync(request, _sendCancellation.Token);
            _response.Text(response.PrettyBody);
            _response.Language(IsJson(response.PrettyBody) ? "json" : "text");
            _responseHeaders.Text(KeyValueParser.Format(response.Headers));
            _responseSummary.Text($"{response.StatusCode} {response.ReasonPhrase}  •  {response.Elapsed.TotalMilliseconds:N0} ms  •  {FormatSize(response.Size)}");
            _history.Insert(0, new RequestHistoryItem(DateTimeOffset.Now, request, response));
            if (_history.Count > 50) _history.RemoveAt(_history.Count - 1);
            RefreshHistory();
        }
        catch (OperationCanceledException) { _responseSummary.Text("Request cancelled"); }
        catch (Exception ex) { _responseSummary.Text("Request failed"); _response.Text(ex.Message); }
        finally { _send.Enable(); }
    }

    private ValueTask ImportCurlAsync() { ImportCurl(); return ValueTask.CompletedTask; }
    private void ImportCurl()
    {
        try { LoadRequest(CurlConverter.Parse(_curl.Text)); _responseSummary.Text("cURL imported — ready to send"); }
        catch (Exception ex) { _responseSummary.Text("Could not import cURL"); _response.Text(ex.Message); }
    }
    private ValueTask GenerateCurlAsync() { _curl.Text(CurlConverter.Export(CurrentRequest())); return ValueTask.CompletedTask; }

    private ApiRequest CurrentRequest() => new(
        _method.SelectedItem?.Text ?? "GET", _url.Text,
        KeyValueParser.Parse(_query.Text, '='), KeyValueParser.Parse(_headers.Text), _body.Text,
        (ApiAuthType)Math.Max(0, Array.IndexOf(_auth.Items ?? [], _auth.SelectedItem)), _authValue.Text, _password.Text);

    private void LoadRequest(ApiRequest request)
    {
        int methodIndex = Array.FindIndex(_method.Items ?? [], x => string.Equals(x.Text, request.Method, StringComparison.OrdinalIgnoreCase));
        _method.Select(methodIndex < 0 ? 0 : methodIndex); _url.Text(request.Url);
        _query.Text(KeyValueParser.Format(request.Query, '=')); _headers.Text(KeyValueParser.Format(request.Headers)); _body.Text(request.Body);
        _auth.Select((int)request.AuthType); _authValue.Text(request.AuthValue); _password.Text(request.BasicPassword);
    }

    private void RefreshHistory() => _historyGrid.WithRows(_history.Select(x => Row(x,
        Cell(x.SentAt.ToString("HH:mm:ss")), Cell(x.Request.Method), Cell(x.Request.Url),
        Cell(x.Response.StatusCode.ToString()), Cell($"{x.Response.Elapsed.TotalMilliseconds:N0} ms"))).ToArray());

    private void LoadHistory(IUIDataGridRow? row)
    {
        if (row?.Value is not RequestHistoryItem item) return;
        LoadRequest(item.Request); _response.Text(item.Response.PrettyBody); _responseHeaders.Text(KeyValueParser.Format(item.Response.Headers));
        _responseSummary.Text($"{item.Response.StatusCode} {item.Response.ReasonPhrase}  •  {item.Response.Elapsed.TotalMilliseconds:N0} ms  •  {FormatSize(item.Response.Size)}");
    }

    private static bool IsJson(string value) { try { System.Text.Json.JsonDocument.Parse(value).Dispose(); return true; } catch { return false; } }
    private static string FormatSize(long bytes) => bytes < 1024 ? $"{bytes} B" : bytes < 1048576 ? $"{bytes / 1024d:N1} KB" : $"{bytes / 1048576d:N1} MB";
}

internal static class DropDownItemExtensions
{
    public static IUISelectDropDownList WithItems(this IUISelectDropDownList list, params string[] values)
        => list.WithItems(values.Select(x => GUI.Item(x)).ToArray());
}
