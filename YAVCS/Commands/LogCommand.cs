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
        SyntaxError
    }
    
    protected override Enum GetCommandCase(string[] args)
    {
        return args switch
        {
            ["--help"] => CommandCases.HelpCase,
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
                var activeBranch = _branchService.GetActiveBranch();
                var currentCommit = _commitService.GetCommitByHash(activeBranch.CommitHash);
                while (currentCommit != null)
                {
                    ShowCommitInfo(currentCommit);
                    currentCommit = _commitService.GetCommitByHash(currentCommit.ParentCommitHash);
                }
                break;
            }
        }
    }


    private void ShowCommitInfo(CommitFileModel commit)
    {
        Console.WriteLine($"Commit message: {commit.Message}");
        Console.WriteLine($"Commit Time: {commit.CreatedAt}");
        Console.WriteLine($"Commit Hash:{commit.Hash}");
        Console.WriteLine('\n');
    }
}