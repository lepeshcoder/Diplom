namespace YAVCS.Exceptions;

public class FileAlreadyExistException : Exception
{
    public FileAlreadyExistException()
    {
    }

    public FileAlreadyExistException(string? message) : base(message)
    {
    }
}