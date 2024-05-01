using YAVCS.Commands.Contracts;
using YAVCS.Models;
using YAVCS.Services.Contracts;

namespace YAVCS.Commands;

public class BranchCommand : Command,ICommand
{

    private readonly IBranchService _branchService;
    private readonly INavigatorService _navigatorService;

    public BranchCommand(IBranchService branchService, INavigatorService navigatorService)
    {
        _branchService = branchService;
        _navigatorService = navigatorService;
    }

    private enum CommandCases
    {
        SyntaxError = 0,
        HelpCase = 1,
        CreateBranch = 2,
        DeleteBranch = 3,
        ShowBranches = 4
    }
    
    protected override Enum GetCommandCase(string[] args)
    {
        return args switch
        {
            ["--help"] => CommandCases.HelpCase,
            ["--show"] => CommandCases.ShowBranches,
            ["--delete", ..] => CommandCases.DeleteBranch,
            _ when args.Length == 1 => CommandCases.CreateBranch,
            _ => CommandCases.SyntaxError
        };
    }

    public string Description => "Show branches, or create new branch";
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
            case CommandCases.ShowBranches:
            {
                var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
                if (vcsRootDirectoryNavigator == null)
                {
                    throw new Exception("repository not found");
                }
                var branches = _branchService.GetAllBranches();
                foreach (var branch in branches)
                {
                    Console.WriteLine($"Branch Name: {branch.Name}");
                    Console.WriteLine($"Branch Head: {branch.CommitHash}\n");
                }
                break;
            }
            case CommandCases.CreateBranch:
            {
                var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
                if (vcsRootDirectoryNavigator == null)
                {
                    throw new Exception("repository not found");
                }
                var newBranchName = args[0];
                if (File.Exists(vcsRootDirectoryNavigator.HeadsDirectory + Path.DirectorySeparatorChar + newBranchName))
                {
                    throw new Exception($"Branch {newBranchName} already exist");
                }
                var headCommitHash = _branchService.GetHeadCommitHash();
                _branchService.CreateBranch(new BranchFileModel(newBranchName,headCommitHash));
                break;
            }
            case CommandCases.DeleteBranch:
            {
                var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
                if (vcsRootDirectoryNavigator == null)
                {
                    throw new Exception("repository not found");
                }
                var branchToDeleteName = args[1];
                var branchToDelete = _branchService.GetBranchByName(branchToDeleteName);
                
                if (branchToDelete == null)
                {
                    throw new ArgumentException($"Branch {branchToDeleteName} doesn't exist");
                }
                
                if (_branchService.IsDetachedHead())
                {
                    var previousActiveBranchName = _branchService.GetPreviousBranchName();
                    if (branchToDeleteName == previousActiveBranchName)
                    {
                        throw new ArgumentException("Cannot delete previous active branch in detached head state");
                    }
                    if (branchToDeleteName.ToLower() == "master")
                    {
                        throw new ArgumentException("Can't Delete Branch Master");
                    }
                    _branchService.DeleteBranch(branchToDeleteName);
                }
                else
                {
                    var activeBranch = _branchService.GetActiveBranch();
                    var activeBranchName = activeBranch.Name;
                    
                    if (activeBranchName == branchToDeleteName)
                    {
                        throw new ArgumentException("Cannot delete active Branch");
                    }
                    if (branchToDeleteName.ToLower() == "master")
                    {
                        throw new ArgumentException("Can't Delete Branch Master");
                    }
                    _branchService.DeleteBranch(branchToDeleteName);
                }
                break;
            }
        }
    }
}