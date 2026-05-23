using Zytxt.PrintClient.Core.Printing;

namespace Zytxt.PrintClient.Core.Labels;

public sealed record LabelRenderPlan(
    LabelPaperSize PaperSize,
    string IdentifierText,
    string ProductName,
    string FinishedWeightText,
    string RoughWeightText,
    string SalesCode,
    string StandardText,
    string AddressText,
    string QrPayload,
    string GoldPurityText,
    string AdditionalPriceText,
    IReadOnlyList<LabelPartRenderPlan> Parts,
    string FooterText);
