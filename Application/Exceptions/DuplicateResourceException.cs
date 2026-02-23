namespace BusinessDirectory.Application.Exceptions;

public sealed class DuplicateResourceException : Exception
{
    public DuplicateResourceException(string message) : base(message)
    {
    }
}
