using YAVCS.Commands.Contracts;
using YAVCS.Services.Contracts;

namespace YAVCS.Commands;

public class CommitCommand : Command, ICommand
{

    private readonly INavigatorService _navigatorService;
    private readonly ITreeService _treeService;
    private readonly ICommitService _commitService;
    private readonly IGarbageCollectorService _garbageCollectorService;
    private readonly IBranchService _branchService;
    private readonly IMergeService _mergeService;

    public CommitCommand(INavigatorService navigatorService, ITreeService treeService, ICommitService commitService,
        IGarbageCollectorService garbageCollectorService, IBranchService branchService, IMergeService mergeService)
    {
        _navigatorService = navigatorService;
        _treeService = treeService;
        _commitService = commitService;
        _garbageCollectorService = garbageCollectorService;
        _branchService = branchService;
        _mergeService = mergeService;
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
            _ when args.Length != 0 => CommandCases.DefaultCase,
            _ => CommandCases.SyntaxError
        };
    }

    public string Description => "Take a snapshot of repository and save it as a version";
    public void Execute(string[] args)
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        if (vcsRootDirectoryNavigator == null)
        {
            throw new Exception("repository not found");
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
                throw new Exception("Invalid args format");
            }
            case CommandCases.DefaultCase:
            {
                if (_commitService.IsIndexSameFromHead())
                {
                    throw new Exception("There is nothing to commit");
                }
                
                var headCommitHash = _branchService.GetHeadCommitHash();
                var rootTreeHash = _treeService.CreateTreeByIndex();
                var commitMessage = args.Aggregate("", (current, arg) => current + arg + " ");
                var parentCommitHashes = (_mergeService.IsOnMergeConflict()
                    ?_mergeService.GetMergeBranches() 
                    :[headCommitHash])
                    .ToList();
                var newCommit = _commitService.CreateCommit(rootTreeHash,DateTime.Now,commitMessage,parentCommitHashes);

                if (_mergeService.IsOnMergeConflict())
                {
                    _mergeService.ResetMergeConflictSign();
                }
                
                if (_branchService.IsDetachedHead())
                {
                    _branchService.SetHead(newCommit.Hash);
                }
                else
                {
                    var activeBranch = _branchService.GetActiveBranch();
                    _branchService.UpdateBranch(activeBranch.Name,newCommit.Hash);
                   // _garbageCollectorService.CollectGarbage();
                }
                break; 
            }
        }
    }
}