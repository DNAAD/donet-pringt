namespace Zytxt.PrintClient.Core.Settings;

public sealed class PrinterSelectionResolver
{
    public string Resolve(PrintClientSettings settings, string? requestPrinterName = null)
    {
        return string.IsNullOrWhiteSpace(settings.DefaultPrinter)
            ? ""
            : settings.DefaultPrinter.Trim();
    }
}
