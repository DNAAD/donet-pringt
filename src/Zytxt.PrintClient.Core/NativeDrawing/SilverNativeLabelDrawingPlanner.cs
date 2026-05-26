using Zytxt.PrintClient.Core.Labels;
using Zytxt.PrintClient.Core.Settings;

namespace Zytxt.PrintClient.Core.NativeDrawing;

internal sealed class SilverNativeLabelDrawingPlanner
{
    public NativeLabelDrawingPlan CreatePlan(LabelRenderPlan labelPlan, LabelOffset? offset = null)
    {
        var offsetX = offset?.X ?? 0m;
        var offsetY = offset?.Y ?? 0m;
        var commands = new List<NativeDrawCommand>
        {
            Text(0m + offsetX, 0m + offsetY, 9.2m, 1.5m, labelPlan.IdentifierText, 4.5m, bold: false),
            Text(0m + offsetX, 2.8m + offsetY, 1.5m, 8.5m, "合格证", 4.6m, bold: false),
            QrCode(1.8m + offsetX, 1.5m + offsetY, 8.8m, labelPlan.QrPayload),
            Text(10.2m + offsetX, 0m + offsetY, 15.2m, 6.7m, labelPlan.ProductName, 4.2m, bold: false, maxLines: 3, ellipsis: true),
            Text(0m + offsetX, 10.8m + offsetY, 26m, 1.8m, $"{labelPlan.StandardText}  标签约0.20g", 3.4m, bold: false),
            Text(16m + offsetX, 12.05m + offsetY, 10m, 1.8m, labelPlan.PriceText, 4.5m, bold: true),
            Text(0m + offsetX, 14.6m + offsetY, 9m, 1.8m, labelPlan.SalesCode, 4.2m, bold: false)
        };

        AddWeightCommands(commands, labelPlan.FinishedWeightText, 10.2m + offsetX, 6.85m + offsetY);
        AddWeightCommands(commands, labelPlan.RoughWeightText, 10.2m + offsetX, 8.8m + offsetY);
        if (!string.IsNullOrWhiteSpace(labelPlan.AdditionalPriceText))
        {
            commands.Add(Text(12m + offsetX, 14.6m + offsetY, 12m, 1.8m, labelPlan.AdditionalPriceText, 4.2m, bold: false));
        }

        AddPartCommands(commands, labelPlan, offsetX, offsetY);

        if (!string.IsNullOrWhiteSpace(labelPlan.FooterText))
        {
            var footerY = CalculateFooterY(labelPlan);
            var footerHeight = Math.Max(1.4m, labelPlan.PaperSize.HeightMm - footerY - 0.4m);
            commands.Add(Text(0m + offsetX, footerY + offsetY, 22.8m, footerHeight, labelPlan.FooterText, 3.8m, bold: false));
        }

        commands.Add(Text(22.8m + offsetX, 15.6m + offsetY, 2.2m, 10m, labelPlan.IdentifierText, 4.2m, bold: true, rotationDegrees: 90m));

        return new NativeLabelDrawingPlan(labelPlan.PaperSize, commands);
    }

    private static void AddPartCommands(List<NativeDrawCommand> commands, LabelRenderPlan labelPlan, decimal offsetX, decimal offsetY)
    {
        for (var index = 0; index < labelPlan.Parts.Count; index++)
        {
            var part = labelPlan.Parts[index];
            var column = index % 2;
            var row = index / 2;
            var x = column == 0 ? 0m : 11.5m;
            var y = 16.4m + row * 2.05m;
            commands.Add(Text(
                x + offsetX,
                y + offsetY,
                11m,
                1.8m,
                $"{part.CategoryName}:{part.PartWeightText}",
                4.0m,
                bold: false));
        }
    }

    private static void AddWeightCommands(List<NativeDrawCommand> commands, string text, decimal x, decimal y)
    {
        var splitIndex = text.LastIndexOf(' ');
        if (splitIndex < 0 || splitIndex == text.Length - 1)
        {
            commands.Add(Text(x, y, 15.2m, 1.8m, text, 4.8m, bold: false));
            return;
        }

        var label = text[..(splitIndex + 1)];
        var value = text[(splitIndex + 1)..];
        commands.Add(Text(x, y + 0.18m, 7.8m, 1.8m, label, 3.9m, bold: false));
        // commands.Add(Text(x + 7.9m, y - 0.32m, 7.3m, 2.4m, value, 4.8m, bold: true));
        commands.Add(Text(x + 7.9m, y - 0.08m, 7.3m, 1.9m, value, 4.8m, bold: true));
    }

    private static decimal CalculateFooterY(LabelRenderPlan labelPlan)
    {
        if (labelPlan.Parts.Count == 0)
        {
            return 23.1m;
        }

        var rows = (labelPlan.Parts.Count + 1) / 2;
        return 16.4m + rows * 2.05m + 0.65m;
    }

    private static NativeDrawCommand Text(
        decimal x,
        decimal y,
        decimal width,
        decimal height,
        string text,
        decimal fontSizePt,
        bool bold,
        decimal rotationDegrees = 0m,
        int maxLines = 0,
        bool ellipsis = false)
    {
        return new NativeDrawCommand(NativeDrawCommandType.Text, x, y, width, height, text, fontSizePt, bold, rotationDegrees, maxLines, ellipsis);
    }

    private static NativeDrawCommand QrCode(decimal x, decimal y, decimal size, string payload)
    {
        return new NativeDrawCommand(NativeDrawCommandType.QrCode, x, y, size, size, payload, 0m, false);
    }
}
