using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FluentValidation.Auto;

public static class Register
{
    public static MvcOptions AddAutoValidation(this MvcOptions o)
    {
        o.ModelValidatorProviders.Add(new ModelValidatorProvider());
        return o;
    }
    
    public static IMvcBuilder AddAutoValidationError(this IMvcBuilder builder, string messageTitle = "Validation error")
    {
        builder.ConfigureApiBehaviorOptions(o => o.InvalidModelStateResponseFactory =
                                                     ctx =>
                                                     {
                                                         var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<IMvcBuilder>>();
                                                         
                                                         if (ctx.ModelState.ValidationState != ModelValidationState.Invalid)
                                                             return new ObjectResult(new ErrorResult(ctx.HttpContext.TraceIdentifier, "Unknown error")) {StatusCode = 400};
                                                         
                                                         var err400 = ctx.ModelState
                                                                         .Where(p => p.Value is {ValidationState: ModelValidationState.Invalid})
                                                                         .GroupBy(p => p.Key)
                                                                         .ToDictionary(p => p.Key,
                                                                                       p => p.First().Value!.Errors.Select(c => c.ErrorMessage).ToArray());
                                                         
                                                         var errLog = string.Join(", ", err400.Select(p => p.Key + ": " + string.Join(",", p.Value)));
                                                         logger.LogWarning("{0}: {1} -> {2}", ctx.HttpContext.Request.Method, ctx.HttpContext.Request.Path, errLog);
                                                         
                                                         return new ObjectResult(new ErrorResult(ctx.HttpContext.TraceIdentifier, messageTitle, Errors: err400)) {StatusCode = 400};
                                                     });
        
        return builder;
    }
}