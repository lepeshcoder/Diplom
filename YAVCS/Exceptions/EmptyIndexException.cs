namespace YAVCS.Exceptions;

public class EmptyIndexException : Exception
{
    public EmptyIndexException()
    {
    }

    public EmptyIndexException(string? message) : base(message)
    {
    }
}