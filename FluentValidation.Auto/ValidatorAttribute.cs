namespace FluentValidation.Auto;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ValidatorAttribute<T> : Attribute where T : IValidator
{
}