namespace Zytxt.PrintClient.Core.Api;

public sealed record PrinterInfo(
    string Name,
    string DisplayName,
    bool IsDefault,
    string Status);
