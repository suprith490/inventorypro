namespace InventoryPro.Api.Common.Exceptions;

/// <summary>
/// Base class for expected business exceptions.
/// The exception middleware maps StatusCode to the HTTP response
/// (the Spring equivalent is @ResponseStatus on a custom exception).
/// </summary>
public abstract class AppException : Exception
{
    protected AppException(string message, int statusCode)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }
}
