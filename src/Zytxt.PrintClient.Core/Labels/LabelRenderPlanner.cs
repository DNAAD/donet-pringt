using System.Globalization;
using Zytxt.PrintClient.Core.Printing;

namespace Zytxt.PrintClient.Core.Labels;

public sealed class LabelRenderPlanner
{
    public LabelRenderPlan CreatePlan(LabelItem item)
    {
        var templateKey = item.FactoryNo == 25003
            ? LabelTemplateKey.Silver80x30
            : LabelTemplateKey.Default80x30;
        var weightCategory = string.IsNullOrWhiteSpace(item.WeightCategory) ? "成品重" : item.WeightCategory.Trim();
        var purity = string.IsNullOrWhiteSpace(item.GoldPurity) ? "" : item.GoldPurity.Trim();
        var parts = CreateParts(item);

        return new LabelRenderPlan(
            LabelPaperSize.Create80x30(),
            templateKey,
            item.IdentifierCode.Trim(),
            item.ProductName.Trim(),
            $"{weightCategory}(g): {FormatWeight(item.FinishedProductWeight)}",
            templateKey == LabelTemplateKey.Silver80x30
                ? $"总重(g): {FormatWeight(item.RoughWeight)}"
                : $"总件重(g): {FormatWeight(item.RoughWeight)}",
            item.SalesCode.Trim(),
            CreateStandardText(templateKey, purity),
            string.IsNullOrWhiteSpace(item.Address) ? "地址:" : $"地址:{item.Address.Trim()}",
            item.IdentifierCode.Trim(),
            purity,
            item.Price > 0 ? $"￥{FormatWeight(item.Price)}" : "",
            CreateAdditionalPriceText(templateKey, item.AdditionalPrice),
            item.TagWeight > 0 ? FormatWeight(item.TagWeight) : "0.20",
            parts,
            CreateFooterText(item));
    }

    private static string CreateStandardText(LabelTemplateKey templateKey, string purity)
    {
        if (templateKey == LabelTemplateKey.Silver80x30)
        {
            return "执行标准QB/T2062 GB11887";
        }

        return string.IsNullOrWhiteSpace(purity)
            ? "执行标准QB/T2062 GB11887"
            : $"执行标准QB/T2062 GB11887  {purity}";
    }

    private static string CreateAdditionalPriceText(LabelTemplateKey templateKey, decimal additionalPrice)
    {
        if (additionalPrice <= 0)
        {
            return "";
        }

        return $"附加:￥{FormatWeight(additionalPrice)}";
    }

    private static IReadOnlyList<LabelPartRenderPlan> CreateParts(LabelItem item)
    {
        if (item.FinishedProductPartVO?.Count > 0)
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
