using System.Threading.Channels;
using YAVCS.Commands.Contracts;
using YAVCS.Exceptions;
using YAVCS.Models;
using YAVCS.Services.Contracts;

namespace YAVCS.Commands;

public class StatusCommand : Command,ICommand
{


    private readonly INavigatorService _navigatorService;
    private readonly IIndexService _indexService;
    private readonly IHashService _hashService;

    private readonly List<string> _stagedItems = [];
    private readonly List<string> _unStagedItems = [];
    private readonly List<string> _unTrackedItems = [];
    private Dictionary<string, IndexRecord> _recordsByPath = new();
    
    private enum CommandCases
    {
        SyntaxError = 0,
        HelpCase = 1,
        DefaultCase = 2
    }

    public StatusCommand(INavigatorService navigatorService, IIndexService indexService, IHashService hashService)
    {
        _navigatorService = navigatorService;
        _indexService = indexService;
        _hashService = hashService;
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
                Console.WriteLine("Invalid args format");
                break;
            }
            case CommandCases.DefaultCase:
            {
                var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
                if (vcsRootDirectoryNavigator is null)
                {
                    throw new RepositoryNotFoundException("StatusCommand.Execute");
                }
                
                GetStatusInfo();
                
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
        _recordsByPath = _indexService.GetRecords();
        
        FillTrackedItems(_recordsByPath);
        FillUnTrackedItems(_recordsByPath);
    }

    private void FillTrackedItems(Dictionary<string, IndexRecord> recordsByPath)
    {
        var repositoryRootDirectory = _navigatorService.TryGetRepositoryRootDirectory()!.RepositoryRootDirectory;
        // fill staged / unStaged items
        var records = recordsByPath.Values.ToList();
        foreach (var record in records)
        {
            var itemAbsolutePath = repositoryRootDirectory + Path.DirectorySeparatorChar + record.RelativePath;
            if (record.IsNew) 
                _stagedItems.Add("new file: " + record.RelativePath);
            else 
                _stagedItems.Add("modified: " + record.RelativePath);
            if (File.Exists(itemAbsolutePath))
            {
                var byteData = File.ReadAllBytes(itemAbsolutePath);
                var newHash = _hashService.GetHash(byteData);
                if (record.BlobHash != newHash)
                {
                    _unStagedItems.Add("modified: " + record.RelativePath);
                }
            }
            else
            {
                _unStagedItems.Add("deleted: " + record.RelativePath);
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
                if (!_recordsByPath.ContainsKey(fileRelativePath))
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
                    var entryRelativePath = Path.GetRelativePath(repositoryRootDirectory, entry) + '/';
                    items.Add(entryRelativePath);
                }
            }
            else throw new Exception("Unknown entry");
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