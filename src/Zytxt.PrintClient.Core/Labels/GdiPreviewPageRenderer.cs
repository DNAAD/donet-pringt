namespace Zytxt.PrintClient.Core.Labels;

public sealed class GdiPreviewPageRenderer
{
    public string Render(string imagePath, string htmlPreviewPath)
    {
        return $$"""
<!doctype html>
<html lang="zh-CN">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>.NET GDI 标签预览</title>
  <style>
    * {
      box-sizing: border-box;
    }
    body {
      margin: 0;
      min-height: 100vh;
      padding: 24px;
      background: #f3f6f8;
      color: #111827;
      font-family: "Microsoft YaHei", "Arial", "SimHei", sans-serif;
    }
    h1 {
      margin: 0 0 16px;
      font-size: 16px;
      font-weight: 600;
    }
    .preview-frame {
      display: inline-block;
      background: #fff;
      box-shadow: 0 8px 24px rgba(15, 23, 42, 0.12);
      line-height: 0;
    }
    .gdi-preview {
      display: block;
      width: 80mm;
      height: 30mm;
      image-rendering: pixelated;
    }
    .toolbar {
      margin-top: 14px;
      display: flex;
      gap: 12px;
      align-items: center;
      font-size: 13px;
    }
    a {
      color: #0f766e;
      text-decoration: none;
      font-weight: 600;
    }
  </style>
</head>
<body>
  <h1>.NET GDI 标签预览：80mm x 30mm 标签画布</h1>
  <div class="preview-frame">
    <img class="gdi-preview" src="{{EscapeAttribute(imagePath)}}" alt="GDI 标签预览">
  </div>
  <div class="toolbar">
    <a href="{{EscapeAttribute(imagePath)}}">打开 PNG</a>
    <a href="{{EscapeAttribute(htmlPreviewPath)}}">HTML 对照</a>
  </div>
</body>
</html>
""";
    }

    private static string EscapeAttribute(string value)
    {
        return value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);
    }
}
