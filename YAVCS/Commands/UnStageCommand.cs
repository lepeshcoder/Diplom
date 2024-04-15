using YAVCS.Commands.Contracts;
using YAVCS.Exceptions;
using YAVCS.Services.Contracts;

namespace YAVCS.Commands;

public class UnStageCommand : Command,ICommand
{
    
    private readonly INavigatorService _navigatorService;
    private readonly IIndexService _indexService;

    public UnStageCommand(INavigatorService navigatorService, IIndexService indexService)
    {
        _navigatorService = navigatorService;
        _indexService = indexService;
    }

    private enum CommandCases
    {
        SyntaxError = 0,
        HelpCase = 1,
        DefaultCase = 2,
        UnStageAll = 3
    }
    
    protected override Enum GetCommandCase(string[] args)
    {
        return args switch
        {
            ["--help"]  => CommandCases.HelpCase,
            ["--all"] or ["--a"] => CommandCases.UnStageAll,
            _ when args.Length == 1 => CommandCases.DefaultCase,
            _ => CommandCases.SyntaxError
        };
    }
    
    public string Description => "Remove item from staging area (index)";
    
    public void Execute(string[] args)
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        if (vcsRootDirectoryNavigator == null)
        {
            throw new RepositoryNotFoundException("UnStageCommand.Execute");
        }
        var commandCase = GetCommandCase(args);
        switch (commandCase)
        {
            case CommandCases.HelpCase:
            {
                Console.WriteLine(Description);
                break;
            }
            case CommandCases.SyntaxError:
            {
                Console.WriteLine("Args syntax error");
                break;
            }
            case CommandCases.DefaultCase:
            {
                var itemRelativePath = args[0];
                var itemAbsolutePath = vcsRootDirectoryNavigator.RepositoryRootDirectory + '\\' + itemRelativePath;
                if (File.Exists(itemAbsolutePath))
                {
                    UnStageFile(itemRelativePath);
                }
                else if (Directory.Exists(itemAbsolutePath))
                {
                    UnStageDirectory(itemAbsolutePath);
                }
                else Console.WriteLine($"{itemAbsolutePath} doesn't exist");
                break;
            }
            case CommandCases.UnStageAll:
            {
                var repositoryRootDirectory = vcsRootDirectoryNavigator.RepositoryRootDirectory;
                UnStageDirectory(repositoryRootDirectory);
                break;
            }
        }
    }

    private void UnStageFile(string fileRelativePath)
    {
        var record = _indexService.TryGetRecordByPath(fileRelativePath);
        //if file not staged
        if (record == null)
        {
            throw new Exception("file isn't staged");
        }
        _indexService.DeleteRecord(fileRelativePath);
        _indexService.SaveChanges();
    }

    private void UnStageDirectory(string dirAbsolutePath)
    {
        var repositoryRootDirectory = _navigatorService.TryGetRepositoryRootDirectory()!.RepositoryRootDirectory;
        foreach (var entry in  Directory.GetFileSystemEntries(dirAbsolutePath))
        {
            if (File.Exists(entry))
            {
                var fileRelativePath = Path.GetRelativePath(repositoryRootDirectory, entry);
                try
                { 
                    UnStageFile(fileRelativePath);
                }
                catch (Exception e)
                {
                    
                }
            }
            else if (Directory.Exists(entry))
            {
                UnStageDirectory(entry);
            }
            else throw new Exception("Unknown fileSystem entry");
        }
    }
    
}