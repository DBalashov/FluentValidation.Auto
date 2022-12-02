using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace FluentValidation.Auto;

sealed class ModelValidatorProvider : IModelValidatorProvider
{
    Type? findValidatorInterface(IReadOnlyList<object> m)
    {
        var attribute = m.OfType<Attribute>()
                         .FirstOrDefault(p => p.GetType()
                                               .GenericTypeArguments
                                               .Any(c => c?.GetType().GetInterfaces().Any(i => i == typeof(IValidator)) != null));
        return attribute?.GetType().GenericTypeArguments.First();
    }

    public void CreateValidators(ModelValidatorProviderContext context)
    {
        if (context.ModelMetadata is not DefaultModelMetadata m ||
            m.Attributes.TypeAttributes == null) return;

        var validatorType = findValidatorInterface(m.Attributes.Attributes);
        if (validatorType == null) return;

        context.Results.Add(new ValidatorItem()
                            {
                                Validator  = new ModelValidator(validatorType),
                                IsReusable = false
                            });
    }
}