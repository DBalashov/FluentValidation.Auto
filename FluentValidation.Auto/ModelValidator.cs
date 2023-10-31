using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace FluentValidation.Auto;

sealed class ModelValidator : IModelValidator
{
    static readonly ReaderWriterLockSlim              rwLock                     = new();
    static readonly Dictionary<Type, ConstructorInfo> validationContextTypeCache = new();
    readonly        Type                              validatorType;

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
        rwLock.EnterUpgradeableReadLock();
        if (!validationContextTypeCache.TryGetValue(modelType, out var constructorInfo))
        {
            rwLock.EnterWriteLock();
            try
            {
                if (!validationContextTypeCache.TryGetValue(modelType, out constructorInfo))
                {
                    var genericType = typeof(ValidationContext<>).MakeGenericType(modelType);
                    
                    constructorInfo = genericType.GetConstructors().MinBy(p => p.GetParameters().Length);
                    if (constructorInfo == null || constructorInfo.GetParameters().Length != 1)
                        throw new FluentValidatorAutoException("Can't find constructor with one parameter for {0}", genericType);

                    validationContextTypeCache.Add(modelType, constructorInfo);
                }
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
        }

        rwLock.ExitUpgradeableReadLock();

        // var constructorInfo = genericType.GetConstructors().MinBy(p => p.GetParameters().Length);
        // if (constructorInfo == null || constructorInfo.GetParameters().Length != 1)
        //     throw new FluentValidatorAutoException("Can't find constructor with one parameter for {0}", genericType);

        return constructorInfo;
    }
}