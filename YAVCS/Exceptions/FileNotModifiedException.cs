namespace YAVCS.Exceptions;

public class FileNotModifiedException : Exception
{
    public FileNotModifiedException()
    {
    }

    public FileNotModifiedException(string? message) : base(message)
    {
    }
}