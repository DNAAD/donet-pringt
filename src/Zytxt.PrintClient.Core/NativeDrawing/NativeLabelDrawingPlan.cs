using Zytxt.PrintClient.Core.Printing;

namespace Zytxt.PrintClient.Core.NativeDrawing;

public sealed record NativeLabelDrawingPlan(
    LabelPaperSize PaperSize,
    IReadOnlyList<NativeDrawCommand> Commands);
