using YAVCS.Commands.Contracts;
using YAVCS.Services.Contracts;

namespace YAVCS.Commands;

public class SwitchCommand : Command,ICommand
{

    private readonly INavigatorService _navigatorService;
    private readonly IBranchService _branchService;
    private readonly ITreeService _treeService;
    private readonly ICommitService _commitService;

    public SwitchCommand(INavigatorService navigatorService, IBranchService branchService,
        ITreeService treeService, ICommitService commitService)
    {
        _navigatorService = navigatorService;
        _branchService = branchService;
        _treeService = treeService;
        _commitService = commitService;
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

    public string Description => "Switch between branches\n" +
                                 "Format:\n" +
                                 "1) switch to specified branch: yavcs switch branchName\n";
    
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
                throw new Exception("Invalid args Format");
            }
            case CommandCases.DefaultCase:
            {
                var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
                if (vcsRootDirectoryNavigator == null)
                {
                    throw new Exception("not a part of repository");
                }
                var branchToSwitchName = args[0];
                var branchToSwitch = _branchService.GetBranchByName(branchToSwitchName);
                
                if (branchToSwitch == null)
                {
                    throw new ArgumentException($"Branch {branchToSwitchName} doesn't exist");
                }

                if (_branchService.IsDetachedHead())
                {
                    var previousActiveBranchName = _branchService.GetPreviousBranchName();
                    var previousActiveBranch = _branchService.GetBranchByName(previousActiveBranchName);
                    var origHeadCommitHash = _branchService.GetOrigHeadCommitHash();

                    if (branchToSwitch.Name == previousActiveBranchName)
                    {
                        var origHeadCommit = _commitService.GetCommitByHash(origHeadCommitHash);
                        _treeService.ResetIndexToState(origHeadCommit!.TreeHash);
                        _treeService.ResetWorkingDirectoryToState(origHeadCommit.TreeHash);
                        _branchService.SetActiveBranch(previousActiveBranch!);
                    }
                    else
                    {
                        var branchToSwitchHeadCommit = _commitService.GetCommitByHash(branchToSwitch.CommitHash);
                        _treeService.ResetIndexToState(branchToSwitchHeadCommit!.TreeHash);
                        _treeService.ResetWorkingDirectoryToState(branchToSwitchHeadCommit.TreeHash);
                        _branchService.SetActiveBranch(branchToSwitch);
                    }
                    _branchService.UpdateBranch(previousActiveBranchName,origHeadCommitHash);
                    _branchService.SetOrigHead("");
                    _branchService.SetPreviousBranch("");
                }
                else
                {
                    var activeBranch = _branchService.GetActiveBranch();
                    var activeBranchName = activeBranch.Name;
                    if (activeBranchName == branchToSwitchName)
                    {
                        throw new ArgumentException($"Already on branch {activeBranchName}");
                    }

                    var activeBranchCommitHash = activeBranch.CommitHash;
                    var branchToSwitchHeadCommit = _commitService.GetCommitByHash(branchToSwitch.CommitHash);
                
                    if (activeBranchCommitHash != branchToSwitch.CommitHash)
                    {
                        _treeService.ResetIndexToState(branchToSwitchHeadCommit!.TreeHash);
                        _treeService.ResetWorkingDirectoryToState(branchToSwitchHeadCommit.TreeHash);
                    }
                    _branchService.SetActiveBranch(branchToSwitch);
                }
              
                break;
            }
        }
    }
}