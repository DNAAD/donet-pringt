using System.Globalization;
using Zytxt.PrintClient.Core.Printing;

namespace Zytxt.PrintClient.Core.Labels;

public sealed class LabelRenderPlanner
{
    public LabelRenderPlan CreatePlan(LabelItem item)
    {
        var weightCategory = string.IsNullOrWhiteSpace(item.WeightCategory) ? "成品重" : item.WeightCategory.Trim();
        var purity = string.IsNullOrWhiteSpace(item.GoldPurity) ? "" : item.GoldPurity.Trim();
        var parts = CreateParts(item);

        return new LabelRenderPlan(
            LabelPaperSize.Create80x30(),
            item.IdentifierCode.Trim(),
            item.ProductName.Trim(),
            $"{weightCategory}(g): {FormatWeight(item.FinishedProductWeight)}",
            $"总件重(g): {FormatWeight(item.RoughWeight)}",
            item.SalesCode.Trim(),
            string.IsNullOrWhiteSpace(purity) ? "执行标准QB/T2062 GB11887" : $"执行标准QB/T2062 GB11887  {purity}",
            string.IsNullOrWhiteSpace(item.Address) ? "地址:" : $"地址:{item.Address.Trim()}",
            item.IdentifierCode.Trim(),
            purity,
            item.AdditionalPrice > 0 ? $"附加:￥{FormatWeight(item.AdditionalPrice)}" : "",
            parts,
            CreateFooterText(item));
    }

    private static IReadOnlyList<LabelPartRenderPlan> CreateParts(LabelItem item)
    {
        if (item.FinishedProductPartVO.Count > 0)
        {
            return item.FinishedProductPartVO
                .Take(9)
                .Select(part => new LabelPartRenderPlan(part.CategoryName.Trim(), FormatWeight(part.PartWeight)))
                .ToList();
        }

        if (string.IsNullOrWhiteSpace(item.CategoryName))
        {
            return [];
        }

        return [new LabelPartRenderPlan(item.CategoryName.Trim(), FormatWeight(item.FinishedProductWeight))];
    }

    private static string CreateFooterText(LabelItem item)
    {
        var fragments = new List<string>();
        if (!string.IsNullOrWhiteSpace(item.AdditionalRemark))
        {
            fragments.Add($"附加:{item.AdditionalRemark.Trim()}");
        }

        if (item.InlayWeight > 0)
        {
            fragments.Add($"附加重:{FormatWeight(item.InlayWeight)}g");
        }

        if (item.RopeWeight > 0)
        {
            fragments.Add($"绳重:{FormatWeight(item.RopeWeight)}g");
        }

        if (!string.IsNullOrWhiteSpace(item.FinishedProductNote))
        {
            fragments.Add(item.FinishedProductNote.Trim());
        }

        return string.Join(" ", fragments);
    }

    private static string FormatWeight(decimal value)
    {
        return value.ToString("0.00", CultureInfo.InvariantCulture);
    }
}
