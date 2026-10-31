using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace InventoryManagement.Api.Filters;

/// <summary>
/// Runs the registered IValidator&lt;T&gt; for every action argument (body AND query objects).
/// Failures throw ValidationException, which the global exception handler turns into an RFC 7807 400 response.
/// (FluentValidation.AspNetCore auto-validation is deprecated, hence this small explicit filter.)
/// </summary>
public sealed class FluentValidationFilter(IServiceProvider services) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null) continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (services.GetService(validatorType) is not IValidator validator) continue;

            var result = await validator.ValidateAsync(new ValidationContext<object>(argument), context.HttpContext.RequestAborted);
            if (!result.IsValid) throw new ValidationException(result.Errors);
        }

        await next();
    }
}
