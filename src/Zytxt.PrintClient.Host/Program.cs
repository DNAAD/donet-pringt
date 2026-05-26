using System.Globalization;
using Microsoft.Win32;
using Zytxt.PrintClient.Core.Api;
using Zytxt.PrintClient.Core.Labels;
using Zytxt.PrintClient.Core.NativeDrawing;
using Zytxt.PrintClient.Core.Settings;
using Zytxt.PrintClient.Host.Printing;
using Zytxt.PrintClient.Host.Security;
using Zytxt.PrintClient.Host.Settings;

var builder = WebApplication.CreateBuilder(args);
var listenUrl = Environment.GetEnvironmentVariable("ZYTXT_PRINT_URL") ?? "http://127.0.0.1:37122";
builder.WebHost.UseUrls(listenUrl);
var dataDir = Environment.GetEnvironmentVariable("ZYTXT_PRINT_DATA_DIR")
    ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "zytxt-dotnet-print");
var settingsStore = new FileSettingsStore(Path.Combine(dataDir, "settings.json"));

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddCors(options =>
{
    options.AddPolicy(LocalCorsPolicy.PolicyName, policy =>
    {
        policy
            .SetIsOriginAllowed(origin => LocalCorsPolicy.IsAllowedOrigin(origin, settingsStore.Load().AllowedOrigins))
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();
var labelPlanner = new LabelRenderPlanner();
var previewRenderer = new LabelPreviewHtmlRenderer();
var gdiPreviewPageRenderer = new GdiPreviewPageRenderer();
var nativeDrawingPlanner = new NativeLabelDrawingPlanner();
var axisDiagnosticDrawingPlanner = new AxisDiagnosticDrawingPlanner();
var printEngine = new WindowsLabelPrintEngine();
var printerSelectionResolver = new PrinterSelectionResolver();
var settingsPageRenderer = new SettingsPageRenderer();

app.Use(async (context, next) =>
{
    if (PrivateNetworkAccessPolicy.ShouldAllow(
        context.Request.Headers[PrivateNetworkAccessPolicy.RequestHeaderName].ToString(),
        context.Request.Headers.Origin.ToString(),
        settingsStore.Load().AllowedOrigins))
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[PrivateNetworkAccessPolicy.ResponseHeaderName] = "true";
            return Task.CompletedTask;
        });
    }

    await next();
});

app.UseCors(LocalCorsPolicy.PolicyName);

app.MapGet("/", () => ApiResponse<HealthInfo>.Ok(new HealthInfo(true, "zytxt-dotnet-print", "http")));

app.MapGet("/health", () => ApiResponse<HealthInfo>.Ok(new HealthInfo(true, "zytxt-dotnet-print", "http")));

app.MapGet("/settings", () => ApiResponse<PrintClientSettings>.Ok(settingsStore.Load()));

app.MapGet("/settings-ui", (HttpRequest request) =>
{
    var settings = settingsStore.Load();
    var printers = GetPrinterInfos(settings);
    var previewTemplate = GetPreviewTemplate(request);
    var statusMessage = request.Query.ContainsKey("saved")
        ? "设置已保存，GDI 预览已按当前偏移刷新。"
        : request.Query.ContainsKey("printed")
            ? "测试打印已发送到本地打印机。"
            : request.Query.ContainsKey("axisPrinted")
                ? "坐标测试页已发送到本地打印机。"
                : "";
    return Results.Content(settingsPageRenderer.Render(settings, printers, statusMessage, previewTemplate), "text/html; charset=utf-8");
});

app.MapPost("/settings-ui", async (HttpRequest request) =>
{
    var form = await request.ReadFormAsync();
    var previewTemplate = GetPreviewTemplate(request, form);
    var settings = settingsStore.Load();
    ApplySettingsForm(settings, form);
    settingsStore.Save(settings);
    return Results.Redirect($"/settings-ui?saved=1&previewTemplate={previewTemplate}");
});

app.MapPost("/settings-ui/test-print", async (HttpRequest request) =>
{
    var form = await request.ReadFormAsync();
    var previewTemplate = GetPreviewTemplate(request, form);
    var settings = settingsStore.Load();
    ApplySettingsForm(settings, form);
    settingsStore.Save(settings);

    try
    {
        var selectedPrinterName = printerSelectionResolver.Resolve(settings);
        var labelPlan = labelPlanner.CreatePlan(CreatePreviewLabelItem(previewTemplate));
        var drawingPlan = nativeDrawingPlanner.CreatePlan(labelPlan, settings.LabelOffset);
        printEngine.Print(drawingPlan, selectedPrinterName);

        return Results.Redirect($"/settings-ui?printed=1&previewTemplate={previewTemplate}");
    }
    catch (Exception ex)
    {
        var printers = GetPrinterInfos(settings);
        return Results.Content(
            settingsPageRenderer.Render(settings, printers, $"测试打印失败：{ex.Message}", previewTemplate),
            "text/html; charset=utf-8",
            statusCode: StatusCodes.Status500InternalServerError);
    }
});

app.MapPost("/settings-ui/test-axis-print", async (HttpRequest request) =>
{
    var form = await request.ReadFormAsync();
    var previewTemplate = GetPreviewTemplate(request, form);
    var settings = settingsStore.Load();
    ApplySettingsForm(settings, form);
    settingsStore.Save(settings);

    try
    {
        var selectedPrinterName = printerSelectionResolver.Resolve(settings);
        printEngine.Print(
            axisDiagnosticDrawingPlanner.CreatePlan(),
            selectedPrinterName,
            ParsePrintMode(request.Query["mode"].ToString()));

        return Results.Redirect($"/settings-ui?axisPrinted=1&previewTemplate={previewTemplate}");
    }
    catch (Exception ex)
    {
        var printers = GetPrinterInfos(settings);
        return Results.Content(
            settingsPageRenderer.Render(settings, printers, $"坐标测试打印失败：{ex.Message}", previewTemplate),
            "text/html; charset=utf-8",
            statusCode: StatusCodes.Status500InternalServerError);
    }
});

app.MapGet("/preview", (HttpRequest request) =>
{
    var previewTemplate = GetPreviewTemplate(request);
    return Results.Content(
        gdiPreviewPageRenderer.Render($"/preview/gdi.png?template={previewTemplate}", $"/preview/html?template={previewTemplate}"),
        "text/html; charset=utf-8");
});

app.MapGet("/preview/html", (HttpRequest request) =>
{
    var plan = labelPlanner.CreatePlan(CreatePreviewLabelItem(GetPreviewTemplate(request)));

    return Results.Content(previewRenderer.Render(plan), "text/html; charset=utf-8");
});

app.MapGet("/preview/native-plan", (HttpRequest request) =>
{
    var settings = settingsStore.Load();
    var plan = labelPlanner.CreatePlan(CreatePreviewLabelItem(GetPreviewTemplate(request)));

    return ApiResponse<NativeLabelDrawingPlan>.Ok(nativeDrawingPlanner.CreatePlan(plan, settings.LabelOffset));
});

app.MapGet("/preview/gdi.png", (HttpRequest request) =>
{
    var settings = settingsStore.Load();
    var plan = labelPlanner.CreatePlan(CreatePreviewLabelItem(GetPreviewTemplate(request)));
    var drawingPlan = nativeDrawingPlanner.CreatePlan(plan, settings.LabelOffset);
    return Results.File(printEngine.RenderPreviewPng(drawingPlan), "image/png");
});

app.MapGet("/diagnostics/axis-plan", () =>
{
    return ApiResponse<NativeLabelDrawingPlan>.Ok(axisDiagnosticDrawingPlanner.CreatePlan());
});

app.MapGet("/diagnostics/axis.png", () =>
{
    return Results.File(printEngine.RenderPreviewPng(axisDiagnosticDrawingPlanner.CreatePlan()), "image/png");
});

app.MapGet("/diagnostics/axis-print-path.png", () =>
{
    return Results.File(printEngine.RenderPrintPathPreviewPng(axisDiagnosticDrawingPlanner.CreatePlan()), "image/png");
});

app.MapPost("/settings", (PrintClientSettings settings) =>
{
    settingsStore.Save(settings);
    return ApiResponse<PrintClientSettings>.Ok(settingsStore.Load());
});

app.MapGet("/printers", () =>
{
    var settings = settingsStore.Load();
    var printers = GetPrinterInfos(settings);

    return ApiResponse<PrinterListResponse>.Ok(new PrinterListResponse(printers));
});

app.MapPost("/print/tag", (PrintTagRequest request) =>
{
    if (request.Items.Count == 0)
    {
        return Results.BadRequest(ApiResponse<object>.Fail("BAD_REQUEST", "至少需要一条标签数据。", new { }));
    }

    var settings = settingsStore.Load();
    var selectedPrinterName = printerSelectionResolver.Resolve(settings, request.PrinterName);

    foreach (var item in request.Items)
    {
        var labelPlan = labelPlanner.CreatePlan(item);
        var drawingPlan = nativeDrawingPlanner.CreatePlan(labelPlan, settings.LabelOffset);
        if (request.ExecutePrint)
        {
            printEngine.Print(drawingPlan, selectedPrinterName);
        }
    }

    var requestId = string.IsNullOrWhiteSpace(request.RequestId)
        ? $"print-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
        : request.RequestId.Trim();
    var jobId = $"job-{Guid.NewGuid():N}";

    return Results.Ok(ApiResponse<PrintJobResult>.Ok(
        PrintJobResult.CreateAccepted(jobId, requestId, request.Items.Count)));
});

try
{
    Console.WriteLine($"ZYTXT Print Client is running at {listenUrl}");
    Console.WriteLine($"Settings UI: {listenUrl.TrimEnd('/')}/settings-ui");
    app.Run();
}
catch (Exception ex)
{
    WriteStartupError(dataDir, ex);
    ShowStartupError(listenUrl, dataDir, ex);
    throw;
}

static IReadOnlyList<string> GetInstalledPrinterNames()
{
    if (!OperatingSystem.IsWindows())
    {
        return [];
    }

    using var devices = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\Devices");
    return devices?.GetValueNames().Order(StringComparer.OrdinalIgnoreCase).ToList() ?? [];
}

static IReadOnlyList<PrinterInfo> GetPrinterInfos(PrintClientSettings settings)
{
    return GetInstalledPrinterNames()
        .Select(name => new PrinterInfo(
            name,
            name,
            string.Equals(name, settings.DefaultPrinter, StringComparison.OrdinalIgnoreCase),
            "unknown"))
        .ToList();
}

static bool TryParseMillimeter(string value, out decimal result)
{
    return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result)
        || decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out result);
}

static LabelPrintMode ParsePrintMode(string value)
{
    return value switch
    {
        "direct80" => LabelPrintMode.Direct80x30,
        "landscape80" => LabelPrintMode.Direct80x30Landscape,
        "feed0" => LabelPrintMode.FeedNoRotation,
        "feed90" => LabelPrintMode.FeedPreRotated90,
        _ => LabelPrintMode.Direct80x30
    };
}

static void ApplySettingsForm(PrintClientSettings settings, IFormCollection form)
{
    if (form.ContainsKey("defaultPrinter"))
    {
        settings.DefaultPrinter = form["defaultPrinter"].ToString();
    }

    settings.LabelOffset = new LabelOffset(
        TryParseMillimeter(form["offsetX"].ToString(), out var offsetX) ? offsetX : settings.LabelOffset.X,
        TryParseMillimeter(form["offsetY"].ToString(), out var offsetY) ? offsetY : settings.LabelOffset.Y);

    if (form.ContainsKey("allowedOrigins"))
    {
        settings.AllowedOrigins = AllowedOriginParser.Parse(form["allowedOrigins"].ToString());
    }
}

static string GetPreviewTemplate(HttpRequest request, IFormCollection? form = null)
{
    var value = form?["previewTemplate"].ToString();
    if (string.IsNullOrWhiteSpace(value))
    {
        value = request.Query["previewTemplate"].ToString();
    }

    if (string.IsNullOrWhiteSpace(value))
    {
        value = request.Query["template"].ToString();
    }

    return string.Equals(value, "silver", StringComparison.OrdinalIgnoreCase)
        ? "silver"
        : "default";
}

static LabelItem CreatePreviewLabelItem(string previewTemplate = "default")
{
    return new LabelItem
    {
        FactoryNo = previewTemplate == "silver" ? 25003 : null,
        IdentifierCode = "1000035933",
        ProductName = "足银镀金串珠-四方拉丝隔珠11.5mm41镶四方拉丝mm41镶四方拉丝99999",
        WeightCategory = "净金重",
        FinishedProductWeight = 123m,
        RoughWeight = 123m,
        SalesCode = "60318000ZB60",
        GoldPurity = "含金量990‰",
        Address = "水贝金座一层1111民族工匠",
        Price = 1299m,
        AdditionalPrice = 430m,
        TagWeight = 0.2m,
        CategoryName = "錾刻ZB",
        FinishedProductPartVO =
        [
            new LabelPartItem { CategoryName = "錾刻ZB", PartWeight = 123.00m },
            new LabelPartItem { CategoryName = "素金PD", PartWeight = 330.45m },
            new LabelPartItem { CategoryName = "錾刻ZB", PartWeight = 123.00m },
            new LabelPartItem { CategoryName = "素金PD", PartWeight = 330.45m },
            new LabelPartItem { CategoryName = "錾刻ZB", PartWeight = 123.00m },
            new LabelPartItem { CategoryName = "錾刻ZB", PartWeight = 123.00m },
            new LabelPartItem { CategoryName = "錾刻ZB", PartWeight = 123.00m }
        ],
        AdditionalRemark = "翡翠2",
        InlayWeight = 0.12m,
        RopeWeight = 0.30m,
        FinishedProductNote = "1111111"
    };

}

static void WriteStartupError(string dataDir, Exception exception)
{
    try
    {
        Directory.CreateDirectory(dataDir);
        var logPath = Path.Combine(dataDir, "startup-error.log");
        File.AppendAllText(
            logPath,
            $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}]{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}");
    }
    catch
    {
        // Startup diagnostics must never hide the original startup failure.
    }
}

static void ShowStartupError(string listenUrl, string dataDir, Exception exception)
{
    if (!Environment.UserInteractive
        || string.Equals(Environment.GetEnvironmentVariable("ZYTXT_PRINT_SUPPRESS_DIALOG"), "1", StringComparison.Ordinal))
    {
        return;
    }

    try
    {
        var message = $"本地打印服务启动失败。{Environment.NewLine}{Environment.NewLine}"
            + $"监听地址: {listenUrl}{Environment.NewLine}"
            + $"常见原因: 端口 37122 已被占用，或系统网络组件异常。{Environment.NewLine}"
            + $"错误日志: {Path.Combine(dataDir, "startup-error.log")}{Environment.NewLine}{Environment.NewLine}"
            + exception.Message;
        System.Windows.Forms.MessageBox.Show(
            message,
            "ZYTXT Print Client",
            System.Windows.Forms.MessageBoxButtons.OK,
            System.Windows.Forms.MessageBoxIcon.Error);
    }
    catch
    {
        // Console/file diagnostics are enough if a message box cannot be shown.
    }
}
