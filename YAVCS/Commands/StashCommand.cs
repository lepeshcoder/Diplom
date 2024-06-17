using System.Security.AccessControl;
using System.Threading.Channels;
using YAVCS.Commands.Contracts;
using YAVCS.Models;
using YAVCS.Services.Contracts;

namespace YAVCS.Commands;

public class StashCommand : Command,ICommand
{
    private readonly INavigatorService _navigatorService;
    private readonly ICommitService _commitService;
    private readonly ITreeService _treeService;
    private readonly IHashService _hashService;
    private readonly IBranchService _branchService;
    private readonly IStashService _stashService;
    private readonly IDiffService _diffService;
    private readonly IMergeService _mergeService;

    public StashCommand(INavigatorService navigatorService, ICommitService commitService, ITreeService treeService,
        IHashService hashService, IBranchService branchService, IStashService stashService, IDiffService diffService, IMergeService mergeService)
    {
        _navigatorService = navigatorService;
        _commitService = commitService;
        _treeService = treeService;
        _hashService = hashService;
        _branchService = branchService;
        _stashService = stashService;
        _diffService = diffService;
        _mergeService = mergeService;
    }

    public string Description => "Save Local changes and resets to head state\n" +
                                 "Format:\n" +
                                 "1) Push local changes: yavcs stash --push\n" +
                                 "2) Pop local cnanges: yavcs stash --pop\n" +
                                 "3) Show stash commits: yavcs stash --list\n" +
                                 "4) Show what  stash commit add to current version: yavcs stash --show stashCommitNumber (from 0)\n";
    
    private enum CommandCases
    {
        SyntaxError = 0,
        HelpCase = 1,
        PushCase = 2,
        PopCase = 3,
        ShowCase = 4,
        ListCase = 5
    }
    
    protected override Enum GetCommandCase(string[] args)
    {
        return args switch
        {
            ["--help"] => CommandCases.HelpCase,
            ["--push"] => CommandCases.PushCase,
            ["--pop"] => CommandCases.PopCase,
            ["--list"] => CommandCases.ListCase,
            ["--show", ..] => CommandCases.ShowCase,
            _ => CommandCases.SyntaxError
        };
    }
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
            case CommandCases.PushCase:
            {
                //done
                var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
                if (vcsRootDirectoryNavigator == null)
                {
                    throw new Exception("Repository not Found");
                }
                
                var currentStateTreeHash = _treeService.CreateTreeByWorkingDirectory();
                var headCommitHash = _branchService.GetHeadCommitHash();
                var headCommit = _commitService.GetCommitByHash(headCommitHash);
                
                var headCommitTreeRecords = _treeService.GetTreeRecordsByPath(headCommit!.TreeHash).Values.ToHashSet();
                var currentStateTreeRecords = _treeService.GetTreeRecordsByPath(currentStateTreeHash).Values.ToHashSet();
                
                if (headCommitTreeRecords.SetEquals(currentStateTreeRecords))
                {
                    throw new Exception("There is no local changes, nothing to stash");
                }

                //create stash Commit
                var createdAt = DateTime.Now;
                const string message = "stashCommit";
                var stashCommitHash = _hashService.GetHash(currentStateTreeHash + createdAt + message);
                var stashCommit = new StashCommitFileModel(currentStateTreeHash, createdAt, message, stashCommitHash, [],
                    headCommitHash);
                _stashService.Push(stashCommit);
                
                // RESET WORKING TREE AND INDEX TO HEAD STATE
                _treeService.ResetIndexToState(headCommit.TreeHash);
                _treeService.ResetWorkingDirectoryToState(headCommit.TreeHash);
                
                break;
            }
            case CommandCases.PopCase:
            {
                var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
                if (vcsRootDirectoryNavigator == null)
                {
                    throw new Exception("Repository not Found");
                }

                var headCommitHash = _branchService.GetHeadCommitHash();
                var stashHeadCommit = _stashService.GetHeadStashCommit();
                
                if (stashHeadCommit == null)
                {
                    throw new Exception("Nothing to pop,stash is clear");
                }

                if (headCommitHash != stashHeadCommit.BaseCommitHash)
                {
                    throw new Exception("Cannot pop stash on not base commit");
                }
                
                var stashCommit = _stashService.Pop();
                var currentTreeHash = _treeService.CreateTreeByWorkingDirectory();
                
                var currentRecords = _treeService.GetTreeRecordsByPath(currentTreeHash);
                var stashRecords = _treeService.GetTreeRecordsByPath(stashCommit!.TreeHash);

                var headCommit = _commitService.GetCommitByHash(_branchService.GetHeadCommitHash());
                var baseCommitIndexRecords = _treeService.GetTreeRecordsByPath(headCommit!.TreeHash);
                
                var mergeResult = _mergeService.Merge(baseCommitIndexRecords,
                        currentRecords,
                        stashRecords,
                        "HEAD", 
                        "stash");

                var mergeTreeHash = _treeService.CreateTreeByRecords(mergeResult.IndexRecords);
                _treeService.ResetWorkingDirectoryToState(mergeTreeHash);
                
                var isMergeConflict = mergeResult.ConflictPaths.Count != 0;
                if (isMergeConflict)
                {
                    Console.WriteLine("There are some Merge conflicts in:\n");
                    foreach (var conflictPath in mergeResult.ConflictPaths)
                    {
                        Console.WriteLine(conflictPath);
                    }
                }
                
                break;
            }
            case CommandCases.ListCase:
            {
                var stashCommits = _stashService.GetStashCommits().ToList();
                if (stashCommits.Count == 0)
                {
                    throw new Exception("There isn't any stash commits");
                }
                
                var i = 0;
                foreach (var stashCommit in stashCommits)
                {
                    var baseCommit = _commitService.GetCommitByHash(stashCommit.BaseCommitHash);
                    Console.WriteLine($"stash [{i++}] on commit {stashCommit.BaseCommitHash[..5]} {baseCommit!.Message}");
                }
                break;
            }
            case CommandCases.ShowCase:
            {
                //done
                var vcsRootDirectory = _navigatorService.TryGetRepositoryRootDirectory();
                if (vcsRootDirectory == null)
                {
                    throw new Exception("Repository not Found");
                }
                
                var success = int.TryParse(args[1],out var stashCommitNumber);
                if (!success)
                {
                    throw new Exception("index not a number");
                }
                
                var stashCommits = _stashService.GetStashCommits().ToList();
                if (stashCommitNumber >= stashCommits.Count)
                {
                    throw new Exception($"There isn't {stashCommitNumber} index in stash queue");
                }
                

                var stashCommit = stashCommits[stashCommitNumber];
                var headCommitHash = _branchService.GetHeadCommitHash();
                
                if (stashCommit.BaseCommitHash != headCommitHash)
                {
                    throw new Exception("Cannot show diff between stash on not baseCommit");
                }
                
                var currentTreeHash = _treeService.CreateTreeByWorkingDirectory();
                var currentRecords = _treeService.GetTreeRecordsByPath(currentTreeHash);
                var stashCommitRecords = _treeService.GetTreeRecordsByPath(stashCommit.TreeHash);

                var diffResult = _diffService.GetDiff(stashCommitRecords, currentRecords);

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