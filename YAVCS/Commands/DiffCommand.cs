using System.Text;
using spkl.Diffs;
using YAVCS.Commands.Contracts;
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
                throw new Exception("Invalid args format");
            }
            case CommandCases.DefaultCase:
            {
                var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
                if (vcsRootDirectoryNavigator == null)
                {
                    throw new Exception("not a part of repository");
                }

                var commitToCompareHash = args[0];
                var commitToCompare = _commitService.GetCommitByHash(commitToCompareHash);
                if (commitToCompare == null)
                {
                    throw new ArgumentException("no commit with this hash");
                }

                var headCommitHash = _branchService.GetHeadCommitHash();
                var headCommit = _commitService.GetCommitByHash(headCommitHash);
                
                var headCommitIndexRecords = _treeService.GetTreeRecordsByPath(headCommit!.TreeHash);
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
                    .Where(
                        pair => commitToCompareIndexRecords.ContainsKey(pair.Key) && 
                                pair.Value.BlobHash != commitToCompareIndexRecords[pair.Key].BlobHash)
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
                        .Split([Environment.NewLine,"\n"], StringSplitOptions.None);
                    var commitToCompareFileText = Encoding.UTF8.GetString(commitToCompareFileBytes)
                        .Split([Environment.NewLine,"\n"], StringSplitOptions.None);
                    var metaData = $"\n{relativePath}:\n";
                    Console.WriteLine(metaData+"\n");
                    var diffResult = GetDiff(commitToCompareFileText, currentCommitFileText);
                    for (var i = 0; i < diffResult.Lines.Count; i++)
                    {
                        Console.ForegroundColor = diffResult.LineColors[i];
                        Console.WriteLine(diffResult.Lines[i]);
                    }
                    Console.WriteLine("\n\n");
                }
                
                break;
            }
        }
    }

    private DiffResultModel GetDiff(string[] previousVersion,string[] currentVersion)
    {
        var result = new DiffResultModel([], []);
        var myersDiff = new MyersDiff<string>(previousVersion,currentVersion);
        var contextLinesNumber = 2;
        foreach (var part in myersDiff.GetEditScript())
        {
            var linesBeforeDifference = (part.LineA >= contextLinesNumber 
                ? previousVersion.Skip(part.LineA - contextLinesNumber).Take(contextLinesNumber)
                : previousVersion.Take(part.LineA)).ToList();
            
            var linesToDelete = previousVersion.Skip(part.LineA).Take(part.CountA).Select(line => "- " + line).ToList();
            var linesToAdd = currentVersion.Skip(part.LineB).Take(part.CountB).Select(line => "+ " + line).ToList();

            var linesAfterDifference = (part.LineA + linesToDelete.Count < previousVersion.Length - 2
                ? previousVersion.Skip(part.LineA + linesToDelete.Count).Take(2).ToArray()
                : previousVersion.Take(new Range(part.LineA + linesToDelete.Count,previousVersion.Length))).ToList();

            
            result.Lines.AddRange(linesBeforeDifference);
            result.LineColors.AddRange(Enumerable.Repeat(ConsoleColor.White,linesBeforeDifference.Count));
            result.Lines.AddRange(linesToDelete);
            result.LineColors.AddRange(Enumerable.Repeat(ConsoleColor.Red,linesToDelete.Count));
            result.Lines.AddRange(linesToAdd);
            result.LineColors.AddRange(Enumerable.Repeat(ConsoleColor.Green,linesToAdd.Count));
            result.Lines.AddRange(linesAfterDifference);
            result.LineColors.AddRange(Enumerable.Repeat(ConsoleColor.White,linesAfterDifference.Count));
            result.Lines.Add("\n\n\n\n");
            result.LineColors.Add(ConsoleColor.White);
        }
        return result;
    }

    
    
    
    
    
}