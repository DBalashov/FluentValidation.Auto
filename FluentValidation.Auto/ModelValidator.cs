using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace FluentValidation.Auto;

sealed class ModelValidator : IModelValidator
{
    static readonly Dictionary<Type, Type> validationContextTypeCache = new();
    static readonly object                 sync                       = new();

    readonly Type validatorType;

    public ModelValidator(Type validatorType) => this.validatorType = validatorType;

    public IEnumerable<ModelValidationResult> Validate(ModelValidationContext context)
    {
        if (context.Model == null) return Array.Empty<ModelValidationResult>(); // empty model -> no validations

        var validator = context.ActionContext.HttpContext.RequestServices.GetService(validatorType) as IValidator;
        if (validator == null)
            throw new FluentValidatorAutoException("Can't found validator by type: {0}", validatorType);

        var constructorInfo   = getValidatorInstance(context.Model.GetType());
        var validatorInstance = constructorInfo.Invoke(new[] {context.Model});

        var r = validator.Validate(validatorInstance as IValidationContext);
        return r.IsValid
                   ? Array.Empty<ModelValidationResult>()
                   : r.Errors.Select(p => new ModelValidationResult(p.PropertyName, p.ErrorMessage)).ToArray();
    }

    ConstructorInfo getValidatorInstance(Type modelType)
    {
        Type? genericType;
        lock (sync)
        {
            if (!validationContextTypeCache.TryGetValue(modelType, out genericType))
            {
                genericType = typeof(ValidationContext<>).MakeGenericType(modelType);
                validationContextTypeCache.Add(modelType, genericType);
            }
        }

        var constructorInfo = genericType
                             .GetConstructors()
                             .OrderBy(p => p.GetParameters().Length)
                             .FirstOrDefault();

        if (constructorInfo == null || constructorInfo.GetParameters().Length != 1)
            throw new FluentValidatorAutoException("Can't find constructor with one parameter for {0}", genericType);

        return constructorInfo;
    }
}