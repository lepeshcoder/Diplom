using YAVCS.Commands.Contracts;
using YAVCS.Services.Contracts;

namespace YAVCS.Commands;

public class UnStageCommand : Command,ICommand
{
    
    private readonly INavigatorService _navigatorService;
    private readonly IIndexService _indexService;
    private readonly IBranchService _branchService;
    private readonly ITreeService _treeService;
    private readonly ICommitService _commitService;
    

    public UnStageCommand(INavigatorService navigatorService, IIndexService indexService,
        IBranchService branchService, ITreeService treeService, ICommitService commitService)
    {
        _navigatorService = navigatorService;
        _indexService = indexService;
        _branchService = branchService;
        _treeService = treeService;
        _commitService = commitService;
    }

    private enum CommandCases
    {
        SyntaxError = 0,
        HelpCase = 1,
        DefaultCase = 2,
        UnStageAll = 3,
        ForceCase= 4
    }
    
    protected override Enum GetCommandCase(string[] args)
    {
        return args switch
        {
            ["--help"]  => CommandCases.HelpCase,
            ["--force", ..] => CommandCases.ForceCase, 
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
                throw new Exception("Args syntax error");
            }
            case CommandCases.ForceCase:
            {
                var itemRelativePath = args[1];
                var itemAbsolutePath = vcsRootDirectoryNavigator.RepositoryRootDirectory + Path.DirectorySeparatorChar + itemRelativePath;
                if (Directory.Exists(itemAbsolutePath))
                {
                    UnStageDirectory(itemAbsolutePath,true);
                }
                else
                {
                    UnStageFile(itemRelativePath,true);
                }
                break;
            }
            case CommandCases.DefaultCase:
            {
                var itemRelativePath = args[0];
                var itemAbsolutePath = vcsRootDirectoryNavigator.RepositoryRootDirectory + Path.DirectorySeparatorChar + itemRelativePath;
                if (Directory.Exists(itemAbsolutePath))
                {
                    UnStageDirectory(itemAbsolutePath);
                }
                else
                {
                    UnStageFile(itemRelativePath);
                }
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

    private void UnStageFile(string fileRelativePath,bool isForce = false)
    {
        var headCommitHash = _branchService.GetHeadCommitHash();
        var headCommit = _commitService.GetCommitByHash(headCommitHash);
        var headIndexRecords = _treeService.GetTreeRecordsByPath(headCommit!.TreeHash);
        var record = _indexService.TryGetRecordByPath(fileRelativePath);
        //if file not staged
        if (record == null)
        {
            throw new Exception("file isn't staged");
        }
        if (isForce)
        {
            _indexService.DeleteRecord(record.RelativePath);
        }
        else
        {
            // if file added to index in previous commits
            if (headIndexRecords.TryGetValue(record.RelativePath, out var headIndexRecord))
            {
                // change current index record to previous state
                _indexService.DeleteRecord(record.RelativePath);
                _indexService.AddRecord(headIndexRecord);
            }
            else
            {
                _indexService.DeleteRecord(fileRelativePath);
            }
        }
        _indexService.SaveChanges();
    }

    private void UnStageDirectory(string dirAbsolutePath,bool isForce = false)
    {
        var repositoryRootDirectory = _navigatorService.TryGetRepositoryRootDirectory()!.RepositoryRootDirectory;
        foreach (var entry in  Directory.GetFileSystemEntries(dirAbsolutePath))
        {
            if (File.Exists(entry))
            {
                var fileRelativePath = Path.GetRelativePath(repositoryRootDirectory, entry);
                try
                { 
                    UnStageFile(fileRelativePath,isForce);
                }
                catch (Exception e)
                {
                    // ignore file isn't staged exception   
                }
            }
            else if (Directory.Exists(entry))
            {
                UnStageDirectory(entry,isForce);
            }
            else throw new Exception("Unknown fileSystem entry");
        }
    }
    
}