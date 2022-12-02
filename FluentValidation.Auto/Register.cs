using Microsoft.AspNetCore.Mvc;

namespace FluentValidation.Auto;

public static class Register
{
    public static MvcOptions AddAutoValidation(this MvcOptions o)
    {
        o.ModelValidatorProviders.Add(new ModelValidatorProvider());
        return o;
    }
}