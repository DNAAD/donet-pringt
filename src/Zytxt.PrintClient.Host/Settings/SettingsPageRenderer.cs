using System.Net;
using Zytxt.PrintClient.Core.Api;
using Zytxt.PrintClient.Core.Settings;

namespace Zytxt.PrintClient.Host.Settings;

public sealed class SettingsPageRenderer
{
    public string Render(PrintClientSettings settings, IReadOnlyList<PrinterInfo> printers, string statusMessage = "")
    {
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

        return $$"""
<!doctype html>
<html lang="zh-CN">
<head>
  <meta charset="utf-8">
  <title>.NET 打印助手设置</title>
  <style>
    * { box-sizing: border-box; }
    body { margin: 0; padding: 24px; font-family: "Microsoft YaHei", Arial, sans-serif; background: #eef2f6; color: #1f2937; }
    main { max-width: 1180px; }
    h1 { margin: 0 0 18px; font-size: 22px; }
    h2 { margin: 0 0 12px; font-size: 16px; }
    .layout { display: grid; grid-template-columns: 360px minmax(0, 1fr); gap: 18px; align-items: start; }
    .panel { padding: 18px; background: #fff; border: 1px solid #d9e2ec; border-radius: 8px; }
    form { display: grid; gap: 14px; }
    label { display: grid; gap: 6px; font-size: 14px; font-weight: 600; }
    select, input { height: 36px; padding: 0 10px; border: 1px solid #b8c5d2; border-radius: 6px; font-size: 14px; }
    .grid { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
    .actions { display: flex; flex-wrap: wrap; gap: 10px; align-items: center; }
    button, .button { display: inline-flex; align-items: center; justify-content: center; min-width: 110px; height: 36px; padding: 0 14px; border: 0; border-radius: 6px; background: #2563eb; color: #fff; font-weight: 600; text-decoration: none; cursor: pointer; }
    .button.secondary { background: #475569; }
    button.danger { background: #b45309; }
    .hint { margin: 8px 0 0; color: #64748b; font-size: 13px; line-height: 1.6; }
    .status { margin: 0 0 14px; padding: 10px 12px; border: 1px solid #a7f3d0; background: #ecfdf5; color: #065f46; border-radius: 6px; font-size: 14px; }
    .preview-wrap { overflow: auto; padding: 16px; background: #f8fafc; border: 1px dashed #cbd5e1; border-radius: 6px; }
    .preview { display: block; width: 80mm; height: 30mm; background: #fff; border: 1px solid #94a3b8; image-rendering: auto; }
    .metrics { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 10px; margin-top: 12px; }
    .metric { padding: 10px; border: 1px solid #e2e8f0; border-radius: 6px; background: #fff; font-size: 13px; }
    .metric strong { display: block; margin-bottom: 4px; color: #0f172a; }
    @media (max-width: 900px) { .layout { grid-template-columns: 1fr; } }
  </style>
</head>
<body>
  <main>
    <h1>.NET 打印助手校准</h1>
    {{status}}
    <div class="layout">
      <section class="panel">
        <h2>打印设置</h2>
        <form method="post" action="/settings-ui">
          <label>
            默认打印机
            <select name="defaultPrinter">
              <option value="">使用 Windows 默认打印机</option>
              {{options}}
            </select>
          </label>
          <div class="grid">
            <label>
              X 偏移(mm，负数向左)
              <input name="offsetX" type="number" step="0.1" value="{{settings.LabelOffset.X}}">
            </label>
            <label>
              Y 偏移(mm，纸宽方向)
              <input name="offsetY" type="number" step="0.1" value="{{settings.LabelOffset.Y}}">
            </label>
          </div>
          <p class="hint">偏移会同时影响 GDI 预览和真实打印；真实打印使用 80mm x 30mm 标签画布直接 GDI 绘制。</p>
          <div class="actions">
            <button class="diagnostic" type="submit" formaction="/settings-ui/test-axis-print?mode=direct80" formmethod="post">Axis B</button>
            <button class="diagnostic" type="submit" formaction="/settings-ui/test-axis-print?mode=landscape80" formmethod="post">Axis C</button>
            <button class="diagnostic" type="submit" formaction="/settings-ui/test-axis-print?mode=feed0" formmethod="post">Axis D</button>
            <button type="submit">保存设置</button>
            <button class="danger" type="submit" formaction="/settings-ui/test-print" formmethod="post">测试打印</button>
            <button class="diagnostic" type="submit" formaction="/settings-ui/test-axis-print" formmethod="post">坐标测试打印</button>
            <a class="button secondary" href="/settings-ui">刷新预览</a>
            <a class="button secondary" href="/diagnostics/axis.png">坐标预览</a>
            <a class="button secondary" href="/diagnostics/axis-print-path.png">打印路径预览</a>
          </div>
        </form>
        <p class="hint">保存后，/print/tag 不需要传 printerName，.NET Host 会优先使用这里选择的打印机和标签偏移。</p>
        <p class="hint">测试打印会使用内置密集样例并执行真实打印，请先确认打印机和纸张已就绪。</p>
      </section>

      <section class="panel">
        <h2>GDI 预览</h2>
        <div class="preview-wrap">
          <img class="preview" src="/preview/gdi.png?v={{previewVersion}}" alt="GDI 标签预览">
        </div>
        <div class="metrics">
          <div class="metric"><strong>纸张</strong>80mm x 30mm 标签画布</div>
          <div class="metric"><strong>X 偏移</strong>{{settings.LabelOffset.X}}mm</div>
          <div class="metric"><strong>Y 偏移</strong>{{settings.LabelOffset.Y}}mm</div>
        </div>
        <p class="hint">预览和真实打印使用相同的 GDI 坐标和绘制路径；真实打印会由程序内部对齐打印机驱动原点。</p>
      </section>
    </div>
  </main>
</body>
</html>
""";
    }

    private static string Encode(string value)
    {
        return WebUtility.HtmlEncode(value);
    }
}
