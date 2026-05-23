using Zytxt.PrintClient.Core.Printing;

namespace Zytxt.PrintClient.Core.NativeDrawing;

public sealed class AxisDiagnosticDrawingPlanner
{
    public NativeLabelDrawingPlan CreatePlan()
    {
        var paperSize = LabelPaperSize.Create80x30();
        var commands = new List<NativeDrawCommand>
        {
            Rectangle(0m, 0m, paperSize.WidthMm, paperSize.HeightMm),
            Rectangle(0.4m, 0.4m, 4m, 4m),
            Text(1m, 1m, 12m, 2.5m, "TL 0,0", 7m, bold: true),
            Text(64m, 1m, 14m, 2.5m, "TR 80,0", 7m, bold: true),
            Text(1m, 26m, 14m, 2.5m, "BL 0,30", 7m, bold: true),
            Text(63m, 26m, 15m, 2.5m, "BR 80,30", 7m, bold: true),
            Text(22m, 3.5m, 28m, 3m, "X -> 80mm", 8m, bold: true),
            Text(4m, 12m, 24m, 3m, "Y -> 30mm", 8m, bold: true),
            Text(25m, 13m, 34m, 3m, "AXIS TEST 80x30", 8m, bold: true),
            Text(25m, 17m, 40m, 2.5m, "If rotated, driver/form direction is wrong", 6m, bold: false)
        };

        return new NativeLabelDrawingPlan(paperSize, commands);
    }

    private static NativeDrawCommand Text(
        decimal x,
        decimal y,
        decimal width,
        decimal height,
        string text,
        decimal fontSizePt,
        bool bold)
    {
        return new NativeDrawCommand(NativeDrawCommandType.Text, x, y, width, height, text, fontSizePt, bold);
    }

    private static NativeDrawCommand Rectangle(decimal x, decimal y, decimal width, decimal height)
    {
        return new NativeDrawCommand(NativeDrawCommandType.Rectangle, x, y, width, height, "", 0m, false);
    }
}
