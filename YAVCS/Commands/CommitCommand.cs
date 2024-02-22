using YAVCS.Commands.Contracts;
using YAVCS.Exceptions;
using YAVCS.Services.Contracts;

namespace YAVCS.Commands;

public class CommitCommand : Command, ICommand
{

    private readonly INavigatorService _navigatorService;
    private readonly IIndexService _indexService;
    private readonly ITreeService _treeService;
    private readonly ICommitService _commitService;

    public CommitCommand(INavigatorService navigatorService, IIndexService indexService, ITreeService treeService, ICommitService commitService)
    {
        _navigatorService = navigatorService;
        _indexService = indexService;
        _treeService = treeService;
        _commitService = commitService;
    }

    private enum CommandCases
    {
        SyntaxError = 0,
        HelpCase = 1,
        DefaultCase = 2,
    }
    
    protected override Enum GetCommandCase(string[] args)
    {
        return args switch
        {
            ["--help"] => CommandCases.HelpCase,
            ["--m"] => CommandCases.DefaultCase,
            _ => CommandCases.SyntaxError
        };
    }

    public string Description => "Take a snapshot of repository and save it as a version";
    public void Execute(string[] args)
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        if (vcsRootDirectoryNavigator == null)
        {
            throw new RepositoryNotFoundException("Commit.Execute");
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
                Console.WriteLine("Invalid args format");
                break;
            }
            case CommandCases.DefaultCase:
            {
                if (_indexService.IsIndexEmpty())
                {
                    throw new EmptyIndexException("Commit.Execute");
                }
                var rootTreeHash = _treeService.CreateTreeByIndex();
                var commitMessage = args[1];
                _commitService.CreateCommit(rootTreeHash,DateTime.Now,commitMessage);
                break;
            }
        }
    }
}