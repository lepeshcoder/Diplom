namespace YAVCS.Exceptions;

public class ItemNoTrackException : Exception
{
    public ItemNoTrackException()
    {
    }

    public ItemNoTrackException(string? message) : base(message)
    {
    }
}