namespace RaceDay.API.Infrastructure;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Core.Exceptions;

/// <summary>
/// Single place where exceptions become RFC 7807 responses. Never exposes stack traces or internal messages
/// for unexpected failures.
/// </summary>
internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            ValidationException ex => ApiProblems.Validation(ex.PropertyName, ex.Message),
            CatalogueLoadException => new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Service unavailable",
                Detail = "The product or activity catalogue is temporarily unavailable. Please try again later."
            },
            RaceDayException ex => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "The request could not be processed",
                Detail = ex.Message
            },
            BadHttpRequestException ex => new ProblemDetails
            {
                Status = ex.StatusCode,
                Title = ex.StatusCode == StatusCodes.Status413PayloadTooLarge
                    ? "Request body too large"
                    : "The request is malformed",
                Detail = ex.StatusCode == StatusCodes.Status413PayloadTooLarge
                    ? $"The request body exceeds the {ApiLimits.MaxRequestBodyBytes} byte limit."
                    : "The request body could not be read. Check that it is valid JSON and that enum values are valid."
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred",
                Detail = "An unexpected error occurred. Please try again later."
            }
        };

        int status = problem.Status ?? StatusCodes.Status500InternalServerError;
        if (status >= StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Request {Method} {Path} failed with {Status}", httpContext.Request.Method, httpContext.Request.Path, status);
        else if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Request {Method} {Path} rejected with {Status}: {Message}", httpContext.Request.Method, httpContext.Request.Path, status, exception.Message);

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem
        });
    }
}
