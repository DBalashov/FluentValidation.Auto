using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace FluentValidation.Auto;

sealed class ModelValidator(Type type) : IModelValidator
{
    static readonly ReaderWriterLockSlim              rwLock                     = new();
    static readonly Dictionary<Type, ConstructorInfo> validationContextTypeCache = new();

    public IEnumerable<ModelValidationResult> Validate(ModelValidationContext context)
    {
        if (context.Model == null) return []; // empty model -> no validations

        if (context.ActionContext.HttpContext.RequestServices.GetService(type) is not IValidator validator)
            throw new FluentValidatorAutoException("Validator for type {0} must be registered as scoped service", type);

        var constructorInfo   = getValidatorInstance(context.Model.GetType());
        var validatorInstance = constructorInfo.Invoke([context.Model]);

        var r = validator.Validate(validatorInstance as IValidationContext);
        return r.IsValid ? [] : r.Errors.Select(p => new ModelValidationResult(p.PropertyName, p.ErrorMessage)).ToArray();
    }

    ConstructorInfo getValidatorInstance(Type modelType)
    {
        rwLock.EnterUpgradeableReadLock();
        try
        {
            if (validationContextTypeCache.TryGetValue(modelType, out var constructorInfo))
                return constructorInfo;

            rwLock.EnterWriteLock();
            try
            {
                if (validationContextTypeCache.TryGetValue(modelType, out constructorInfo))
                    return constructorInfo;

                var genericType = typeof(ValidationContext<>).MakeGenericType(modelType);

                constructorInfo = genericType.GetConstructors().MinBy(p => p.GetParameters().Length);
                if (constructorInfo == null || constructorInfo.GetParameters().Length != 1)
                    throw new FluentValidatorAutoException("Can't find constructor with one parameter for {0}", genericType);

                validationContextTypeCache.Add(modelType, constructorInfo);

                return constructorInfo;
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
        }
        finally
        {
            rwLock.ExitUpgradeableReadLock();
        }
    }
}