namespace RaceDay.API.Infrastructure;

using Microsoft.AspNetCore.Http.HttpResults;

/// <summary>
/// Builders for the API's problem responses so every error has the same shape.
/// </summary>
internal static class ApiProblems
{
    public const string ValidationTitle = "One or more validation errors occurred.";

    public static HttpValidationProblemDetails Validation(string propertyName, string message) =>
        new(new Dictionary<string, string[]> { [propertyName] = [message] })
        {
            Status = StatusCodes.Status400BadRequest,
            Title = ValidationTitle,
            Detail = message
        };

    public static ValidationProblem ValidationResult(string propertyName, string message) =>
        TypedResults.ValidationProblem(
            new Dictionary<string, string[]> { [propertyName] = [message] },
            detail: message,
            title: ValidationTitle);
}
