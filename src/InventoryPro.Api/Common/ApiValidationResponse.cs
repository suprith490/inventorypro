using Microsoft.AspNetCore.Mvc;

namespace InventoryPro.Api.Common;

/// <summary>
/// Replaces the default ASP.NET Core validation response so that model-binding
/// failures also use the standard ApiResponse envelope. This means the frontend
/// always reads { success, message, errors } no matter what went wrong.
/// In Spring Boot this is analogous to the MethodArgumentNotValidException
/// handler you write in @RestControllerAdvice.
/// </summary>
public static class ApiValidationResponse
{
    public static IActionResult Build(ActionContext context)
    {
        var errors = context.ModelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .SelectMany(entry => entry.Value!.Errors.Select(error =>
                string.IsNullOrWhiteSpace(error.ErrorMessage)
                    ? $"{entry.Key}: invalid value."
                    : error.ErrorMessage))
            .Distinct()
            .ToList();

        var response = ApiResponse.Fail(
            "One or more validation errors occurred.",
            StatusCodes.Status400BadRequest,
            errors);

        return new BadRequestObjectResult(response);
    }
}
