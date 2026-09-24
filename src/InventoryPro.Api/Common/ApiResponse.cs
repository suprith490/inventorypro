namespace InventoryPro.Api.Common;

/// <summary>
/// Standard response envelope returned by every endpoint that has no payload.
/// Using one shape for all responses makes the JavaScript frontend predictable.
/// Similar to wrapping everything in a Spring Boot ResponseEntity&lt;ApiResponse&gt;.
/// </summary>
public class ApiResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int StatusCode { get; init; }
    public IReadOnlyList<string>? Errors { get; init; }
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;

    public static ApiResponse Ok(string message = "Request successful", int statusCode = 200) => new()
    {
        Success = true,
        Message = message,
        StatusCode = statusCode
    };

    public static ApiResponse Fail(
        string message,
        int statusCode = 400,
        IReadOnlyList<string>? errors = null) => new()
    {
        Success = false,
        Message = message,
        StatusCode = statusCode,
        Errors = errors
    };
}

/// <summary>
/// Standard response envelope returned by every endpoint that has a payload.
/// </summary>
public class ApiResponse<T> : ApiResponse
{
    public T? Data { get; init; }

    public static ApiResponse<T> Ok(
        T data,
        string message = "Request successful",
        int statusCode = 200) => new()
    {
        Success = true,
        Message = message,
        StatusCode = statusCode,
        Data = data
    };

    public static new ApiResponse<T> Fail(
        string message,
        int statusCode = 400,
        IReadOnlyList<string>? errors = null) => new()
    {
        Success = false,
        Message = message,
        StatusCode = statusCode,
        Errors = errors
    };
}
