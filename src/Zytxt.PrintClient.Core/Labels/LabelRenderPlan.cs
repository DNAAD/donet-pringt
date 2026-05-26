using Zytxt.PrintClient.Core.Printing;

namespace Zytxt.PrintClient.Core.Labels;

public sealed record LabelRenderPlan(
    LabelPaperSize PaperSize,
    LabelTemplateKey TemplateKey,
    string IdentifierText,
    string ProductName,
    string FinishedWeightText,
    string RoughWeightText,
    string SalesCode,
    string StandardText,
    string AddressText,
    string QrPayload,
    string GoldPurityText,
    string PriceText,
    string AdditionalPriceText,
    string TagWeightText,
    IReadOnlyList<LabelPartRenderPlan> Parts,
    string FooterText);
