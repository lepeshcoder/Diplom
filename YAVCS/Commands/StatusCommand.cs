using YAVCS.Commands.Contracts;
using YAVCS.Models;
using YAVCS.Services.Contracts;

namespace YAVCS.Commands;

public class StatusCommand : Command,ICommand
{
    private readonly INavigatorService _navigatorService;
    private readonly IIndexService _indexService;
    private readonly IHashService _hashService;
    private readonly ICommitService _commitService;
    private readonly IBranchService _branchService;

    private readonly List<string> _stagedItems = [];
    private readonly List<string> _unStagedItems = [];
    private readonly List<string> _unTrackedItems = [];
    
    private Dictionary<string, IndexRecord> _indexRecordsByPath = new();
    private Dictionary<string, IndexRecord> _headRecordsByPath = new();
    
    private enum CommandCases
    {
        SyntaxError = 0,
        HelpCase = 1,
        DefaultCase = 2
    }

    public StatusCommand(INavigatorService navigatorService, IIndexService indexService,
        IHashService hashService, ICommitService commitService, IBranchService branchService)
    {
        _navigatorService = navigatorService;
        _indexService = indexService;
        _hashService = hashService;
        _commitService = commitService;
        _branchService = branchService;
    }

    public string Description => "Show status of working tree";
    
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
                if (vcsRootDirectoryNavigator is null)
                {
                    throw new Exception("Repository not found");
                }
                
                GetStatusInfo();

                if (_branchService.IsDetachedHead())
                {
                    var headCommitHash = _branchService.GetHeadCommitHash();
                    Console.WriteLine($"Detached head set to {headCommitHash}");
                }
                else
                {
                    var activeBranch = _branchService.GetActiveBranch();
                    Console.WriteLine($"On branch {activeBranch.Name}\n");
                }
                
                Console.WriteLine("Staged Items:");
                foreach (var item in _stagedItems) Console.WriteLine(item);
                
                Console.WriteLine("\nUnStaged Items:");
                foreach (var item in _unStagedItems) Console.WriteLine(item);

                Console.WriteLine("\nUnTracked Items:");
                foreach (var item in _unTrackedItems) Console.WriteLine(item);
                
                break;
            }
        }
    }

    protected override Enum GetCommandCase(string[] args)
    {
        return args switch
        {
            [] => CommandCases.DefaultCase,
            ["--help"]  => CommandCases.HelpCase,
            _ => CommandCases.SyntaxError
        };
    }

    private void GetStatusInfo()
    {
        _indexRecordsByPath = _indexService.GetRecords();
        _headRecordsByPath = _commitService.GetHeadRecordsByPath();
        FillTrackedItems(_indexRecordsByPath,_headRecordsByPath);
        FillUnTrackedItems(_indexRecordsByPath);
    }

    private void FillTrackedItems(Dictionary<string, IndexRecord> indexRecordsByPath,Dictionary<string,IndexRecord> headRecordsByPath)
    {
        var repositoryRootDirectory = _navigatorService.TryGetRepositoryRootDirectory()!.RepositoryRootDirectory;
        //fill stagedItems
        foreach (var indexRecord in indexRecordsByPath.Values)
        {
            var recordPath = indexRecord.RelativePath;
            if (!headRecordsByPath.ContainsKey(recordPath))
            {
                _stagedItems.Add($"new file: {recordPath}");
            }
            else
            {
                var headRecord = headRecordsByPath[recordPath];
                if (indexRecord.BlobHash != headRecord.BlobHash)
                {
                    _stagedItems.Add($"modified: {recordPath}");
                }
            }
        } 
        //fill unstaged items
        foreach (var indexRecord in indexRecordsByPath.Values)
        {
            var recordAbsolutePath = repositoryRootDirectory + Path.DirectorySeparatorChar + indexRecord.RelativePath;
            if (!File.Exists(recordAbsolutePath))
            {
                _unStagedItems.Add($"deleted: {indexRecord.RelativePath}");
            }
            else
            {
                var fileData = File.ReadAllBytes(recordAbsolutePath);
                var newHash = _hashService.GetHash(fileData);
                if (newHash != indexRecord.BlobHash)
                {
                    _unStagedItems.Add($"modified: {indexRecord.RelativePath}");
                }
            }
        }
        
    }
    
    private void FillUnTrackedItems(Dictionary<string, IndexRecord> recordsByPath)
    {
        var repositoryRootDirectory = _navigatorService.TryGetRepositoryRootDirectory()!.RepositoryRootDirectory;
        //fill untracked items
        CheckDirectory(repositoryRootDirectory);
    }

    private bool CheckDirectory(string dirAbsolutePath,bool isRoot = true)
    {
        var repositoryRootDirectory = _navigatorService.TryGetRepositoryRootDirectory()!.RepositoryRootDirectory;
        var isDirHidden = true;
        List<string> items = [];
        foreach (var entry in Directory.GetFileSystemEntries(dirAbsolutePath))
        {
            if (File.Exists(entry))
            {
                var fileRelativePath = Path.GetRelativePath(repositoryRootDirectory, entry);
                if (!_indexRecordsByPath.ContainsKey(fileRelativePath))
                    items.Add(fileRelativePath);
                else isDirHidden = false;
            }
            else if (Directory.Exists(entry))
            {
                var isInnerDirectoryHidden = CheckDirectory(entry,false);
                if (!isInnerDirectoryHidden)
                {
                    isDirHidden = false;
                }
                else
                {
                    var entryRelativePath = Path.GetRelativePath(repositoryRootDirectory, entry) + Path.DirectorySeparatorChar;
                    items.Add(entryRelativePath);
                }
            }
        }
        
        if (isDirHidden)
        {
            if (isRoot)
            {
                foreach (var item in items)
                {
                    _unTrackedItems.Add(item);
                }
            }
            return true;
        }
        else
        {
            foreach (var item in items)
            {
                _unTrackedItems.Add(item);
            }
        }
        return isDirHidden;
    }
}