namespace Zytxt.PrintClient.Core.Api;

public sealed record PrintJobResult(
    string JobId,
    string RequestId,
    string Status,
    int Accepted,
    int Total,
    int Printed,
    int Failed,
    int Pending)
{
    public static PrintJobResult CreateAccepted(string jobId, string requestId, int total)
    {
        return new PrintJobResult(jobId, requestId, "queued", total, total, 0, 0, total);
    }
}
