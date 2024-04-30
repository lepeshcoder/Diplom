using System;
using System.Threading.Channels;
using YAVCS.Commands.Contracts;
using YAVCS.Exceptions;
using YAVCS.Models;
using YAVCS.Services.Contracts;

namespace YAVCS.Commands;

public class LogCommand : Command,ICommand
{

    private readonly INavigatorService _navigatorService;
    private readonly IBranchService _branchService;
    private readonly ICommitService _commitService;

    public LogCommand(INavigatorService navigatorService, IBranchService branchService, ICommitService commitService)
    {
        _navigatorService = navigatorService;
        _branchService = branchService;
        _commitService = commitService;
    }

    private enum CommandCases
    {
        HelpCase,
        DefaultCase,
        SyntaxError,
        GraphCase
    }
    
    protected override Enum GetCommandCase(string[] args)
    {
        return args switch
        {
            ["--help"] => CommandCases.HelpCase,
            ["--graph"] => CommandCases.GraphCase,
            _ when args.Length == 0 => CommandCases.DefaultCase,
            _ => CommandCases.SyntaxError
        };
    }

    public string Description => "Show commits on this branch";
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
                Console.WriteLine("Invalid args format");
                break;
            }
            case CommandCases.DefaultCase:
            {
                var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
                if (vcsRootDirectoryNavigator == null)
                {
                    throw new RepositoryNotFoundException("Log.Execute");
                }
                var headCommitHash = _branchService.GetHeadCommitHash();
                var headCommit = _commitService.GetCommitByHash(headCommitHash);
                
                var allBranches = _branchService.GetAllBranches();
                while (headCommit != null)
                {
                    ShowCommitInfo(headCommit,allBranches);
                    //TODO: СДЕЛАТЬ ЧТО ТО С ЛОГОМ
                    // check for zero commit
                    if (headCommit.ParentCommitHashes.Count == 0) return;
                    var parentCommit = _commitService.GetCommitByHash(headCommit.ParentCommitHashes[0]);
                    if (parentCommit == null) break;
                    headCommit = parentCommit;
                } 
                break;
            }
            case CommandCases.GraphCase:
            {
                var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
                if (vcsRootDirectoryNavigator == null)
                {
                    throw new RepositoryNotFoundException("Log.Execute");
                }
                var headCommitHash = _branchService.GetHeadCommitHash();
                var headCommit = _commitService.GetCommitByHash(headCommitHash);
                
                var allBranches = _branchService.GetAllBranches();

                var ancestors = new Queue<string>([headCommit!.Hash]);
                var commitHashesList = new List<string>();
                while (ancestors.Count != 0)
                {
                    var ancestorCommit = _commitService.GetCommitByHash(ancestors.Dequeue());
                    commitHashesList.Add(ancestorCommit!.Hash);
                    foreach (var ancestorCommitParentHash in ancestorCommit!.ParentCommitHashes)
                    {
                        ancestors.Enqueue(ancestorCommitParentHash);
                    }
                }

                var sortedCommitHashesList = commitHashesList.ToHashSet().OrderByDescending(hash=> _commitService.GetCommitByHash(hash)!.CreatedAt).ToList();
                foreach (var commitHash in sortedCommitHashesList)
                {
                    var commit = _commitService.GetCommitByHash(commitHash);
                    ShowCommitInfo(commit!,allBranches);
                }

                break;
            }
        }
    }


    private void ShowCommitInfo(CommitFileModel commit,List<BranchFileModel> allBranches)
    {
        var branchesOnCommit = allBranches.Where(branch => branch.CommitHash == commit.Hash);
        foreach (var branch in branchesOnCommit)
        {
            Console.Write(branch.Name + " ");
        }
        Console.WriteLine($"\nCommit message: {commit.Message}");
        Console.WriteLine($"Commit Time: {commit.CreatedAt}");
        Console.WriteLine($"Commit Hash:{commit.Hash}");
        Console.WriteLine('\n');
    }
}