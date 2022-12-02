namespace FluentValidation.Auto;

public class FluentValidatorAutoException : Exception
{
    public FluentValidatorAutoException(string formatString, params object[] parms) : base(string.Format(formatString, parms))
    {
    }
}