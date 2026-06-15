using System.Drawing;
using Zytxt.PrintClient.Core.Printing;
using Zytxt.PrintClient.Core.Settings;
using Zytxt.PrintClient.Core.Labels;
using Zytxt.PrintClient.Core.NativeDrawing;
using Zytxt.PrintClient.Core.Api;
using Zytxt.PrintClient.Core.Qr;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Zytxt.PrintClient.Host.Printing;
using Zytxt.PrintClient.Host.Security;
using Zytxt.PrintClient.Host.Settings;

var tests = new List<(string Name, Action Test)>
{
    ("LabelPaperSize.Create80x30 exposes millimeters and hundredths of an inch", TestLabelPaperSize80x30),
    ("FileSettingsStore returns safe defaults when settings file is missing", TestSettingsDefaults),
    ("FileSettingsStore persists printer, label offset, and allowed origins", TestSettingsPersistence),
    ("FileSettingsStore persists template element overrides", TestSettingsTemplateOverridePersistence),
    ("LocalCorsPolicy allows browser calls from localhost and loopback frontends", TestLocalCorsPolicy),
    ("PrivateNetworkAccessPolicy only allows trusted private-network preflights", TestPrivateNetworkAccessPolicy),
    ("AllowedOriginParser normalizes settings UI CORS origins", TestAllowedOriginParser),
    ("PrinterSelectionResolver uses .NET local default printer instead of request printer", TestPrinterSelectionResolver),
    ("LabelRenderPlanner maps label fields into a fixed 80x30 render plan", TestLabelRenderPlan),
    ("LabelRenderPlanner selects silver template for factory 25003", TestLabelRenderPlannerTemplateSelection),
    ("LabelRenderPlanner falls back to category weight when part array is empty or null", TestLabelRenderPlannerPartFallback),
    ("LabelRenderPlanner preserves Electron label detail fields", TestLabelRenderPlanDetails),
    ("LabelPreviewHtmlRenderer renders a fixed-size preview page", TestLabelPreviewHtmlRenderer),
    ("LabelPreviewHtmlRenderer renders silver template differences", TestLabelPreviewHtmlRendererSilverTemplate),
    ("GdiPreviewPageRenderer embeds the GDI PNG preview as the visible label", TestGdiPreviewPageRenderer),
    ("SettingsPageRenderer exposes printer selection, calibration preview, and test print", TestSettingsPageRendererCalibrationWorkbench),
    ("SettingsPageRenderer can switch GDI preview templates", TestSettingsPageRendererPreviewTemplateSwitch),
    ("SettingsPageRenderer exposes template element editor controls", TestSettingsPageRendererTemplateEditor),
    ("SettingsPageRenderer exposes preview zoom and template safety controls", TestSettingsPageRendererTemplateSafetyControls),
    ("SettingsFormApplier clamps template element values", TestSettingsFormApplierClampsTemplateElementValues),
    ("SettingsFormApplier resets the selected template element override", TestSettingsFormApplierResetsTemplateElementOverride),
    ("WindowsLabelPrintEngine renders GDI preview on the full 80x30 label canvas", TestWindowsLabelPrintEngineFullLabelPreview),
    ("WindowsLabelPrintEngine configures default print as direct 80x30", TestWindowsLabelPrintEngineDefaultPrintPage),
    ("WindowsLabelPrintEngine exposes axis diagnostic print mode variants", TestWindowsLabelPrintEnginePrintModeVariants),
    ("WindowsLabelPrintEngine renders print path through a pre-rotated feed canvas", TestWindowsLabelPrintEnginePreRotatedPrintPathPreview),
    ("AxisDiagnosticDrawingPlanner emits corner and direction markers", TestAxisDiagnosticDrawingPlan),
    ("NativeLabelDrawingPlanner emits millimeter-based drawing commands", TestNativeDrawingPlan),
    ("NativeLabelDrawingPlanner applies template element overrides", TestNativeDrawingPlanTemplateOverrides),
    ("NativeLabelDrawingPlanner emits silver label commands for factory 25003", TestNativeDrawingPlanSilverTemplate),
    ("NativeLabelDrawingPlanner applies silver template element overrides", TestNativeDrawingPlanSilverTemplateOverrides),
    ("NativeLabelDrawingPlanner keeps footer below dense part rows", TestNativeDrawingPlanKeepsFooterBelowPartRows),
    ("NativeLabelDrawingPlanner applies saved label offset to every command", TestNativeDrawingPlanOffset),
    ("QrCodeMatrixRenderer creates a real QR module matrix for label payloads", TestQrCodeMatrixRenderer),
    ("PrintUnitConverter converts millimeters into print units and pixels", TestPrintUnitConverter),
    ("ApiResponse.Ok returns existing local-service envelope shape", TestApiResponseEnvelope),
    ("PrintJobResult.Accepted returns queued job counters", TestPrintJobResultAccepted)
};

var failed = 0;

foreach (var (name, test) in tests)
{
    try
    {
        test();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception ex)
    {
        failed++;
        Console.WriteLine($"FAIL {name}");
        Console.WriteLine(ex.Message);
    }
}

if (failed > 0)
{
    Environment.Exit(1);
}

static void TestLabelPaperSize80x30()
{
    var size = LabelPaperSize.Create80x30();

    AssertEqual(80m, size.WidthMm, "WidthMm");
    AssertEqual(30m, size.HeightMm, "HeightMm");
    AssertEqual(315, size.WidthHundredthsInch, "WidthHundredthsInch");
    AssertEqual(118, size.HeightHundredthsInch, "HeightHundredthsInch");
}

static void TestSettingsDefaults()
{
    var settingsPath = BuildTestSettingsPath();
    var store = new FileSettingsStore(settingsPath);

    var settings = store.Load();

    AssertEqual("", settings.DefaultPrinter, "DefaultPrinter");
    AssertEqual(0m, settings.LabelOffset.X, "LabelOffset.X");
    AssertEqual(0m, settings.LabelOffset.Y, "LabelOffset.Y");
    AssertEqual(0, settings.AllowedOrigins.Count, "AllowedOrigins.Count");
}

static void TestSettingsPersistence()
{
    var settingsPath = BuildTestSettingsPath();
    var store = new FileSettingsStore(settingsPath);
    var expected = new PrintClientSettings
    {
        DefaultPrinter = "EPSON LQ-735KII ESC/P2",
        LabelOffset = new LabelOffset(1.5m, -2m),
        AllowedOrigins = ["http://127.0.0.1:80", "http://localhost:5173"]
    };

    store.Save(expected);
    var actual = store.Load();

    AssertEqual(expected.DefaultPrinter, actual.DefaultPrinter, "DefaultPrinter");
    AssertEqual(expected.LabelOffset.X, actual.LabelOffset.X, "LabelOffset.X");
    AssertEqual(expected.LabelOffset.Y, actual.LabelOffset.Y, "LabelOffset.Y");
    AssertEqual(expected.AllowedOrigins.Count, actual.AllowedOrigins.Count, "AllowedOrigins.Count");
    AssertEqual(expected.AllowedOrigins[0], actual.AllowedOrigins[0], "AllowedOrigins[0]");
    AssertEqual(expected.AllowedOrigins[1], actual.AllowedOrigins[1], "AllowedOrigins[1]");
}

static void TestSettingsTemplateOverridePersistence()
{
    var settingsPath = BuildTestSettingsPath();
    var store = new FileSettingsStore(settingsPath);
    var expected = new PrintClientSettings
    {
        TemplateOverrides = new Dictionary<string, Dictionary<string, TemplateElementOverride>>
        {
            ["default"] = new()
            {
                ["productName"] = new TemplateElementOverride
                {
                    X = 10.8m,
                    Y = 0.4m,
                    FontSizePt = 4.9m,
                    Bold = true
                }
            }
        }
    };

    store.Save(expected);
    var actual = store.Load();
    var productName = actual.TemplateOverrides["default"]["productName"];

    AssertEqual(10.8m, productName.X, "TemplateOverride.X");
    AssertEqual(0.4m, productName.Y, "TemplateOverride.Y");
    AssertEqual(4.9m, productName.FontSizePt, "TemplateOverride.FontSizePt");
    AssertEqual(true, productName.Bold, "TemplateOverride.Bold");
}

static void TestLocalCorsPolicy()
{
    var configuredOrigins = new[] { "https://manager.example.com" };

    AssertTrue(LocalCorsPolicy.IsAllowedOrigin("http://localhost", configuredOrigins), "localhost without port");
    AssertTrue(LocalCorsPolicy.IsAllowedOrigin("http://localhost:53451", configuredOrigins), "localhost with port");
    AssertTrue(LocalCorsPolicy.IsAllowedOrigin("http://127.0.0.1:5173", configuredOrigins), "127 loopback");
    AssertTrue(LocalCorsPolicy.IsAllowedOrigin("http://[::1]:5173", configuredOrigins), "ipv6 loopback");
    AssertTrue(LocalCorsPolicy.IsAllowedOrigin("http://114.132.160.27", configuredOrigins), "production frontend ip");
    AssertTrue(LocalCorsPolicy.IsAllowedOrigin("https://manager.example.com", configuredOrigins), "configured origin");
    AssertTrue(!LocalCorsPolicy.IsAllowedOrigin("http://example.com", configuredOrigins), "external origin denied");
    AssertTrue(!LocalCorsPolicy.IsAllowedOrigin("ftp://localhost", configuredOrigins), "non-http origin denied");
}

static void TestPrivateNetworkAccessPolicy()
{
    var configuredOrigins = new[] { "https://www.zy-taoxiaoti.com" };

    AssertTrue(
        PrivateNetworkAccessPolicy.ShouldAllow("true", "https://www.zy-taoxiaoti.com", configuredOrigins),
        "configured https origin can use private network access");
    AssertTrue(
        !PrivateNetworkAccessPolicy.ShouldAllow("false", "https://www.zy-taoxiaoti.com", configuredOrigins),
        "missing private network preflight header is ignored");
    AssertTrue(
        !PrivateNetworkAccessPolicy.ShouldAllow("true", "https://evil.example.com", configuredOrigins),
        "untrusted origin cannot use private network access");
}

static void TestAllowedOriginParser()
{
    var origins = AllowedOriginParser.Parse("""
        https://www.zy-taoxiaoti.com/
        http://114.132.160.27
        https://www.zy-taoxiaoti.com
        ftp://bad.example.com
        https://bad.example.com/path
        """);

    AssertEqual(2, origins.Count, "AllowedOrigins.Count");
    AssertEqual("https://www.zy-taoxiaoti.com", origins[0], "AllowedOrigins[0]");
    AssertEqual("http://114.132.160.27", origins[1], "AllowedOrigins[1]");
}

static void TestPrinterSelectionResolver()
{
    var resolver = new PrinterSelectionResolver();
    var settings = new PrintClientSettings
    {
        DefaultPrinter = "EPSON LQ-735KII ESC/P2"
    };

    var selected = resolver.Resolve(settings, requestPrinterName: "Ignored Request Printer");
    var fallback = resolver.Resolve(new PrintClientSettings(), requestPrinterName: "Ignored Request Printer");

    AssertEqual("EPSON LQ-735KII ESC/P2", selected, "selected");
    AssertEqual("", fallback, "fallback");
}


static string BuildTestSettingsPath()
{
    var directory = Path.Combine(AppContext.BaseDirectory, "test-data", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    return Path.Combine(directory, "settings.json");
}

static void TestLabelRenderPlan()
{
    var planner = new LabelRenderPlanner();
    var item = new LabelItem
    {
        IdentifierCode = "10000000789",
        ProductName = "瓒抽噾闀跺祵鍚婂潬",
        WeightCategory = "成品重",
        FinishedProductWeight = 3.2m,
        RoughWeight = 3.56m,
        SalesCode = "XS-001",
        GoldPurity = "瓒抽噾999",
        Address = "娣卞湷姘磋礉",
        CategoryName = "鍚婂潬",
        AdditionalRemark = "绮惧搧"
    };

    var plan = planner.CreatePlan(item);

    AssertEqual(80m, plan.PaperSize.WidthMm, "PaperSize.WidthMm");
    AssertEqual(30m, plan.PaperSize.HeightMm, "PaperSize.HeightMm");
    AssertEqual("10000000789", plan.IdentifierText, "IdentifierText");
    AssertEqual("瓒抽噾闀跺祵鍚婂潬", plan.ProductName, "ProductName");
    AssertEqual("成品重(g): 3.20", plan.FinishedWeightText, "FinishedWeightText");
    AssertEqual("总件重(g): 3.56", plan.RoughWeightText, "RoughWeightText");
    AssertEqual("XS-001", plan.SalesCode, "SalesCode");
    AssertEqual("执行标准QB/T2062 GB11887  瓒抽噾999", plan.StandardText, "StandardText");
    AssertEqual("地址:娣卞湷姘磋礉", plan.AddressText, "AddressText");
    AssertEqual("10000000789", plan.QrPayload, "QrPayload");
}

static void TestLabelRenderPlannerTemplateSelection()
{
    var planner = new LabelRenderPlanner();

    var silverPlan = planner.CreatePlan(new LabelItem { FactoryNo = 25003 });
    var defaultPlan = planner.CreatePlan(new LabelItem { FactoryNo = 10001 });
    var missingPlan = planner.CreatePlan(new LabelItem());

    AssertEqual(LabelTemplateKey.Silver80x30, silverPlan.TemplateKey, "Silver.TemplateKey");
    AssertEqual(LabelTemplateKey.Default80x30, defaultPlan.TemplateKey, "Default.TemplateKey");
    AssertEqual(LabelTemplateKey.Default80x30, missingPlan.TemplateKey, "Missing.TemplateKey");
}

static void TestLabelRenderPlannerPartFallback()
{
    var planner = new LabelRenderPlanner();

    var defaultPlan = planner.CreatePlan(new LabelItem
    {
        CategoryName = "錾刻ZB",
        FinishedProductWeight = 123m,
        FinishedProductPartVO = []
    });
    var silverPlan = planner.CreatePlan(new LabelItem
    {
        FactoryNo = 25003,
        CategoryName = "素金PD",
        FinishedProductWeight = 330.45m,
        FinishedProductPartVO = null!
    });

    AssertEqual(1, defaultPlan.Parts.Count, "Default.Parts.Count");
    AssertEqual("錾刻ZB", defaultPlan.Parts[0].CategoryName, "Default.Parts[0].CategoryName");
    AssertEqual("123.00", defaultPlan.Parts[0].PartWeightText, "Default.Parts[0].PartWeightText");
    AssertEqual(1, silverPlan.Parts.Count, "Silver.Parts.Count");
    AssertEqual("素金PD", silverPlan.Parts[0].CategoryName, "Silver.Parts[0].CategoryName");
    AssertEqual("330.45", silverPlan.Parts[0].PartWeightText, "Silver.Parts[0].PartWeightText");
}

static void TestLabelRenderPlanDetails()
{
    var planner = new LabelRenderPlanner();
    var plan = planner.CreatePlan(new LabelItem
    {
        IdentifierCode = "10000000789",
        ProductName = "瓒抽噾闀跺祵鍚婂潬",
        WeightCategory = "成品重",
        FinishedProductWeight = 3.2m,
        RoughWeight = 3.56m,
        SalesCode = "XS-001",
        GoldPurity = "瓒抽噾999",
        Address = "娣卞湷姘磋礉",
        AdditionalPrice = 8m,
        CategoryName = "鍚婂潬",
        AdditionalRemark = "绮惧搧",
        InlayWeight = 0.12m,
        RopeWeight = 0.34m,
        FinishedProductNote = "澶囨敞"
    });

    AssertEqual("瓒抽噾999", plan.GoldPurityText, "GoldPurityText");
    AssertEqual("附加:￥8.00", plan.AdditionalPriceText, "AdditionalPriceText");
    AssertEqual(1, plan.Parts.Count, "Parts.Count");
    AssertEqual("鍚婂潬", plan.Parts[0].CategoryName, "Parts[0].CategoryName");
    AssertEqual("3.20", plan.Parts[0].PartWeightText, "Parts[0].PartWeightText");
    AssertEqual("附加:绮惧搧 附加重:0.12g 绳重:0.34g 澶囨敞", plan.FooterText, "FooterText");
}

static void TestLabelPreviewHtmlRenderer()
{
    var planner = new LabelRenderPlanner();
    var renderer = new LabelPreviewHtmlRenderer();
    var plan = planner.CreatePlan(new LabelItem
    {
        IdentifierCode = "10000000789",
        ProductName = "瓒抽噾闀跺祵鍚婂潬",
        WeightCategory = "成品重",
        FinishedProductWeight = 3.2m,
        RoughWeight = 3.56m,
        SalesCode = "XS-001",
        GoldPurity = "瓒抽噾999",
        Address = "娣卞湷姘磋礉",
        CategoryName = "鍚婂潬",
        AdditionalRemark = "绮惧搧"
    });

    var html = renderer.Render(plan);

    AssertContains(html, "<!doctype html>", "doctype");
    AssertContains(html, "width: 80mm", "width");
    AssertContains(html, "height: 30mm", "height");
    AssertContains(html, "10000000789", "identifier");
    AssertContains(html, "label-root", "electron label root class");
    AssertContains(html, "content-band", "electron content band class");
    AssertContains(html, "detail-grid", "electron detail grid class");
    AssertContains(html, "footer-box", "electron footer box class");
    AssertContains(html, "瓒抽噾闀跺祵鍚婂潬", "productName");
    AssertContains(html, "成品重(g): 3.20", "finishedWeight");
    AssertContains(html, "总件重(g): 3.56", "roughWeight");
    AssertContains(html, "<svg", "qr svg");
    AssertContains(html, "data-qr-payload=\"10000000789\"", "qr payload");
    AssertContains(html, "鍚婂潬:<span class=\"number\">3.20</span>", "part detail");
    AssertContains(html, "附加:绮惧搧", "footer detail");
}

static void TestGdiPreviewPageRenderer()
{
    var renderer = new GdiPreviewPageRenderer();

    var html = renderer.Render("/preview/gdi.png", "/preview/html");

    AssertContains(html, "<!doctype html>", "doctype");
    AssertContains(html, "src=\"/preview/gdi.png\"", "gdi image");
    AssertContains(html, "width: 80mm", "preview width");
    AssertContains(html, "height: 30mm", "preview height");
    AssertContains(html, "HTML 对照", "html fallback link");
    AssertContains(html, "href=\"/preview/html\"", "html fallback href");
}

static void TestSettingsPageRendererCalibrationWorkbench()
{
    var renderer = new SettingsPageRenderer();
    var settings = new PrintClientSettings
    {
        DefaultPrinter = "EPSON LQ-735KII ESC/P2",
        LabelOffset = new LabelOffset(1.2m, -0.5m),
        AllowedOrigins = ["https://www.zy-taoxiaoti.com", "http://114.132.160.27"]
    };
    var printers = new List<PrinterInfo>
    {
        new("EPSON LQ-735KII ESC/P2", "EPSON LQ-735KII ESC/P2", true, "unknown"),
        new("Microsoft Print to PDF", "Microsoft Print to PDF", false, "unknown")
    };

    var html = renderer.Render(settings, printers);

    AssertContains(html, "name=\"defaultPrinter\"", "printer select");
    AssertContains(html, "EPSON LQ-735KII ESC/P2", "saved printer");
    AssertContains(html, "value=\"1.2\"", "offset x");
    AssertContains(html, "value=\"-0.5\"", "offset y");
    AssertContains(html, "name=\"allowedOrigins\"", "allowed origins textarea");
    AssertContains(html, "https://www.zy-taoxiaoti.com", "saved allowed domain");
    AssertContains(html, "http://114.132.160.27", "saved allowed ip");
    AssertNotContains(html, "name=\"printOffsetX\"", "print offset x removed");
    AssertNotContains(html, "name=\"printOffsetY\"", "print offset y removed");
    AssertContains(html, "src=\"/preview/gdi.png", "gdi preview");
    AssertContains(html, "formaction=\"/settings-ui/test-print\"", "test print submits current settings");
    AssertContains(html, "formaction=\"/settings-ui/test-axis-print\"", "axis test print submits current settings");
    AssertContains(html, "mode=direct80", "axis direct80 mode");
    AssertContains(html, "mode=landscape80", "axis landscape80 mode");
    AssertContains(html, "mode=feed0", "axis feed0 mode");
    AssertContains(html, "href=\"/diagnostics/axis.png\"", "axis diagnostic preview link");
    AssertContains(html, "href=\"/diagnostics/axis-print-path.png\"", "axis print path preview link");
    AssertContains(html, "class=\"settings-shell\"", "settings shell layout");
    AssertContains(html, "class=\"settings-column settings-column-left\"", "left settings column");
    AssertContains(html, "class=\"preview-stage\"", "central preview stage");
    AssertContains(html, "class=\"template-sidebar\"", "template editor sidebar");
    AssertContains(html, "打印机与连接", "printer connection panel title");
    AssertContains(html, "校准与测试", "calibration panel title");
}

static void TestSettingsPageRendererPreviewTemplateSwitch()
{
    var renderer = new SettingsPageRenderer();
    var html = renderer.Render(new PrintClientSettings(), [], previewTemplate: "silver");

    AssertContains(html, "name=\"previewTemplate\"", "preview template select");
    AssertContains(html, "value=\"default\"", "default template option");
    AssertContains(html, "value=\"silver\" selected", "silver template selected");
    AssertContains(html, "src=\"/preview/gdi.png?template=silver", "silver gdi preview url");
    AssertContains(html, "formaction=\"/settings-ui?previewTemplate=silver\"", "save preserves preview template");
    AssertContains(html, "formaction=\"/settings-ui/test-print\"", "test print action");
    AssertContains(html, "href=\"/preview/native-plan?template=silver\"", "native plan link");
    AssertContains(html, "href=\"/preview/html?template=silver\"", "html preview link");
}

static void TestSettingsPageRendererTemplateEditor()
{
    var renderer = new SettingsPageRenderer();
    var settings = new PrintClientSettings
    {
        TemplateOverrides = new Dictionary<string, Dictionary<string, TemplateElementOverride>>
        {
            ["default"] = new()
            {
                ["productName"] = new TemplateElementOverride
                {
                    X = 10.8m,
                    Y = 0.4m,
                    FontSizePt = 4.9m,
                    Bold = true
                }
            }
        }
    };

    var html = renderer.Render(settings, [], previewTemplate: "default");

    AssertContains(html, "name=\"templateElementKey\"", "template element select");
    AssertContains(html, "value=\"productName\" selected", "product name selected");
    AssertContains(html, "name=\"templateX\"", "template x input");
    AssertContains(html, "value=\"10.8\"", "template x value");
    AssertContains(html, "name=\"templateY\"", "template y input");
    AssertContains(html, "value=\"0.4\"", "template y value");
    AssertContains(html, "name=\"templateFontSizePt\"", "template font size input");
    AssertContains(html, "value=\"4.9\"", "template font size value");
    AssertContains(html, "name=\"templateBold\" type=\"checkbox\" checked", "template bold checked");
}

static void TestSettingsPageRendererTemplateSafetyControls()
{
    var renderer = new SettingsPageRenderer();
    var html = renderer.Render(new PrintClientSettings(), [], previewTemplate: "default");

    AssertContains(html, "name=\"previewZoom\"", "preview zoom control");
    AssertContains(html, "id=\"previewZoomValue\"", "preview zoom percentage label");
    AssertContains(html, "name=\"templateReset\" value=\"current\"", "reset current template element button");
    AssertContains(html, "name=\"templateX\" type=\"number\" step=\"0.1\" min=\"0\" max=\"80\"", "template x range");
    AssertContains(html, "name=\"templateY\" type=\"number\" step=\"0.1\" min=\"0\" max=\"30\"", "template y range");
    AssertContains(html, "name=\"templateFontSizePt\" type=\"number\" step=\"0.1\" min=\"1\" max=\"12\"", "template font range");
}

static void TestSettingsFormApplierClampsTemplateElementValues()
{
    var settings = new PrintClientSettings();
    var form = BuildForm(new Dictionary<string, string>
    {
        ["offsetX"] = "0",
        ["offsetY"] = "0",
        ["templateElementKey"] = "productName",
        ["templateX"] = "-12",
        ["templateY"] = "99",
        ["templateFontSizePt"] = "42",
        ["templateBold"] = "true"
    });

    SettingsFormApplier.Apply(settings, form, "default");
    var productName = settings.TemplateOverrides["default"]["productName"];

    AssertEqual(0m, productName.X, "TemplateOverride.X clamps to left edge");
    AssertEqual(30m, productName.Y, "TemplateOverride.Y clamps to paper height");
    AssertEqual(12m, productName.FontSizePt, "TemplateOverride.FontSizePt clamps to max");
    AssertEqual(true, productName.Bold, "TemplateOverride.Bold");
}

static void TestSettingsFormApplierResetsTemplateElementOverride()
{
    var settings = new PrintClientSettings
    {
        TemplateOverrides = new Dictionary<string, Dictionary<string, TemplateElementOverride>>
        {
            ["default"] = new()
            {
                ["productName"] = new TemplateElementOverride
                {
                    X = 10.8m,
                    Y = 0.4m,
                    FontSizePt = 4.9m,
                    Bold = true
                }
            }
        }
    };
    var form = BuildForm(new Dictionary<string, string>
    {
        ["offsetX"] = "0",
        ["offsetY"] = "0",
        ["templateElementKey"] = "productName",
        ["templateX"] = "10.8",
        ["templateY"] = "0.4",
        ["templateFontSizePt"] = "4.9",
        ["templateReset"] = "current"
    });

    SettingsFormApplier.Apply(settings, form, "default");

    AssertEqual(false, settings.TemplateOverrides.ContainsKey("default"), "Default template override removed when empty");
}

static void TestWindowsLabelPrintEngineFullLabelPreview()
{
    var labelPlan = new LabelRenderPlanner().CreatePlan(new LabelItem
    {
        IdentifierCode = "10000000789",
        ProductName = "瓒抽噾闀跺祵鍚婂潬",
        WeightCategory = "成品重",
        FinishedProductWeight = 3.2m,
        RoughWeight = 3.56m,
        SalesCode = "XS-001",
        GoldPurity = "瓒抽噾999",
        Address = "娣卞湷姘磋礉"
    });
    var drawingPlan = new NativeLabelDrawingPlanner().CreatePlan(labelPlan);
    var png = new WindowsLabelPrintEngine().RenderPreviewPng(drawingPlan);

    using var image = Image.FromStream(new MemoryStream(png));

    AssertEqual(945, image.Width, "preview width");
    AssertEqual(354, image.Height, "preview height");
    AssertTrue(CountDarkPixels((Bitmap)image) > 500, "preview contains drawn content");
}

static void TestWindowsLabelPrintEngineDefaultPrintPage()
{
    var plan = new AxisDiagnosticDrawingPlanner().CreatePlan();
    using var document = new WindowsLabelPrintEngine().CreatePrintDocument(plan, "");

    AssertEqual(315, document.DefaultPageSettings.PaperSize.Width, "PaperSize.Width");
    AssertEqual(118, document.DefaultPageSettings.PaperSize.Height, "PaperSize.Height");
    AssertEqual(false, document.DefaultPageSettings.Landscape, "Landscape");
    AssertEqual(false, document.OriginAtMargins, "OriginAtMargins");
    AssertEqual(0, document.DefaultPageSettings.Margins.Left, "Margin.Left");
    AssertEqual(0, document.DefaultPageSettings.Margins.Top, "Margin.Top");
}

static void TestWindowsLabelPrintEnginePrintModeVariants()
{
    var plan = new AxisDiagnosticDrawingPlanner().CreatePlan();
    var engine = new WindowsLabelPrintEngine();

    using var direct = engine.CreatePrintDocument(plan, "", LabelPrintMode.Direct80x30);
    using var landscape = engine.CreatePrintDocument(plan, "", LabelPrintMode.Direct80x30Landscape);
    using var feed = engine.CreatePrintDocument(plan, "", LabelPrintMode.FeedNoRotation);

    AssertEqual(315, direct.DefaultPageSettings.PaperSize.Width, "Direct.PaperSize.Width");
    AssertEqual(118, direct.DefaultPageSettings.PaperSize.Height, "Direct.PaperSize.Height");
    AssertEqual(false, direct.DefaultPageSettings.Landscape, "Direct.Landscape");
    AssertEqual(315, landscape.DefaultPageSettings.PaperSize.Width, "Landscape.PaperSize.Width");
    AssertEqual(118, landscape.DefaultPageSettings.PaperSize.Height, "Landscape.PaperSize.Height");
    AssertEqual(true, landscape.DefaultPageSettings.Landscape, "Landscape.Landscape");
    AssertEqual(118, feed.DefaultPageSettings.PaperSize.Width, "Feed.PaperSize.Width");
    AssertEqual(315, feed.DefaultPageSettings.PaperSize.Height, "Feed.PaperSize.Height");
    AssertEqual(true, feed.DefaultPageSettings.Landscape, "Feed.Landscape");
}

static void TestWindowsLabelPrintEnginePreRotatedPrintPathPreview()
{
    var plan = new AxisDiagnosticDrawingPlanner().CreatePlan();
    var png = new WindowsLabelPrintEngine().RenderPrintPathPreviewPng(plan);

    using var image = Image.FromStream(new MemoryStream(png));

    AssertEqual(354, image.Width, "print path preview width");
    AssertEqual(945, image.Height, "print path preview height");
    AssertTrue(CountDarkPixels((Bitmap)image) > 500, "print path preview contains drawn content");
}

static void TestAxisDiagnosticDrawingPlan()
{
    var plan = new AxisDiagnosticDrawingPlanner().CreatePlan();

    AssertEqual(80m, plan.PaperSize.WidthMm, "PaperSize.WidthMm");
    AssertEqual(30m, plan.PaperSize.HeightMm, "PaperSize.HeightMm");
    AssertTrue(plan.Commands.Any(command => command.Type == NativeDrawCommandType.Rectangle && command.Width == 80m && command.Height == 30m), "Border rectangle");
    AssertTrue(plan.Commands.Any(command => command.Text == "TL 0,0"), "TL marker");
    AssertTrue(plan.Commands.Any(command => command.Text == "TR 80,0"), "TR marker");
    AssertTrue(plan.Commands.Any(command => command.Text == "BL 0,30"), "BL marker");
    AssertTrue(plan.Commands.Any(command => command.Text == "BR 80,30"), "BR marker");
    AssertTrue(plan.Commands.Any(command => command.Text == "X -> 80mm"), "X direction marker");
    AssertTrue(plan.Commands.Any(command => command.Text == "Y -> 30mm"), "Y direction marker");
}

static void TestNativeDrawingPlan()
{
    var labelPlan = new LabelRenderPlanner().CreatePlan(new LabelItem
    {
        IdentifierCode = "10000000789",
        ProductName = "瓒抽噾闀跺祵鍚婂潬",
        WeightCategory = "成品重",
        FinishedProductWeight = 3.2m,
        RoughWeight = 3.56m,
        SalesCode = "XS-001",
        GoldPurity = "瓒抽噾999",
        Address = "娣卞湷姘磋礉",
        CategoryName = "鍚婂潬"
    });
    var drawingPlan = new NativeLabelDrawingPlanner().CreatePlan(labelPlan);

    AssertEqual(80m, drawingPlan.PaperSize.WidthMm, "PaperSize.WidthMm");
    AssertEqual(30m, drawingPlan.PaperSize.HeightMm, "PaperSize.HeightMm");
    AssertTrue(drawingPlan.Commands.Count >= 11, "Commands.Count");
    AssertEqual(NativeDrawCommandType.Text, drawingPlan.Commands[0].Type, "Commands[0].Type");
    AssertEqual("10000000789", drawingPlan.Commands[0].Text, "Commands[0].Text");
    AssertEqual(0m, drawingPlan.Commands[0].X, "Commands[0].X");
    AssertEqual(0m, drawingPlan.Commands[0].Y, "Commands[0].Y");
    AssertEqual(NativeDrawCommandType.QrCode, drawingPlan.Commands[2].Type, "Commands[2].Type");
    AssertEqual("10000000789", drawingPlan.Commands[2].Text, "Commands[2].Text");
    AssertEqual(9m, drawingPlan.Commands[2].Width, "Commands[2].Width");
    AssertEqual(1.8m, drawingPlan.Commands[2].X, "Commands[2].X");
    AssertEqual(1m, drawingPlan.Commands[2].Y, "Commands[2].Y");
    var labelWeightCommand = drawingPlan.Commands.Single(command => command.Text == "标签约0.20g");
    AssertEqual(1.8m, labelWeightCommand.X, "LabelWeight.X");
    AssertTrue(labelWeightCommand.Y >= drawingPlan.Commands[2].Y + drawingPlan.Commands[2].Height - 1.1m, "LabelWeight near QR bottom");
    AssertTrue(labelWeightCommand.Y <= drawingPlan.Commands[2].Y + drawingPlan.Commands[2].Height + 0.4m, "LabelWeight directly below QR");
    var productNameCommand = drawingPlan.Commands.Single(command => command.Text == labelPlan.ProductName);
    AssertEqual(3, productNameCommand.MaxLines, "ProductName.MaxLines");
    AssertEqual(true, productNameCommand.Ellipsis, "ProductName.Ellipsis");
    AssertTrue(productNameCommand.Height <= 5.4m, "ProductName height allows at most three lines");
    var finishedWeightValueCommand = drawingPlan.Commands.First(command => command.Text == "3.20" && command.FontSizePt == 4.5m);
    var roughWeightValueCommand = drawingPlan.Commands.First(command => command.Text == "3.56" && command.FontSizePt == 4.5m);
    AssertEqual(4.5m, finishedWeightValueCommand.FontSizePt, "FinishedWeightValue.FontSizePt");
    AssertEqual(4.5m, roughWeightValueCommand.FontSizePt, "RoughWeightValue.FontSizePt");
    AssertEqual(false, drawingPlan.Commands.Single(command => command.Text == labelPlan.StandardText).Bold, "StandardText.Bold");
    AssertEqual(false, drawingPlan.Commands.Single(command => command.Text == labelPlan.AddressText).Bold, "AddressText.Bold");
    AssertEqual(true, drawingPlan.Commands.Single(command => command.Text == labelPlan.SalesCode).Bold, "SalesCode.Bold");
    AssertEqual(NativeDrawCommandType.Text, drawingPlan.Commands[^1].Type, "LastCommand.Type");
    AssertEqual(labelPlan.IdentifierText, drawingPlan.Commands[^1].Text, "LastCommand.Text");
    AssertEqual(90m, drawingPlan.Commands[^1].RotationDegrees, "LastCommand.RotationDegrees");
    AssertTrue(drawingPlan.Commands[^1].X >= 22m, "LastCommand.X places code at right side");
    AssertTrue(drawingPlan.Commands[^1].Y >= 15m, "LastCommand.Y places code as a vertical line");
    AssertTrue(drawingPlan.Commands.Any(command => command.Text.Contains("XS-001", StringComparison.Ordinal)), "sales code command");
    var partCommand = drawingPlan.Commands.Single(command => command.Text.Contains("鍚婂潬:3.20", StringComparison.Ordinal));
    AssertEqual(false, partCommand.Bold, "PartCommand.Bold");
}

static void TestNativeDrawingPlanTemplateOverrides()
{
    var labelPlan = new LabelRenderPlanner().CreatePlan(new LabelItem
    {
        IdentifierCode = "10000000789",
        ProductName = "足金镶嵌吊坠",
        WeightCategory = "成品重",
        FinishedProductWeight = 3.2m,
        RoughWeight = 3.56m,
        SalesCode = "XS-001",
        GoldPurity = "足金999",
        Address = "水贝金座"
    });
    var overrides = new Dictionary<string, TemplateElementOverride>
    {
        ["productName"] = new()
        {
            X = 11.1m,
            Y = 0.6m,
            FontSizePt = 4.9m,
            Bold = true
        }
    };

    var drawingPlan = new NativeLabelDrawingPlanner().CreatePlan(labelPlan, offset: null, overrides);
    var productNameCommand = drawingPlan.Commands.Single(command => command.ElementKey == "productName");

    AssertEqual(11.1m, productNameCommand.X, "ProductName.X");
    AssertEqual(0.6m, productNameCommand.Y, "ProductName.Y");
    AssertEqual(4.9m, productNameCommand.FontSizePt, "ProductName.FontSizePt");
    AssertEqual(true, productNameCommand.Bold, "ProductName.Bold");
}

static void TestLabelPreviewHtmlRendererSilverTemplate()
{
    var planner = new LabelRenderPlanner();
    var renderer = new LabelPreviewHtmlRenderer();
    var plan = planner.CreatePlan(new LabelItem
    {
        FactoryNo = 25003,
        IdentifierCode = "100003593",
        ProductName = "足银镀金串珠",
        WeightCategory = "净金重",
        FinishedProductWeight = 123m,
        RoughWeight = 123m,
        SalesCode = "60318000ZB60",
        Address = "水贝金座一层1111民族工匠",
        Price = 1299m,
        AdditionalPrice = 430m,
        TagWeight = 0.2m,
        CategoryName = "錾刻ZB"
    });

    var html = renderer.Render(plan);

    AssertContains(html, "label-root silver-template", "silver root class");
    AssertContains(html, "总重(g): 123.00", "silver rough weight");
    AssertContains(html, "执行标准QB/T2062 GB11887  标签约0.20g", "silver standard line");
    AssertContains(html, ">￥1299.00<", "silver price line");
    AssertContains(html, "附加:￥430.00", "silver additional price line");
    AssertNotContains(html, "地址:水贝金座一层1111民族工匠", "silver address omitted");
    AssertNotContains(html, "标签约<span class=\"number\">0.20</span>g", "silver qr note omitted");
}

static void TestNativeDrawingPlanSilverTemplate()
{
    var labelPlan = new LabelRenderPlanner().CreatePlan(new LabelItem
    {
        FactoryNo = 25003,
        IdentifierCode = "100003593",
        ProductName = "足银镀金串珠-四方拉丝隔珠11.5mm41镶四方拉丝",
        WeightCategory = "净金重",
        FinishedProductWeight = 123m,
        RoughWeight = 123m,
        SalesCode = "60318000ZB60",
        GoldPurity = "含金量999‰",
        Address = "水贝金座一层1111民族工匠",
        Price = 1299m,
        AdditionalPrice = 430m,
        TagWeight = 0.2m,
        CategoryName = "錾刻ZB",
        FinishedProductPartVO =
        [
            new LabelPartItem { CategoryName = "錾刻ZB", PartWeight = 123.00m },
            new LabelPartItem { CategoryName = "素金PD", PartWeight = 330.45m }
        ],
        AdditionalRemark = "翡翠2",
        InlayWeight = 0.12m,
        RopeWeight = 0.30m,
        FinishedProductNote = "1111111"
    });

    var drawingPlan = new NativeLabelDrawingPlanner().CreatePlan(labelPlan);

    AssertEqual(LabelTemplateKey.Silver80x30, labelPlan.TemplateKey, "TemplateKey");
    AssertTrue(drawingPlan.Commands.Any(command => command.Text == "总重(g): "), "silver rough weight label");
    AssertTrue(drawingPlan.Commands.Any(command => command.Text == "执行标准QB/T2062 GB11887  标签约0.20g"), "silver standard line");
    AssertTrue(drawingPlan.Commands.Any(command => command.Text == "￥1299.00"), "silver price line");
    AssertTrue(drawingPlan.Commands.Any(command => command.Text == "附加:￥430.00"), "silver additional price line");
    var silverWeightValues = drawingPlan.Commands
        .Where(command => command.Text == "123.00" && command.X >= 18m && command.Y < 10m)
        .ToList();
    AssertEqual(2, silverWeightValues.Count, "SilverWeightValues.Count");
    AssertTrue(silverWeightValues.All(command => command.FontSizePt >= 5.2m), "silver weight value font is enlarged");
    AssertTrue(!drawingPlan.Commands.Any(command => command.Text == labelPlan.AddressText), "silver omits address line");
    AssertTrue(drawingPlan.Commands.Any(command => command.Text.Contains("錾刻ZB:123.00", StringComparison.Ordinal)), "silver part without grams suffix");
    AssertEqual(labelPlan.IdentifierText, drawingPlan.Commands[^1].Text, "BottomCode.Text");
    AssertEqual(90m, drawingPlan.Commands[^1].RotationDegrees, "BottomCode.RotationDegrees");
}

static void TestNativeDrawingPlanSilverTemplateOverrides()
{
    var labelPlan = new LabelRenderPlanner().CreatePlan(new LabelItem
    {
        FactoryNo = 25003,
        IdentifierCode = "100003593",
        ProductName = "足银镀金串珠",
        WeightCategory = "净金重",
        FinishedProductWeight = 123m,
        RoughWeight = 123m,
        SalesCode = "60318000ZB60",
        Price = 1299m,
        AdditionalPrice = 430m,
        CategoryName = "素金PD"
    });
    var overrides = new Dictionary<string, TemplateElementOverride>
    {
        ["roughWeightValue"] = new()
        {
            X = 19.4m,
            Y = 9.2m,
            FontSizePt = 5.6m,
            Bold = false
        }
    };

    var drawingPlan = new NativeLabelDrawingPlanner().CreatePlan(labelPlan, offset: null, overrides);
    var roughWeightValueCommand = drawingPlan.Commands.Single(command => command.ElementKey == "roughWeightValue");

    AssertEqual(19.4m, roughWeightValueCommand.X, "RoughWeightValue.X");
    AssertEqual(9.2m, roughWeightValueCommand.Y, "RoughWeightValue.Y");
    AssertEqual(5.6m, roughWeightValueCommand.FontSizePt, "RoughWeightValue.FontSizePt");
    AssertEqual(false, roughWeightValueCommand.Bold, "RoughWeightValue.Bold");
}

static void TestNativeDrawingPlanKeepsFooterBelowPartRows()
{
    var labelPlan = new LabelRenderPlanner().CreatePlan(new LabelItem
    {
        IdentifierCode = "100003593",
        ProductName = "18K金吊坠-菩提莲华白度母佑圣坠A款25mmFP004",
        WeightCategory = "鍑€閲戦噸",
        FinishedProductWeight = 123m,
        RoughWeight = 123m,
        SalesCode = "60318000ZB60",
        GoldPurity = "含金量999‰",
        Address = "姘磋礉閲戝骇涓€灞?111姘戞棌宸ュ尃",
        AdditionalPrice = 430m,
        AdditionalRemark = "缈＄繝2",
        InlayWeight = 0.12m,
        RopeWeight = 0.30m,
        FinishedProductNote = "1111111",
        FinishedProductPartVO =
        [
            new LabelPartItem { CategoryName = "閷惧埢ZB", PartWeight = 123.00m },
            new LabelPartItem { CategoryName = "绱犻噾PD", PartWeight = 330.45m },
            new LabelPartItem { CategoryName = "閷惧埢ZB", PartWeight = 123.00m },
            new LabelPartItem { CategoryName = "绱犻噾PD", PartWeight = 330.45m },
            new LabelPartItem { CategoryName = "閷惧埢ZB", PartWeight = 123.00m },
            new LabelPartItem { CategoryName = "閷惧埢ZB", PartWeight = 123.00m },
            new LabelPartItem { CategoryName = "閷惧埢ZB", PartWeight = 123.00m }
        ]
    });

    var drawingPlan = new NativeLabelDrawingPlanner().CreatePlan(labelPlan);
    var partCommands = drawingPlan.Commands
        .Where(command => command.Text.Contains("閷惧埢ZB:", StringComparison.Ordinal)
            || command.Text.Contains("绱犻噾PD:", StringComparison.Ordinal))
        .ToList();
    var footerCommand = drawingPlan.Commands.Single(command => command.Text.Contains("缈＄繝2", StringComparison.Ordinal));
    var productNameCommand = drawingPlan.Commands.Single(command => command.Text == labelPlan.ProductName);
    var additionalPriceCommand = drawingPlan.Commands.Single(command => command.Text == "附加:￥430.00");
    var bottomCodeCommand = drawingPlan.Commands[^1];
    var lastPartBottom = partCommands.Max(command => command.Y + command.Height);

    AssertTrue(productNameCommand.Width >= 16.3m, "ProductName uses available width before split line");
    AssertEqual(3, productNameCommand.MaxLines, "DenseProductName.MaxLines");
    AssertTrue(additionalPriceCommand.X >= 12m, "AdditionalPrice starts after sales code");
    AssertTrue(footerCommand.Y >= lastPartBottom + 0.2m, "Footer starts below part rows");
    AssertTrue(footerCommand.Width >= 22.4m, "Footer uses available width before vertical code");
    AssertTrue(footerCommand.Y + footerCommand.Height <= drawingPlan.PaperSize.HeightMm, "Footer stays inside paper");
    AssertEqual(labelPlan.IdentifierText, bottomCodeCommand.Text, "BottomCode.Text");
    AssertEqual(90m, bottomCodeCommand.RotationDegrees, "BottomCode.RotationDegrees");
    AssertTrue(bottomCodeCommand.X >= 22m, "BottomCode.X places code at right side");
    AssertTrue(bottomCodeCommand.Y >= 15m, "BottomCode.Y places code as one vertical line");
    AssertTrue(drawingPlan.Commands.Where(command => command.Type == NativeDrawCommandType.Text).Max(command => command.FontSizePt) <= 4.6m, "Dense label uses compact overall font size");
    AssertTrue(partCommands.All(command => command.FontSizePt <= 4.0m), "Dense part rows use compact font size");
    AssertTrue(additionalPriceCommand.FontSizePt <= 4.2m, "Additional price uses compact font size");
}

static void TestNativeDrawingPlanOffset()
{
    var labelPlan = new LabelRenderPlanner().CreatePlan(new LabelItem
    {
        IdentifierCode = "10000000789",
        ProductName = "瓒抽噾闀跺祵鍚婂潬",
        WeightCategory = "成品重",
        FinishedProductWeight = 3.2m,
        RoughWeight = 3.56m,
        SalesCode = "XS-001",
        GoldPurity = "瓒抽噾999",
        Address = "娣卞湷姘磋礉"
    });
    var drawingPlan = new NativeLabelDrawingPlanner().CreatePlan(labelPlan, new LabelOffset(1.2m, -0.5m));

    AssertEqual(1.2m, drawingPlan.Commands[0].X, "Commands[0].X");
    AssertEqual(-0.5m, drawingPlan.Commands[0].Y, "Commands[0].Y");
    AssertEqual(3.0m, drawingPlan.Commands[2].X, "Commands[2].X");
    AssertEqual(0.5m, drawingPlan.Commands[2].Y, "Commands[2].Y");
    AssertEqual(9m, drawingPlan.Commands[2].Width, "Commands[2].Width");
    AssertEqual(23.7m, drawingPlan.Commands[^1].X, "LastCommand.X");
    AssertEqual(15.1m, drawingPlan.Commands[^1].Y, "LastCommand.Y");
    AssertEqual(90m, drawingPlan.Commands[^1].RotationDegrees, "LastCommand.RotationDegrees");
}

static void TestPrintUnitConverter()
{
    var converter = new PrintUnitConverter(300m);

    AssertEqual(315, converter.MillimetersToHundredthsInch(80m), "80mm hundredths");
    AssertEqual(118, converter.MillimetersToHundredthsInch(30m), "30mm hundredths");
    AssertEqual(945, converter.MillimetersToPixels(80m), "80mm pixels");
    AssertEqual(354, converter.MillimetersToPixels(30m), "30mm pixels");
    AssertEqual(12, converter.MillimetersToPixels(1m), "1mm pixels");
}

static void TestQrCodeMatrixRenderer()
{
    var renderer = new QrCodeMatrixRenderer();

    var matrix = renderer.Render("10000000789");

    AssertTrue(matrix.Size >= 21, "matrix.Size");
    AssertTrue(matrix.DarkModuleCount > 80, "matrix.DarkModuleCount");
    AssertTrue(matrix.DarkModuleCount < matrix.Size * matrix.Size, "matrix has light modules");
    AssertTrue(!matrix.HasDarkModule(-1, 0), "out of range is light");
}

static void TestApiResponseEnvelope()
{
    var response = ApiResponse<HealthInfo>.Ok(new HealthInfo(true, "zytxt-dotnet-print", "http"));

    AssertEqual(true, response.Success, "Success");
    AssertEqual("OK", response.Code, "Code");
    AssertEqual("", response.Message, "Message");
    AssertEqual(true, response.Data.Ready, "Data.Ready");
    AssertEqual("zytxt-dotnet-print", response.Data.Service, "Data.Service");
    AssertEqual("http", response.Data.Protocol, "Data.Protocol");
}

static void TestPrintJobResultAccepted()
{
    var result = PrintJobResult.CreateAccepted("job-1", "request-1", 2);

    AssertEqual("job-1", result.JobId, "JobId");
    AssertEqual("request-1", result.RequestId, "RequestId");
    AssertEqual("queued", result.Status, "Status");
    AssertEqual(2, result.Accepted, "Accepted");
    AssertEqual(2, result.Total, "Total");
    AssertEqual(0, result.Printed, "Printed");
    AssertEqual(0, result.Failed, "Failed");
    AssertEqual(2, result.Pending, "Pending");
}

static void AssertEqual<T>(T expected, T actual, string name)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{name}: expected {expected}, got {actual}");
    }
}

static void AssertContains(string text, string expected, string name)
{
    if (!text.Contains(expected, StringComparison.Ordinal))
    {
        throw new InvalidOperationException($"{name}: expected text to contain {expected}");
    }
}

static void AssertNotContains(string text, string unexpected, string name)
{
    if (text.Contains(unexpected, StringComparison.Ordinal))
    {
        throw new InvalidOperationException($"{name}: expected text not to contain {unexpected}");
    }
}

static void AssertTrue(bool condition, string name)
{
    if (!condition)
    {
        throw new InvalidOperationException($"{name}: expected true");
    }
}

static FormCollection BuildForm(Dictionary<string, string> values)
{
    return new FormCollection(values.ToDictionary(
        pair => pair.Key,
        pair => new StringValues(pair.Value)));
}

static int CountDarkPixels(Bitmap bitmap)
{
    var darkPixels = 0;
    for (var y = 0; y < bitmap.Height; y += 3)
    {
        for (var x = 0; x < bitmap.Width; x += 3)
        {
            var pixel = bitmap.GetPixel(x, y);
            if (pixel.R < 80 && pixel.G < 80 && pixel.B < 80)
            {
                darkPixels++;
            }
        }
    }

    return darkPixels;
}
