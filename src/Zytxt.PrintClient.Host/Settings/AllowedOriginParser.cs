namespace Zytxt.PrintClient.Host.Settings;

public static class AllowedOriginParser
{
    private static readonly string[] Separators = ["\r\n", "\n", "\r", ",", ";"];

    public static List<string> Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        var origins = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in value.Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var origin = Normalize(entry);
            if (origin.Length == 0 || !seen.Add(origin))
            {
                continue;
            }

            origins.Add(origin);
        }

        return origins;
    }

    private static string Normalize(string value)
    {
        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
            || !IsHttpScheme(uri.Scheme)
            || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment)
            || (uri.AbsolutePath.Length > 0 && uri.AbsolutePath != "/"))
        {
            return "";
        }

        var host = uri.HostNameType == UriHostNameType.IPv6
            ? $"[{uri.IdnHost}]"
            : uri.IdnHost;
        var authority = uri.IsDefaultPort ? host : $"{host}:{uri.Port}";

        return $"{uri.Scheme.ToLowerInvariant()}://{authority}";
    }

    private static bool IsHttpScheme(string scheme)
    {
        return string.Equals(scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            || string.Equals(scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
    }
}
