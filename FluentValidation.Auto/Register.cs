using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FluentValidation.Auto;

/// <summary>
/// <param name="errors">List of errors [property:errors]. Passed only invalid properties (with at least one error)</param>
/// <returns>Must return object for serialization or null for no content</returns>
/// </summary>
public delegate object? FluentValidationErrorFormatter(HttpContext ctx, Dictionary<string, string[]> errors);

public static class Register
{
    /// <summary> Add automatic FluentValidation calling for input models </summary>
    public static MvcOptions AddAutoValidation(this MvcOptions o)
    {
        o.ModelValidatorProviders.Add(new ModelValidatorProvider());
        return o;
    }

    [Obsolete("Use IServiceCollection.AddAutoValidationErrorFormatter", true)]
    public static IMvcBuilder AddAutoValidationErrorFormatter(this IMvcBuilder builder, FluentValidationErrorFormatter? formatter = null) => builder;

    /// <summary>
    /// Add optional automatic error formatter for FluentValidation errors.
    /// If formatter is null, default error formatter will be used (<see cref="ErrorResult"/>).
    /// </summary>
    public static IServiceCollection AddAutoValidationErrorFormatter(this IServiceCollection builder, FluentValidationErrorFormatter? formatter = null)
    {
        builder.Configure<ApiBehaviorOptions>(o => o.InvalidModelStateResponseFactory =
                                                       ctx =>
                                                       {
                                                           var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<IMvcBuilder>>();

                                                           if (ctx.ModelState.ValidationState != ModelValidationState.Invalid)
                                                               return new ObjectResult(new ErrorResult(ctx.HttpContext.TraceIdentifier, "Unknown error")) {StatusCode = 400};

                                                           var err400 = ctx.ModelState
                                                                           .Where(p => p.Value is {ValidationState: ModelValidationState.Invalid})
                                                                           .GroupBy(p => p.Key)
                                                                           .ToDictionary(p => p.Key,
                                                                                         p => p.First().Value!.Errors.Select(c => c.ErrorMessage).ToArray(),
                                                                                         StringComparer.Ordinal);

                                                           var errLog = string.Join(", ", err400.Select(p => p.Key + ": " + string.Join(",", p.Value)));
                                                           logger.LogWarning("{0}: {1} -> {2}", ctx.HttpContext.Request.Method, ctx.HttpContext.Request.Path, errLog);

                                                           var outputResult = formatter != null
                                                                                  ? formatter(ctx.HttpContext, err400)
                                                                                  : new ErrorResult(ctx.HttpContext.TraceIdentifier, "Validation error",
                                                                                                    Errors: err400);

                                                           return outputResult == null ? new BadRequestResult() : new ObjectResult(outputResult) {StatusCode = 400};
                                                       });

        return builder;
    }
}