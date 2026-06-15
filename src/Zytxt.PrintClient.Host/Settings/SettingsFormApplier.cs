using System.Globalization;
using Microsoft.AspNetCore.Http;
using Zytxt.PrintClient.Core.Settings;

namespace Zytxt.PrintClient.Host.Settings;

public static class SettingsFormApplier
{
    private const decimal MinTemplateX = 0m;
    private const decimal MaxTemplateX = 80m;
    private const decimal MinTemplateY = 0m;
    private const decimal MaxTemplateY = 30m;
    private const decimal MinTemplateFontSizePt = 1m;
    private const decimal MaxTemplateFontSizePt = 12m;

    public static void Apply(PrintClientSettings settings, IFormCollection form, string previewTemplate)
    {
        if (form.ContainsKey("defaultPrinter"))
        {
            settings.DefaultPrinter = form["defaultPrinter"].ToString();
        }

        settings.LabelOffset = new LabelOffset(
            TryParseDecimal(form["offsetX"].ToString(), out var offsetX) ? offsetX : settings.LabelOffset.X,
            TryParseDecimal(form["offsetY"].ToString(), out var offsetY) ? offsetY : settings.LabelOffset.Y);

        if (form.ContainsKey("allowedOrigins"))
        {
            settings.AllowedOrigins = AllowedOriginParser.Parse(form["allowedOrigins"].ToString());
        }

        ApplyTemplateOverride(settings, form, previewTemplate);
    }

    private static void ApplyTemplateOverride(PrintClientSettings settings, IFormCollection form, string previewTemplate)
    {
        if (!form.ContainsKey("templateElementKey"))
        {
            return;
        }

        var elementKey = form["templateElementKey"].ToString().Trim();
        if (string.IsNullOrWhiteSpace(elementKey))
        {
            return;
        }

        var templateKey = NormalizePreviewTemplateOverrideKey(previewTemplate);
        if (form["templateReset"].ToString() == "current")
        {
            RemoveTemplateOverride(settings, templateKey, elementKey);
            return;
        }

        settings.TemplateOverrides ??= [];
        if (!settings.TemplateOverrides.TryGetValue(templateKey, out var templateOverrides))
        {
            templateOverrides = [];
            settings.TemplateOverrides[templateKey] = templateOverrides;
        }

        templateOverrides[elementKey] = new TemplateElementOverride
        {
            X = TryParseDecimal(form["templateX"].ToString(), out var x) ? Clamp(x, MinTemplateX, MaxTemplateX) : null,
            Y = TryParseDecimal(form["templateY"].ToString(), out var y) ? Clamp(y, MinTemplateY, MaxTemplateY) : null,
            FontSizePt = TryParseDecimal(form["templateFontSizePt"].ToString(), out var fontSizePt)
                ? Clamp(fontSizePt, MinTemplateFontSizePt, MaxTemplateFontSizePt)
                : null,
            Bold = form.ContainsKey("templateBold")
        };
    }

    private static void RemoveTemplateOverride(PrintClientSettings settings, string templateKey, string elementKey)
    {
        if (settings.TemplateOverrides is null
            || !settings.TemplateOverrides.TryGetValue(templateKey, out var templateOverrides))
        {
            return;
        }

        templateOverrides.Remove(elementKey);
        if (templateOverrides.Count == 0)
        {
            settings.TemplateOverrides.Remove(templateKey);
        }
    }

    private static bool TryParseDecimal(string value, out decimal result)
    {
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result)
            || decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out result);
    }

    private static decimal Clamp(decimal value, decimal min, decimal max)
    {
        return Math.Min(Math.Max(value, min), max);
    }

    private static string NormalizePreviewTemplateOverrideKey(string previewTemplate)
    {
        return string.Equals(previewTemplate, "silver", StringComparison.OrdinalIgnoreCase)
            ? "silver"
            : "default";
    }
}
