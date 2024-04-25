using System.Text;
using spkl.Diffs;
using YAVCS.Commands.Contracts;
using YAVCS.Exceptions;
using YAVCS.Models;
using YAVCS.Services.Contracts;

namespace YAVCS.Commands;

public class DiffCommand : Command,ICommand
{


    private readonly INavigatorService _navigatorService;
    private readonly ICommitService _commitService;
    private readonly ITreeService _treeService;
    private readonly IBranchService _branchService;
    private readonly IBlobService _blobService;

    public DiffCommand(INavigatorService navigatorService, ICommitService commitService,
        ITreeService treeService, IBranchService branchService, IBlobService blobService)
    {
        _navigatorService = navigatorService;
        _commitService = commitService;
        _treeService = treeService;
        _branchService = branchService;
        _blobService = blobService;
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

    public string Description => "Show Differencies between active commit and argument commit";
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
                    throw new RepositoryNotFoundException("not a part of repository");
                }

                var commitToCompareHash = args[0];
                var commitToCompare = _commitService.GetCommitByHash(commitToCompareHash);
                if (commitToCompare == null)
                {
                    throw new ArgumentException("no commit with this hash");
                }

                var activeBranch = _branchService.GetActiveBranch();
                var activeBranchHeadCommit = _commitService.GetCommitByHash(activeBranch.CommitHash);
                
                var headCommitIndexRecords = _treeService.GetTreeRecordsByPath(activeBranchHeadCommit!.TreeHash);
                var commitToCompareIndexRecords = _treeService.GetTreeRecordsByPath(commitToCompare.TreeHash);
                
                // Элементы, которые есть только в первой коллекции
                var newFiles  = headCommitIndexRecords
                    .Where(pair => !commitToCompareIndexRecords.ContainsKey(pair.Key))
                    .ToDictionary(pair => pair.Key, pair => pair.Value);

                // Элементы, которые есть только во второй коллекции
                var deletedFiles = commitToCompareIndexRecords
                    .Where(pair => !headCommitIndexRecords.ContainsKey(pair.Key))
                    .ToDictionary(pair => pair.Key, pair => pair.Value);

                // Элементы, которые есть в обеих коллекциях
                var modifiedFiles = headCommitIndexRecords
                    .Where(pair => commitToCompareIndexRecords.ContainsKey(pair.Key))
                    .ToDictionary(pair => pair.Key, pair => pair.Value);

                foreach (var newFile in newFiles.Values)
                {
                    Console.WriteLine($"New file: {newFile.RelativePath}\n");
                }
                foreach (var deletedFile in deletedFiles.Values)
                {
                    Console.WriteLine($"Deleted file: {deletedFile.RelativePath}\n");
                }

                foreach (var modifiedFile in modifiedFiles.Values)
                {
                    var relativePath = modifiedFile.RelativePath;
                    var currentCommitFileBytes = _blobService.GetBlobData(headCommitIndexRecords[relativePath].BlobHash);
                    var commitToCompareFileBytes = _blobService.GetBlobData(commitToCompareIndexRecords[relativePath].BlobHash); 
                    var currentCommitFileText =  Encoding.UTF8.GetString(currentCommitFileBytes)
                        .Split([Environment.NewLine], StringSplitOptions.None);
                    var commitToCompareFileText = Encoding.UTF8.GetString(commitToCompareFileBytes)
                        .Split([Environment.NewLine], StringSplitOptions.None);
                    var metaData = $"\n{relativePath}:\n";
                    Console.WriteLine($"{metaData}\n{GetDiff(currentCommitFileText, commitToCompareFileText)}\n");
                }
                
                break;
            }
        }
    }

    private string GetDiff(string[] previousVersion,string[] currentVersion)
    {
        var diff = "";
        var myersDiff = new MyersDiff<string>(previousVersion, currentVersion);
        foreach (var part in myersDiff.GetEditScript())
        {
            var linesBeforeDifference = part.LineA >= 2 
                ? currentVersion.Skip(part.LineB - 2).Take(2).ToArray()
                : [];

            var linesToDelete = previousVersion.Skip(part.LineA).Take(part.CountA).Select(line => "- " + line).ToArray();
            var linesToAdd = currentVersion.Skip(part.LineB).Take(part.CountB).Select(line => "+ " + line).ToArray();

            var linesAfterDifference = part.LineB < currentVersion.Length - 2
                ? currentVersion.Skip(part.LineB + part.CountB).Take(2).ToArray()
                : [];


            diff += string.Join("\n", linesBeforeDifference
                .Concat(linesToDelete)
                .Concat(linesToAdd)
                .Concat(linesAfterDifference)
                .ToArray()) + "\n\n";
        }
        return diff;
    }

    
    
    
    
    
}