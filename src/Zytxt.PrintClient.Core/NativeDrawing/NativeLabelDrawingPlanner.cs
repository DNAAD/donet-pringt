using Zytxt.PrintClient.Core.Labels;
using Zytxt.PrintClient.Core.Settings;

namespace Zytxt.PrintClient.Core.NativeDrawing;

public sealed class NativeLabelDrawingPlanner
{
    private readonly SilverNativeLabelDrawingPlanner silverPlanner = new();

    public NativeLabelDrawingPlan CreatePlan(LabelRenderPlan labelPlan, LabelOffset? offset = null)
    {
        return CreatePlan(labelPlan, offset, overrides: null);
    }

    public NativeLabelDrawingPlan CreatePlan(
        LabelRenderPlan labelPlan,
        LabelOffset? offset,
        IReadOnlyDictionary<string, TemplateElementOverride>? overrides)
    {
        if (labelPlan.TemplateKey == LabelTemplateKey.Silver80x30)
        {
            return silverPlanner.CreatePlan(labelPlan, offset, overrides);
        }

        var offsetX = offset?.X ?? 0m;
        var offsetY = offset?.Y ?? 0m;
        var commands = new List<NativeDrawCommand>
        {
            Text(0m + offsetX, 0m + offsetY, 9.2m, 1.5m, labelPlan.IdentifierText, 4.5m, bold: false, elementKey: "identifier"),
            Text(0m + offsetX, 2.5m + offsetY, 2m, 8.5m, "合格证", 4.6m, bold: false, elementKey: "qualityMark"),
            QrCode(1.8m + offsetX, 1m + offsetY, 9m, labelPlan.QrPayload, "qrCode"),
            Text(1.8m + offsetX, 9m + offsetY, 9m, 1.2m, "标签约0.20g", 3.6m, bold: false, elementKey: "qrNote"),
            Text(10.2m + offsetX, 0m + offsetY, 16.3m, 5.4m, labelPlan.ProductName, 4.2m, bold: false, maxLines: 3, ellipsis: true, elementKey: "productName"),
            Text(0m + offsetX, 10.8m + offsetY, 26m, 1.8m, labelPlan.StandardText, 3.4m, bold: false, elementKey: "standardText"),
            Text(0m + offsetX, 12.05m + offsetY, 26m, 1.8m, labelPlan.AddressText, 4.0m, bold: false, elementKey: "addressText"),
            Text(0m + offsetX, 14.6m + offsetY, 9m, 1.8m, labelPlan.SalesCode, 4.2m, bold: true, elementKey: "salesCode")
        };

        AddWeightCommands(commands, labelPlan.FinishedWeightText, 10.2m + offsetX, 6.85m + offsetY, "finishedWeight");
        AddWeightCommands(commands, labelPlan.RoughWeightText, 10.2m + offsetX, 8.8m + offsetY, "roughWeight");

        if (!string.IsNullOrWhiteSpace(labelPlan.AdditionalPriceText))
        {
            commands.Add(Text(12m + offsetX, 14.6m + offsetY, 12m, 1.8m, labelPlan.AdditionalPriceText, 4.2m, bold: false, elementKey: "additionalPrice"));
        }

        AddPartCommands(commands, labelPlan, offsetX, offsetY);

        if (!string.IsNullOrWhiteSpace(labelPlan.FooterText))
        {
            var footerY = CalculateFooterY(labelPlan);
            var footerHeight = Math.Max(1.4m, labelPlan.PaperSize.HeightMm - footerY - 0.4m);
            commands.Add(Text(0m + offsetX, footerY + offsetY, 22.8m, footerHeight, labelPlan.FooterText, 3.8m, bold: false, elementKey: "footerText"));
        }

        commands.Add(Text(22.5m + offsetX, 15.6m + offsetY, 2.2m, 10m, labelPlan.IdentifierText, 4.2m, bold: true, rotationDegrees: 90m, elementKey: "verticalIdentifier"));

        return new NativeLabelDrawingPlan(labelPlan.PaperSize, ApplyOverrides(commands, overrides, offsetX, offsetY));
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
                $"{part.CategoryName}:{part.PartWeightText}g",
                4.0m,
                bold: false,
                elementKey: "partRow"));
        }
    }

    private static void AddWeightCommands(List<NativeDrawCommand> commands, string text, decimal x, decimal y, string keyPrefix)
    {
        var splitIndex = text.LastIndexOf(' ');
        if (splitIndex < 0 || splitIndex == text.Length - 1)
        {
            commands.Add(Text(x, y, 15.2m, 1.8m, text, 4.5m, bold: false, elementKey: $"{keyPrefix}Value"));
            return;
        }

        var label = text[..(splitIndex + 1)];
        var value = text[(splitIndex + 1)..];
        commands.Add(Text(x, y, 7.8m, 1.8m, label, 4.0m, bold: false, elementKey: $"{keyPrefix}Label"));
        commands.Add(Text(x + 7.9m, y - 0.08m, 7.3m, 1.9m, value, 4.5m, bold: true, elementKey: $"{keyPrefix}Value"));
    }

    internal static IReadOnlyList<NativeDrawCommand> ApplyOverrides(
        IReadOnlyList<NativeDrawCommand> commands,
        IReadOnlyDictionary<string, TemplateElementOverride>? overrides,
        decimal offsetX,
        decimal offsetY)
    {
        if (overrides is null || overrides.Count == 0)
        {
            return commands;
        }

        return commands.Select(command =>
        {
            if (string.IsNullOrWhiteSpace(command.ElementKey)
                || !overrides.TryGetValue(command.ElementKey, out var elementOverride))
            {
                return command;
            }

            return command with
            {
                X = elementOverride.X.HasValue ? elementOverride.X.Value + offsetX : command.X,
                Y = elementOverride.Y.HasValue ? elementOverride.Y.Value + offsetY : command.Y,
                FontSizePt = elementOverride.FontSizePt ?? command.FontSizePt,
                Bold = elementOverride.Bold ?? command.Bold
            };
        }).ToList();
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
        bool ellipsis = false,
        string elementKey = "")
    {
        return new NativeDrawCommand(NativeDrawCommandType.Text, x, y, width, height, text, fontSizePt, bold, rotationDegrees, maxLines, ellipsis, elementKey);
    }

    private static NativeDrawCommand QrCode(decimal x, decimal y, decimal size, string payload, string elementKey)
    {
        return new NativeDrawCommand(NativeDrawCommandType.QrCode, x, y, size, size, payload, 0m, false, ElementKey: elementKey);
    }
}
