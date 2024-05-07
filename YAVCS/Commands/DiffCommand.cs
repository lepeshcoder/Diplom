using System.Text;
using spkl.Diffs;
using YAVCS.Commands.Contracts;
using YAVCS.Models;
using YAVCS.Services.Contracts;

namespace YAVCS.Commands;

public class DiffCommand : Command,ICommand
{
    private readonly INavigatorService _navigatorService;
    private readonly ICommitService _commitService;
    private readonly ITreeService _treeService;
    private readonly IBranchService _branchService;
    private readonly IDiffService _diffService;

    public DiffCommand(INavigatorService navigatorService, ICommitService commitService,
        ITreeService treeService, IBranchService branchService, IDiffService diffService)
    {
        _navigatorService = navigatorService;
        _commitService = commitService;
        _treeService = treeService;
        _branchService = branchService;
        _diffService = diffService;
    }

    private enum CommandCases
    {
        SyntaxError = 0,
        HelpCase = 1,
        DefaultCase = 2
    }
    
    protected override Enum GetCommandCase(string[] args)
    {
        return args switch
        {
            ["--help"] => CommandCases.HelpCase,
            _ when args.Length == 1 => CommandCases.DefaultCase,
            _ => CommandCases.SyntaxError
        };
    }

    public string Description => "Show Differencies between active commit and argument commit";
    public void Execute(string[] args)
    {
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
                throw new Exception("Invalid args format");
            }
            case CommandCases.DefaultCase:
            {
                var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
                if (vcsRootDirectoryNavigator == null)
                {
                    throw new Exception("not a part of repository");
                }

                var commitToCompareHash = args[0];
                var commitToCompare = _commitService.GetCommitByHash(commitToCompareHash);
                if (commitToCompare == null)
                {
                    throw new ArgumentException("no commit with this hash");
                }

                var headCommitHash = _branchService.GetHeadCommitHash();
                var headCommit = _commitService.GetCommitByHash(headCommitHash);
                
                var headCommitIndexRecords = _treeService.GetTreeRecordsByPath(headCommit!.TreeHash);
                var commitToCompareIndexRecords = _treeService.GetTreeRecordsByPath(commitToCompare.TreeHash);

                var diffResult = _diffService.GetDiff(headCommitIndexRecords, commitToCompareIndexRecords);

                for (int i = 0; i < diffResult.Lines.Count; i++)
                {
                    Console.ForegroundColor = diffResult.LineColors[i];
                    Console.WriteLine(diffResult.Lines[i]);
                }

                break;
            }
        }
    }
    
}