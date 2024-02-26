namespace YAVCS.Logger;

public interface ILogger
{
    void Log(string message,string messageSource = "");
}