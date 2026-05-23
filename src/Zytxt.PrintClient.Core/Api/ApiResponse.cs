namespace Zytxt.PrintClient.Core.Api;

public sealed record ApiResponse<T>(
    bool Success,
    string Code,
    string Message,
    T Data)
{
    public static ApiResponse<T> Ok(T data)
    {
        return new ApiResponse<T>(true, "OK", "", data);
    }

    public static ApiResponse<T> Fail(string code, string message, T data)
    {
        return new ApiResponse<T>(false, code, message, data);
    }
}
