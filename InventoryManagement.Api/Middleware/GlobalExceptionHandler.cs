using System.Diagnostics;
using FluentValidation;
using InventoryManagement.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.Api.Middleware;

/// <summary>
/// Single place that maps exceptions to RFC 7807 problem responses. Controllers contain no try/catch.
/// Internal details are never leaked outside Development.
/// </summary>
internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetails,
    IHostEnvironment env,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        if (context.RequestAborted.IsCancellationRequested)
        {
            context.Response.StatusCode = 499;   // client closed request
            return true;
        }

        var (status, title) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "One or more validation errors occurred."),
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found."),
            AuthenticationFailedException => (StatusCodes.Status401Unauthorized, "Authentication failed."),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict."),          // incl. ConcurrencyException
            DomainException => (StatusCodes.Status422UnprocessableEntity, "Business rule violation."),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        if (status >= 500) logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
        else logger.LogWarning("Request rejected ({Status}): {Message}", status, exception.Message);

        ProblemDetails problem = exception is ValidationException validation
            ? new ValidationProblemDetails(validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray()))
            : new ProblemDetails();

        problem.Status = status;
        problem.Title = title;
        problem.Instance = context.Request.Path;
        problem.Detail = exception switch
        {
            ValidationException => null,
            _ when status >= 500 => env.IsDevelopment() ? exception.ToString() : null,
            _ => exception.Message
        };
        problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;

        context.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem,
            Exception = exception
        });
    }
}
