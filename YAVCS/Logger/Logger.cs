using YAVCS.Services.Contracts;

namespace YAVCS.Logger;

public class Logger : ILogger
{

    private readonly INavigatorService _navigatorService;

    public Logger(INavigatorService navigatorService)
    {
        _navigatorService = navigatorService;
    }

    public void Log(string message,string messageSource = "")
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        var logFile = vcsRootDirectoryNavigator!.LogFile;
        var messageToWrite = messageSource + '\n' + message + '\n';
        File.AppendAllText(logFile,messageToWrite);
    }
}