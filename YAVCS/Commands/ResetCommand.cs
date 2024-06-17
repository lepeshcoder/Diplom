using YAVCS.Commands.Contracts;
using YAVCS.Services.Contracts;

namespace YAVCS.Commands;

public class ResetCommand : Command,ICommand
{

    private readonly INavigatorService _navigatorService;
    private readonly IBranchService _branchService;
    private readonly ICommitService _commitService;
    private readonly ITreeService _treeService;
    private readonly IIndexService _indexService;
    private readonly IBlobService _blobService;

    public ResetCommand(IBranchService branchService, INavigatorService navigatorService,
        ICommitService commitService, ITreeService treeService, IIndexService indexService, IBlobService blobService)
    {
        _branchService = branchService;
        _navigatorService = navigatorService;
        _commitService = commitService;
        _treeService = treeService;
        _indexService = indexService;
        _blobService = blobService;
    }

    private enum CommandCases
    {
        SyntaxError = 0,
        HelpCase = 1,
        SoftReset = 2,
        MixedReset = 3,
        HardReset = 4
    }
    
    protected override Enum GetCommandCase(string[] args)
    {
        return args switch
        {
            ["--help"] => CommandCases.HelpCase,
            ["--soft", ..] => CommandCases.SoftReset,
            ["--mixed", ..] => CommandCases.MixedReset,
            ["--hard",..] => CommandCases.HardReset,
            _ => CommandCases.SyntaxError
        };
    }

    public string Description => "Reset head to specified commit:\n" +
                                 "Format:\n" +
                                 "1) Reset with only branch reference update: yavcs reset --soft commitHash\n" +
                                 "2) Same as a soft reset, but index reset to commit state: yavcs reset --mixed commitHash\n" +
                                 "3) Same as a mixed reset, but working directory reset to commit state: yavcs reset --hard commitHash\n";
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
            case CommandCases.SoftReset:
            {
                var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
                if (vcsRootDirectoryNavigator == null)
                {
                    throw new Exception("not a part of repository");
                }
                var commitHash = args[1];
                var newHeadCommit = _commitService.GetCommitByHash(commitHash);
                if (newHeadCommit == null)
                {
                    throw new Exception("commit" + commitHash + "doesn't exist");
                }
                // set orig head
                if (!_branchService.IsDetachedHead())
                {
                    var activeBranch = _branchService.GetActiveBranch();
                    _branchService.SetOrigHead(activeBranch.CommitHash);
                    _branchService.SetPreviousBranch(activeBranch.Name);
                    _branchService.SetHead(newHeadCommit.Hash);
                }
                else
                {
                    _branchService.SetHead(newHeadCommit.Hash);
                }
                break;
            }
            case CommandCases.MixedReset:
            {
                var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
                if (vcsRootDirectoryNavigator == null)
                {
                    throw new Exception("not a part of repository");
                }
                var commitHash = args[1];
                var newHeadCommit = _commitService.GetCommitByHash(commitHash);
                if (newHeadCommit == null)
                {
                    throw new Exception("commit" + commitHash + "doesn't exist");
                }
                
                _treeService.ResetIndexToState(newHeadCommit.TreeHash);
               
                if (!_branchService.IsDetachedHead())
                {
                    var activeBranch = _branchService.GetActiveBranch();
                    _branchService.SetOrigHead(activeBranch.CommitHash);
                    _branchService.SetPreviousBranch(activeBranch.Name);
                    _branchService.SetHead(newHeadCommit.Hash);
                }
                else
                {
                    _branchService.SetHead(newHeadCommit.Hash);
                }
                break;
            }
            case CommandCases.HardReset:
            {
                var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
                if (vcsRootDirectoryNavigator == null)
                {
                    throw new Exception("not a part of repository");
                }
                var commitHash = args[1];
                var newHeadCommit = _commitService.GetCommitByHash(commitHash);
                if (newHeadCommit == null)
                {
                    throw new Exception("commit" + commitHash + "doesn't exist");
                }
                
                _treeService.ResetIndexToState(newHeadCommit.TreeHash);
                _treeService.ResetWorkingDirectoryToState(newHeadCommit.TreeHash);

                if (!_branchService.IsDetachedHead())
                {
                    var activeBranch = _branchService.GetActiveBranch();
                    _branchService.SetOrigHead(activeBranch.CommitHash);
                    _branchService.SetPreviousBranch(activeBranch.Name);
                    _branchService.SetHead(newHeadCommit.Hash);
                }
                else
                {
                    _branchService.SetHead(newHeadCommit.Hash);
                }
                break;
            }
        }
    }
}