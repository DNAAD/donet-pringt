namespace Zytxt.PrintClient.Core.NativeDrawing;

public sealed record NativeDrawCommand(
    NativeDrawCommandType Type,
    decimal X,
    decimal Y,
    decimal Width,
    decimal Height,
    string Text,
    decimal FontSizePt,
    bool Bold,
    decimal RotationDegrees = 0m,
    int MaxLines = 0,
    bool Ellipsis = false);
