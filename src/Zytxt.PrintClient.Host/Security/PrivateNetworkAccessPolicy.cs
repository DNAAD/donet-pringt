namespace Zytxt.PrintClient.Host.Security;

public static class PrivateNetworkAccessPolicy
{
    public const string RequestHeaderName = "Access-Control-Request-Private-Network";
    public const string ResponseHeaderName = "Access-Control-Allow-Private-Network";

    public static bool ShouldAllow(
        string? requestPrivateNetwork,
        string? origin,
        IEnumerable<string>? configuredOrigins = null)
    {
        return string.Equals(requestPrivateNetwork?.Trim(), "true", StringComparison.OrdinalIgnoreCase)
            && LocalCorsPolicy.IsAllowedOrigin(origin, configuredOrigins);
    }
}
