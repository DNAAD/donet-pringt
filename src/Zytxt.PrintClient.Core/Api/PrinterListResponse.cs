namespace Zytxt.PrintClient.Core.Api;

public sealed record PrinterListResponse(IReadOnlyList<PrinterInfo> Printers);
