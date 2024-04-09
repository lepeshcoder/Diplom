namespace YAVCS.Exceptions;

public class CommitNotFoundException : Exception
{
    public CommitNotFoundException()
    {
    }

    public CommitNotFoundException(string? message) : base(message)
    {
    }
}