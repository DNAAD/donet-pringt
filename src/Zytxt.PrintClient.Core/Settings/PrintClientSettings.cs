namespace Zytxt.PrintClient.Core.Settings;

public sealed class PrintClientSettings
{
    public string DefaultPrinter { get; set; } = "";

    public LabelOffset LabelOffset { get; set; } = new(0m, 0m);

    public List<string> AllowedOrigins { get; set; } = [];

    public Dictionary<string, Dictionary<string, TemplateElementOverride>> TemplateOverrides { get; set; } = [];
}
