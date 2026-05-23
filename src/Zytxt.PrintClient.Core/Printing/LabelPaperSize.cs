namespace Zytxt.PrintClient.Core.Printing;

public sealed record LabelPaperSize(
    decimal WidthMm,
    decimal HeightMm,
    int WidthHundredthsInch,
    int HeightHundredthsInch)
{
    private const decimal MillimetersPerInch = 25.4m;

    public static LabelPaperSize Create80x30()
    {
        return FromMillimeters(80m, 30m);
    }

    public static LabelPaperSize FromMillimeters(decimal widthMm, decimal heightMm)
    {
        return new LabelPaperSize(
            widthMm,
            heightMm,
            ToHundredthsInch(widthMm),
            ToHundredthsInch(heightMm));
    }

    private static int ToHundredthsInch(decimal millimeters)
    {
        return (int)Math.Round(millimeters / MillimetersPerInch * 100m, MidpointRounding.AwayFromZero);
    }
}
