namespace Zytxt.PrintClient.Core.Labels;

public sealed class LabelItem
{
    public string IdentifierCode { get; set; } = "";

    public string ProductName { get; set; } = "";

    public string WeightCategory { get; set; } = "";

    public decimal FinishedProductWeight { get; set; }

    public decimal RoughWeight { get; set; }

    public string SalesCode { get; set; } = "";

    public string GoldPurity { get; set; } = "";

    public string Address { get; set; } = "";

    public decimal AdditionalPrice { get; set; }

    public string CategoryName { get; set; } = "";

    public List<LabelPartItem> FinishedProductPartVO { get; set; } = [];

    public string AdditionalRemark { get; set; } = "";

    public decimal InlayWeight { get; set; }

    public decimal RopeWeight { get; set; }

    public string FinishedProductNote { get; set; } = "";
}
