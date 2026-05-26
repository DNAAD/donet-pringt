namespace Zytxt.PrintClient.Host.Security;

public static class LocalCorsPolicy
{
    public const string PolicyName = "LocalPrintClientCors";
    private static readonly HashSet<string> BuiltInTrustedHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "114.132.160.27"
    };

    public static bool IsAllowedOrigin(string? origin, IEnumerable<string>? configuredOrigins = null)
    {
        if (string.IsNullOrWhiteSpace(origin)
            || !Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            || !IsHttpScheme(uri.Scheme))
        {
            return false;
        }

        if (IsLoopbackHost(uri.Host))
        {
            return true;
        }

        if (BuiltInTrustedHosts.Contains(uri.Host))
        {
            return true;
        }

        return configuredOrigins?.Any(allowed =>
            string.Equals(allowed?.Trim(), origin, StringComparison.OrdinalIgnoreCase)) == true;
    }

    private static bool IsHttpScheme(string scheme)
    {
        return string.Equals(scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            || string.Equals(scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLoopbackHost(string host)
    {
        return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "[::1]", StringComparison.OrdinalIgnoreCase);
    }
}
