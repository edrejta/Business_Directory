namespace BusinessDirectory.Application.Exceptions;

public sealed class EmailNotVerifiedException : Exception
{
    public EmailNotVerifiedException(string message) : base(message)
    {
    }
}
