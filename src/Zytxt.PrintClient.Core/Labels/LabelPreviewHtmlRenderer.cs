using System.Net;
using System.Text;
using Zytxt.PrintClient.Core.Qr;

namespace Zytxt.PrintClient.Core.Labels;

public sealed class LabelPreviewHtmlRenderer
{
    private readonly QrCodeMatrixRenderer qrCodeRenderer = new();

    public string Render(LabelRenderPlan plan)
    {
        return $$"""
<!doctype html>
<html lang="zh-CN">
<head>
  <meta charset="utf-8">
  <title>80mm x 30mm 标签预览</title>
  <style>
    * { box-sizing: border-box; }
    body {
      margin: 0;
      padding: 24px;
      background: #f3f6f8;
      font-family: "Microsoft YaHei", "Arial", "SimHei", sans-serif;
      color: #000;
    }
    .page-title {
      margin: 0 0 16px;
      font-size: 16px;
      font-weight: 600;
    }
    .label-root {
      --label-qr-size-mm: 8mm;
      --label-product-font-pt: 4.8pt;
      --label-body-font-pt: 4.5pt;
      --label-line-height: 1.05;
      --label-rotate-font-pt: 4.8pt;
      position: relative;
      width: {{plan.PaperSize.WidthMm}}mm;
      height: {{plan.PaperSize.HeightMm}}mm;
      overflow: hidden;
      background: #fff;
      color: #000;
      box-shadow: 0 8px 24px rgba(15, 23, 42, 0.12);
      font-family: "Microsoft YaHei", "Arial", "SimHei", sans-serif;
    }
    .content-band {
      width: 26mm;
      height: 100%;
    }
    .top-row {
      display: grid;
      grid-template-columns: 9.2mm 15.2mm;
      gap: 1mm;
      height: 11mm;
    }
    .identifier-code {
      margin: 0;
      font-size: 4.2pt;
      font-weight: 700;
      line-height: 1;
      white-space: nowrap;
      overflow: hidden;
    }
    .qr-row {
      display: grid;
      grid-template-columns: 2mm 1fr;
      gap: 0.25mm;
      align-items: center;
    }
    .quality-mark {
      margin: 0;
      font-size: 5.2pt;
      line-height: 1.4;
      word-break: break-all;
      font-weight: 700;
    }
    .qr-box {
      width: var(--label-qr-size-mm);
      height: var(--label-qr-size-mm);
      display: grid;
      place-items: center;
    }
    .qr-code {
      width: var(--label-qr-size-mm);
      height: var(--label-qr-size-mm);
      display: block;
    }
    .qr-note {
      margin: 0;
      font-size: 4pt;
      line-height: 1;
      padding-left: 1.5mm;
      white-space: nowrap;
      font-weight: 700;
    }
    .top-right {
      display: grid;
      grid-template-rows: 6.7mm 3.8mm;
      gap: 0.15mm;
    }
    .product-name {
      margin: 0;
      display: -webkit-box;
      -webkit-box-orient: vertical;
      -webkit-line-clamp: 3;
      overflow: hidden;
      text-overflow: ellipsis;
      font-size: var(--label-product-font-pt);
      font-weight: 500;
      line-height: 1.2;
      word-break: break-all;
    }
    .metric-line,
    .middle-line,
    .detail-line,
    .footer-line {
      margin: 0;
      font-size: var(--label-body-font-pt);
      line-height: var(--label-line-height);
      font-weight: 700;
    }
    .middle-line {
      white-space: nowrap;
      text-overflow: ellipsis;
    }
    .sales-code {
      margin: 0;
      display: flex;
    }
    .sales-code p {
      margin: 0;
      font-size: 4.8pt;
      font-weight: 700;
    }
    .additional-price {
      padding-left: 1mm;
    }
    .detail-grid {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: 0;
      min-height: 6mm;
      width: 23mm;
    }
    .detail-column {
      display: grid;
      grid-auto-rows: 1.8mm;
      gap: 0.25mm;
    }
    .detail-line {
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }
    .footer-box {
      width: 22mm;
    }
    .footer-line {
      line-height: 1.1;
      word-break: break-all;
      text-overflow: ellipsis;
    }
    .rotate-code {
      position: absolute;
      left: 22.5mm;
      top: 16.6mm;
      margin: 0;
      writing-mode: vertical-rl;
      transform: rotate(180deg);
      font-size: var(--label-rotate-font-pt);
      font-weight: 700;
    }
    .number {
      font-weight: 700;
    }
  </style>
</head>
<body>
  <h1 class="page-title">.NET POC 标签预览：80mm x 30mm</h1>
  <article class="{{RootClass(plan)}}">
    <section class="content-band">
      <section class="top-row">
        <div class="top-left">
          <p class="identifier-code number">{{Encode(plan.IdentifierText)}}</p>
          <div class="qr-row">
            <p class="quality-mark">合格证</p>
            <div class="qr-box">{{RenderQrSvg(plan.QrPayload)}}</div>
          </div>
          {{RenderQrNote(plan)}}
        </div>
        <div class="top-right">
          <p class="product-name">{{Encode(plan.ProductName)}}</p>
          <div>
            <p class="metric-line">{{Encode(plan.FinishedWeightText)}}</p>
            <p class="metric-line">{{Encode(plan.RoughWeightText)}}</p>
          </div>
        </div>
      </section>
      <section class="middle-line">{{Encode(RenderStandardLine(plan))}}</section>
      {{RenderAddressLine(plan)}}
      {{RenderPriceLine(plan)}}
      <section class="sales-code">
        <p class="number">{{Encode(plan.SalesCode)}}</p>
        {{RenderAdditionalPrice(plan.AdditionalPriceText)}}
      </section>
      <section class="detail-grid">
        <div class="detail-column">{{RenderPartColumn(plan.Parts, 0)}}</div>
        <div class="detail-column">{{RenderPartColumn(plan.Parts, 1)}}</div>
      </section>
      <section class="footer-box">
        <p class="footer-line">{{Encode(plan.FooterText)}}</p>
      </section>
    </section>
    <p class="rotate-code number">{{Encode(plan.IdentifierText)}}</p>
  </article>
</body>
</html>
""";
    }

    private static string RenderAdditionalPrice(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? ""
            : $"<p class=\"additional-price\">{Encode(value)}</p>";
    }

    private static string RenderStandardLine(LabelRenderPlan plan)
    {
        return plan.TemplateKey == LabelTemplateKey.Silver80x30
            ? $"{plan.StandardText}  标签约{plan.TagWeightText}g"
            : plan.StandardText;
    }

    private static string RenderPriceLine(LabelRenderPlan plan)
    {
        return plan.TemplateKey == LabelTemplateKey.Silver80x30 && !string.IsNullOrWhiteSpace(plan.PriceText)
            ? $"<section class=\"middle-line price-line\"><span class=\"number\">{Encode(plan.PriceText)}</span></section>"
            : "";
    }

    private static string RootClass(LabelRenderPlan plan)
    {
        return plan.TemplateKey == LabelTemplateKey.Silver80x30
            ? "label-root silver-template"
            : "label-root";
    }

    private static string RenderQrNote(LabelRenderPlan plan)
    {
        return plan.TemplateKey == LabelTemplateKey.Silver80x30
            ? ""
            : "<p class=\"qr-note\">标签约<span class=\"number\">0.20</span>g</p>";
    }

    private static string RenderAddressLine(LabelRenderPlan plan)
    {
        return plan.TemplateKey == LabelTemplateKey.Silver80x30
            ? ""
            : $"<section class=\"middle-line address-line\">{Encode(plan.AddressText)}</section>";
    }

    private static string RenderPartColumn(IReadOnlyList<LabelPartRenderPlan> parts, int parity)
    {
        var builder = new StringBuilder();
        for (var index = parity; index < parts.Count; index += 2)
        {
            var part = parts[index];
            builder.Append("<p class=\"detail-line\">");
            builder.Append(Encode(part.CategoryName));
            builder.Append(":<span class=\"number\">");
            builder.Append(Encode(part.PartWeightText));
            builder.Append("</span></p>");
        }

        return builder.ToString();
    }

    private string RenderQrSvg(string payload)
    {
        var matrix = qrCodeRenderer.Render(payload);
        var builder = new StringBuilder();
        builder.Append("<svg class=\"qr-code\" data-qr-payload=\"");
        builder.Append(Encode(payload));
        builder.Append("\" viewBox=\"0 0 ");
        builder.Append(matrix.Size);
        builder.Append(' ');
        builder.Append(matrix.Size);
        builder.Append("\" xmlns=\"http://www.w3.org/2000/svg\" shape-rendering=\"crispEdges\">");
        builder.Append("<rect width=\"100%\" height=\"100%\" fill=\"#fff\"/>");

        for (var y = 0; y < matrix.Size; y++)
        {
            for (var x = 0; x < matrix.Size; x++)
            {
                if (!matrix.HasDarkModule(x, y))
                {
                    continue;
                }

                builder.Append("<rect x=\"");
                builder.Append(x);
                builder.Append("\" y=\"");
                builder.Append(y);
                builder.Append("\" width=\"1\" height=\"1\" fill=\"#111\"/>");
            }
        }

        builder.Append("</svg>");
        return builder.ToString();
    }

    private static string Encode(string value)
    {
        return WebUtility.HtmlEncode(value);
    }
}
