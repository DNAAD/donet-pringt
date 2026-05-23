namespace Zytxt.PrintClient.Core.Api;

public sealed record HealthInfo(bool Ready, string Service, string Protocol);
