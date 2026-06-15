using System.Globalization;
using System.Net;
using System.Text.Json;
using Zytxt.PrintClient.Core.Api;
using Zytxt.PrintClient.Core.Settings;

namespace Zytxt.PrintClient.Host.Settings;

public sealed class SettingsPageRenderer
{
    public string Render(
        PrintClientSettings settings,
        IReadOnlyList<PrinterInfo> printers,
        string statusMessage = "",
        string previewTemplate = "default")
    {
        var template = NormalizePreviewTemplate(previewTemplate);
        var defaultTemplateSelected = template == "default" ? " selected" : "";
        var silverTemplateSelected = template == "silver" ? " selected" : "";
        var options = string.Join(
            Environment.NewLine,
            printers.Select(printer =>
            {
                var selected = string.Equals(printer.Name, settings.DefaultPrinter, StringComparison.OrdinalIgnoreCase)
                    ? " selected"
                    : "";
                var label = string.IsNullOrWhiteSpace(printer.DisplayName) ? printer.Name : printer.DisplayName;
                return $"""<option value="{Encode(printer.Name)}"{selected}>{Encode(label)}</option>""";
            }));
        var previewVersion = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var status = string.IsNullOrWhiteSpace(statusMessage)
            ? ""
            : $"""<div class="status">{Encode(statusMessage)}</div>""";
        var allowedOriginsText = string.Join(Environment.NewLine, settings.AllowedOrigins);
        var templateEditor = RenderTemplateEditor(settings, template);
        var templateEditorState = BuildTemplateEditorStateJson(settings, template);

        return $$"""
<!doctype html>
<html lang="zh-CN">
<head>
  <meta charset="utf-8">
  <title>.NET 打印助手设置</title>
  <style>
    * { box-sizing: border-box; }
    :root { color-scheme: light; --bg: #f3f6fa; --panel: #fff; --ink: #172033; --muted: #647086; --line: #d9e2ec; --line-strong: #b9c7d6; --blue: #1d4ed8; --blue-soft: #eff6ff; --amber: #a16207; --slate: #475569; }
    body { margin: 0; padding: 24px; font-family: "Microsoft YaHei", Arial, sans-serif; background: var(--bg); color: var(--ink); }
    main { width: min(1480px, calc(100vw - 48px)); margin: 0 auto; }
    h1 { margin: 0; font-size: 24px; line-height: 1.25; }
    h2 { margin: 0 0 14px; font-size: 16px; line-height: 1.35; }
    .page-head { display: flex; justify-content: space-between; align-items: end; gap: 18px; margin-bottom: 18px; }
    .page-subtitle { margin: 6px 0 0; color: var(--muted); font-size: 13px; }
    .settings-shell { display: grid; grid-template-columns: minmax(300px, 360px) minmax(470px, 1fr) minmax(300px, 340px); gap: 16px; align-items: start; }
    .settings-column, .template-sidebar { display: grid; gap: 14px; min-width: 0; }
    .panel, .preview-stage { padding: 18px; background: var(--panel); border: 1px solid var(--line); border-radius: 8px; box-shadow: 0 12px 32px rgba(15, 23, 42, 0.06); }
    .panel-head { display: flex; justify-content: space-between; align-items: center; gap: 10px; margin-bottom: 14px; }
    .panel-head h2 { margin: 0; }
    .badge { display: inline-flex; align-items: center; height: 24px; padding: 0 9px; border-radius: 999px; background: var(--blue-soft); color: #1e40af; font-size: 12px; font-weight: 700; white-space: nowrap; }
    label { display: grid; gap: 6px; font-size: 13px; font-weight: 700; color: #25324a; }
    select, input, textarea { width: 100%; border: 1px solid var(--line-strong); border-radius: 6px; background: #fff; color: var(--ink); font-size: 14px; }
    select, input { height: 38px; padding: 0 10px; }
    input[type="checkbox"] { width: 18px; height: 18px; padding: 0; justify-self: start; }
    textarea { min-height: 112px; padding: 9px 10px; resize: vertical; line-height: 1.45; font-family: Consolas, "Microsoft YaHei", Arial, sans-serif; }
    fieldset { margin: 0; border: 1px solid var(--line); border-radius: 8px; padding: 14px; }
    legend { padding: 0 6px; font-weight: 800; color: #111827; }
    .grid { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
    .stack { display: grid; gap: 12px; }
    .actions { display: flex; flex-wrap: wrap; gap: 9px; align-items: center; }
    .actions-primary { padding-top: 2px; }
    .actions-diagnostics { margin-top: 12px; padding-top: 12px; border-top: 1px solid var(--line); }
    button, .button { display: inline-flex; align-items: center; justify-content: center; min-width: 96px; height: 36px; padding: 0 13px; border: 0; border-radius: 6px; background: var(--blue); color: #fff; font-size: 13px; font-weight: 700; text-decoration: none; cursor: pointer; white-space: nowrap; }
    button.diagnostic, .button.secondary { background: var(--slate); }
    button.danger { background: var(--amber); }
    .hint { margin: 8px 0 0; color: var(--muted); font-size: 12px; line-height: 1.6; }
    .status { margin: 0 0 14px; padding: 10px 12px; border: 1px solid #a7f3d0; background: #ecfdf5; color: #065f46; border-radius: 6px; font-size: 14px; }
    .preview-tools { display: flex; justify-content: space-between; align-items: center; gap: 12px; margin-bottom: 14px; }
    .preview-controls { display: flex; align-items: center; gap: 10px; margin-bottom: 12px; color: #334155; font-size: 13px; }
    .preview-controls label { display: flex; align-items: center; gap: 8px; font-size: 13px; white-space: nowrap; }
    .preview-controls input[type="range"] { width: 160px; height: auto; padding: 0; }
    .preview-viewport { min-height: 30mm; display: grid; place-items: start center; transition: min-height 120ms ease; }
    .preview-wrap { overflow: auto; padding: 22px; background: linear-gradient(135deg, #f8fafc, #eef4fb); border: 1px dashed #cbd5e1; border-radius: 8px; }
    .preview { display: block; width: 80mm; height: 30mm; margin: 0 auto; background: #fff; border: 1px solid #94a3b8; box-shadow: 0 14px 28px rgba(15, 23, 42, 0.14); image-rendering: auto; transform-origin: top center; transition: transform 120ms ease; }
    .metrics { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 10px; margin-top: 14px; }
    .metric { min-height: 62px; padding: 10px; border: 1px solid #e2e8f0; border-radius: 6px; background: #fff; color: #334155; font-size: 13px; line-height: 1.35; }
    .metric strong { display: block; margin-bottom: 4px; color: #0f172a; font-size: 12px; }
    @media (max-width: 1200px) { .settings-shell { grid-template-columns: minmax(300px, 360px) minmax(0, 1fr); } .template-sidebar { grid-column: 1 / -1; grid-template-columns: 1fr; } }
    @media (max-width: 820px) { body { padding: 16px; } main { width: 100%; } .page-head { display: block; } .settings-shell { grid-template-columns: 1fr; } .metrics { grid-template-columns: 1fr 1fr; } .grid { grid-template-columns: 1fr; } }
  </style>
</head>
<body>
  <main>
    <div class="page-head">
      <div>
        <h1>.NET 打印助手设置</h1>
        <p class="page-subtitle">面向 Godex G530 等 80mm x 30mm 标签打印的本地 GDI 工作台</p>
      </div>
      <span class="badge">{{TemplateLabel(template)}}</span>
    </div>
    {{status}}
    <form class="settings-shell" method="post" action="/settings-ui">
      <div class="settings-column settings-column-left">
        <section class="panel">
          <div class="panel-head">
            <h2>打印机与连接</h2>
            <span class="badge">本机服务</span>
          </div>
          <label>
            默认打印机
            <select name="defaultPrinter">
              <option value="">使用 Windows 默认打印机</option>
              {{options}}
            </select>
          </label>
          <label>
            GDI 预览模板
            <select name="previewTemplate">
              <option value="default"{{defaultTemplateSelected}}>默认标签</option>
              <option value="silver"{{silverTemplateSelected}}>银标签 factoryNo=25003</option>
            </select>
          </label>
          <label>
            CORS 允许来源(每行一个完整 Origin)
            <textarea name="allowedOrigins" rows="4" placeholder="https://www.zy-taoxiaoti.com&#10;http://114.132.160.27">{{Encode(allowedOriginsText)}}</textarea>
          </label>
          <p class="hint">线上网页调用本机打印服务时，需要在这里填写完整 Origin；localhost 和 127.0.0.1 默认允许。</p>
          <p class="hint">保存后，/print/tag 不需要传 printerName，.NET Host 会优先使用这里选择的打印机。</p>
        </section>

        <section class="panel">
          <div class="panel-head">
            <h2>校准与测试</h2>
            <span class="badge">80 x 30mm</span>
          </div>
          <div class="grid">
            <label>
              X 偏移(mm，负数向左)
              <input name="offsetX" type="number" step="0.1" value="{{FormatDecimal(settings.LabelOffset.X)}}">
            </label>
            <label>
              Y 偏移(mm，纸宽方向)
              <input name="offsetY" type="number" step="0.1" value="{{FormatDecimal(settings.LabelOffset.Y)}}">
            </label>
          </div>
          <p class="hint">偏移和模板元素覆盖会同时影响 GDI 预览和真实打印。</p>
          <div class="actions actions-primary">
            <button type="submit" formaction="/settings-ui?previewTemplate={{template}}" formmethod="post">保存设置</button>
            <button class="danger" type="submit" formaction="/settings-ui/test-print" formmethod="post">测试打印</button>
            <button class="diagnostic" type="submit" formaction="/settings-ui/test-axis-print" formmethod="post">坐标测试打印</button>
          </div>
          <div class="actions actions-diagnostics">
            <button class="diagnostic" type="submit" formaction="/settings-ui/test-axis-print?mode=direct80" formmethod="post">Axis B</button>
            <button class="diagnostic" type="submit" formaction="/settings-ui/test-axis-print?mode=landscape80" formmethod="post">Axis C</button>
            <button class="diagnostic" type="submit" formaction="/settings-ui/test-axis-print?mode=feed0" formmethod="post">Axis D</button>
            <a class="button secondary" href="/diagnostics/axis.png">坐标预览</a>
            <a class="button secondary" href="/diagnostics/axis-print-path.png">打印路径预览</a>
          </div>
          <p class="hint">测试打印会使用内置密集样例并执行真实打印，请先确认打印机和纸张已就绪。</p>
        </section>
      </div>

      <section class="preview-stage">
        <div class="preview-tools">
          <h2>GDI 预览</h2>
          <div class="actions">
            <a class="button secondary" href="/settings-ui?previewTemplate={{template}}">刷新预览</a>
            <a class="button secondary" href="/preview/native-plan?template={{template}}">绘制计划</a>
            <a class="button secondary" href="/preview/html?template={{template}}">HTML 对照</a>
          </div>
        </div>
        <div class="preview-controls">
          <label>
            预览缩放
            <input id="previewZoom" name="previewZoom" type="range" min="1" max="2" step="0.25" value="1">
          </label>
          <span id="previewZoomValue">100%</span>
        </div>
        <div class="preview-wrap">
          <div class="preview-viewport">
            <img class="preview" src="/preview/gdi.png?template={{template}}&v={{previewVersion}}" alt="GDI 标签预览">
          </div>
        </div>
        <div class="metrics">
          <div class="metric"><strong>纸张</strong>80mm x 30mm 标签画布</div>
          <div class="metric"><strong>X 偏移</strong>{{FormatDecimal(settings.LabelOffset.X)}}mm</div>
          <div class="metric"><strong>Y 偏移</strong>{{FormatDecimal(settings.LabelOffset.Y)}}mm</div>
          <div class="metric"><strong>模板</strong>{{TemplateLabel(template)}}</div>
        </div>
        <p class="hint">预览和真实打印使用相同的 GDI 坐标和绘制路径；真实打印会由程序内部对齐打印机驱动原点。</p>
      </section>

      <aside class="template-sidebar">
        <section class="panel">
          <div class="panel-head">
            <h2>模板元素</h2>
            <span class="badge">现有元素</span>
          </div>
          {{templateEditor}}
        </section>
      </aside>
    </form>
  </main>
  <script>
    const templateEditorState = {{templateEditorState}};
    const templateElementSelect = document.querySelector('[name="templateElementKey"]');
    const templateXInput = document.querySelector('[name="templateX"]');
    const templateYInput = document.querySelector('[name="templateY"]');
    const templateFontSizeInput = document.querySelector('[name="templateFontSizePt"]');
    const templateBoldInput = document.querySelector('[name="templateBold"]');
    const previewZoomInput = document.querySelector('[name="previewZoom"]');
    const previewZoomValue = document.getElementById('previewZoomValue');
    const previewImage = document.querySelector('.preview');
    const previewViewport = document.querySelector('.preview-viewport');
    function updateTemplateEditorFields() {
      const state = templateEditorState[templateElementSelect.value] || {};
      templateXInput.value = state.x || '';
      templateYInput.value = state.y || '';
      templateFontSizeInput.value = state.fontSizePt || '';
      templateBoldInput.checked = state.bold === true;
    }
    function updatePreviewZoom() {
      const zoom = Number(previewZoomInput.value || '1');
      previewImage.style.transform = `scale(${zoom})`;
      previewViewport.style.minHeight = `${30 * zoom}mm`;
      previewZoomValue.textContent = `${Math.round(zoom * 100)}%`;
    }
    templateElementSelect.addEventListener('change', updateTemplateEditorFields);
    previewZoomInput.addEventListener('input', updatePreviewZoom);
    updatePreviewZoom();
  </script>
</body>
</html>
""";
    }

    private static string RenderTemplateEditor(PrintClientSettings settings, string template)
    {
        const string selectedElementKey = "productName";
        var templateOverrides = settings.TemplateOverrides is not null
            && settings.TemplateOverrides.TryGetValue(template, out var overrides)
            ? overrides
            : new Dictionary<string, TemplateElementOverride>();
        var current = templateOverrides.TryGetValue(selectedElementKey, out var saved)
            ? saved
            : GetDefaultElementOverride(selectedElementKey);
        var boldChecked = current.Bold == true ? " checked" : "";

        return $$"""
          <fieldset>
            <legend>模板元素编辑</legend>
            <label>
              元素
              <select name="templateElementKey">
                {{RenderTemplateElementOptions(selectedElementKey)}}
              </select>
            </label>
            <div class="grid">
              <label>
                元素 X(mm)
                <input name="templateX" type="number" step="0.1" min="0" max="80" value="{{FormatDecimal(current.X)}}">
              </label>
              <label>
                元素 Y(mm)
                <input name="templateY" type="number" step="0.1" min="0" max="30" value="{{FormatDecimal(current.Y)}}">
              </label>
              <label>
                字号(pt)
                <input name="templateFontSizePt" type="number" step="0.1" min="1" max="12" value="{{FormatDecimal(current.FontSizePt)}}">
              </label>
              <label>
                加粗
                <input name="templateBold" type="checkbox"{{boldChecked}} value="true">
              </label>
            </div>
            <div class="actions actions-diagnostics">
              <button class="diagnostic" type="submit" name="templateReset" value="current" formaction="/settings-ui?previewTemplate={{template}}" formmethod="post">恢复当前元素默认值</button>
            </div>
            <p class="hint">第一版只调整已有元素的位置、字号和加粗；保存后会影响 GDI 预览和真实打印。</p>
          </fieldset>
""";
    }

    private static string RenderTemplateElementOptions(string selectedElementKey)
    {
        var elements = new (string Key, string Label)[]
        {
            ("identifier", "编号"),
            ("qualityMark", "合格证"),
            ("qrCode", "二维码"),
            ("qrNote", "二维码说明"),
            ("productName", "产品名"),
            ("finishedWeightLabel", "成品重标签"),
            ("finishedWeightValue", "成品重数值"),
            ("roughWeightLabel", "总重标签"),
            ("roughWeightValue", "总重数值"),
            ("standardText", "执行标准"),
            ("addressText", "地址"),
            ("priceText", "银标签价格"),
            ("salesCode", "销售码"),
            ("additionalPrice", "附加价"),
            ("partRow", "明细行"),
            ("footerText", "底部备注"),
            ("verticalIdentifier", "竖排编号")
        };

        return string.Join(Environment.NewLine, elements.Select(element =>
        {
            var selected = element.Key == selectedElementKey ? " selected" : "";
            return $"""<option value="{Encode(element.Key)}"{selected}>{Encode(element.Label)}</option>""";
        }));
    }

    private static TemplateElementOverride GetDefaultElementOverride(string elementKey)
    {
        if (elementKey == "productName")
        {
            return new TemplateElementOverride
            {
                X = 10.2m,
                Y = 0m,
                FontSizePt = 4.2m,
                Bold = false
            };
        }

        return new TemplateElementOverride
        {
            X = 0m,
            Y = 0m,
            FontSizePt = 4.2m,
            Bold = false
        };
    }

    private static string BuildTemplateEditorStateJson(PrintClientSettings settings, string template)
    {
        var templateOverrides = settings.TemplateOverrides is not null
            && settings.TemplateOverrides.TryGetValue(template, out var overrides)
            ? overrides
            : new Dictionary<string, TemplateElementOverride>();
        var state = new[]
            {
                "identifier",
                "qualityMark",
                "qrCode",
                "qrNote",
                "productName",
                "finishedWeightLabel",
                "finishedWeightValue",
                "roughWeightLabel",
                "roughWeightValue",
                "standardText",
                "addressText",
                "priceText",
                "salesCode",
                "additionalPrice",
                "partRow",
                "footerText",
                "verticalIdentifier"
            }
            .ToDictionary(
                elementKey => elementKey,
                elementKey =>
                {
                    var value = templateOverrides.TryGetValue(elementKey, out var saved)
                        ? Merge(GetDefaultElementOverride(elementKey), saved)
                        : GetDefaultElementOverride(elementKey);
                    return new
                    {
                        x = FormatDecimal(value.X),
                        y = FormatDecimal(value.Y),
                        fontSizePt = FormatDecimal(value.FontSizePt),
                        bold = value.Bold == true
                    };
                });

        return JsonSerializer.Serialize(state);
    }

    private static TemplateElementOverride Merge(TemplateElementOverride defaults, TemplateElementOverride saved)
    {
        return new TemplateElementOverride
        {
            X = saved.X ?? defaults.X,
            Y = saved.Y ?? defaults.Y,
            FontSizePt = saved.FontSizePt ?? defaults.FontSizePt,
            Bold = saved.Bold ?? defaults.Bold
        };
    }

    private static string Encode(string value)
    {
        return WebUtility.HtmlEncode(value);
    }

    private static string NormalizePreviewTemplate(string value)
    {
        return string.Equals(value, "silver", StringComparison.OrdinalIgnoreCase)
            ? "silver"
            : "default";
    }

    private static string TemplateLabel(string value)
    {
        return value == "silver" ? "银标签 factoryNo=25003" : "默认标签";
    }

    private static string FormatDecimal(decimal value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string FormatDecimal(decimal? value)
    {
        return value?.ToString("0.###", CultureInfo.InvariantCulture) ?? "";
    }
}
