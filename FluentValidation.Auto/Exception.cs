namespace FluentValidation.Auto;

public class FluentValidatorAutoException(string formatString, params object[] parms) : Exception(string.Format(formatString, parms));