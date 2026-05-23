using Zytxt.PrintClient.Core.Labels;

namespace Zytxt.PrintClient.Core.Api;

public sealed class PrintTagRequest
{
    public string RequestId { get; set; } = "";

    public string PrinterName { get; set; } = "";

    public bool ExecutePrint { get; set; }

    public List<LabelItem> Items { get; set; } = [];
}
