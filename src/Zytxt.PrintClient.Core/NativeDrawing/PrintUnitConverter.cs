namespace Zytxt.PrintClient.Core.NativeDrawing;

public sealed class PrintUnitConverter
{
    private const decimal MillimetersPerInch = 25.4m;
    private readonly decimal _dpi;

    public PrintUnitConverter(decimal dpi)
    {
        if (dpi <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpi), "DPI must be greater than zero.");
        }

        _dpi = dpi;
    }

    public int MillimetersToHundredthsInch(decimal millimeters)
    {
        return (int)Math.Round(millimeters / MillimetersPerInch * 100m, MidpointRounding.AwayFromZero);
    }

    public int MillimetersToPixels(decimal millimeters)
    {
        return (int)Math.Round(millimeters / MillimetersPerInch * _dpi, MidpointRounding.AwayFromZero);
    }
}
