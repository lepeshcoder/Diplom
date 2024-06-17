using System.Text;
using spkl.Diffs;
using YAVCS.Models;
using YAVCS.Services.Contracts;

namespace YAVCS.Services;

public class DiffService : IDiffService
{
    private readonly IBlobService _blobService;

    public DiffService(IBlobService blobService)
    {
        _blobService = blobService;
    }

    public DiffResultModel GetDiff(string[] previousVersion, string[] currentVersion)
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

    public DiffResultModel GetDiff(Dictionary<string, IndexRecord> baseCommitRecords,
        Dictionary<string, IndexRecord> commitToCompareIndexRecords)
    {
        // Элементы, которые есть только в первой коллекции
        var newFiles = baseCommitRecords
            .Where(pair => !commitToCompareIndexRecords.ContainsKey(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        // Элементы, которые есть только во второй коллекции
        var deletedFiles = commitToCompareIndexRecords
            .Where(pair => !baseCommitRecords.ContainsKey(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value); 

        // Элементы, которые есть в обеих коллекциях
        var modifiedFiles = baseCommitRecords
            .Where(
                pair => commitToCompareIndexRecords.ContainsKey(pair.Key) &&
                        pair.Value.BlobHash != commitToCompareIndexRecords[pair.Key].BlobHash)
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        var result = new DiffResultModel([],[]);

        foreach (var newFile in newFiles.Values)
        {
            result.Lines.Add($"New file: {newFile.RelativePath}");
            result.LineColors.Add(ConsoleColor.Green);
        }

        result.Lines.Add("");
        result.LineColors.Add(ConsoleColor.White);
        
        foreach (var deletedFile in deletedFiles.Values)
        {
            result.Lines.Add($"Deleted file: {deletedFile.RelativePath}");
            result.LineColors.Add(ConsoleColor.Red);
        }

        result.Lines.Add("");
        result.LineColors.Add(ConsoleColor.White);
        
        foreach (var modifiedFile in modifiedFiles.Values)
        {
            var relativePath = modifiedFile.RelativePath;
            var currentCommitFileBytes = _blobService.GetBlobData(baseCommitRecords[relativePath].BlobHash);
            var commitToCompareFileBytes = _blobService.GetBlobData(commitToCompareIndexRecords[relativePath].BlobHash);
            var currentCommitFileText = Encoding.UTF8.GetString(currentCommitFileBytes)
                .Split([Environment.NewLine, "\n"], StringSplitOptions.None);
            var commitToCompareFileText = Encoding.UTF8.GetString(commitToCompareFileBytes)
                .Split([Environment.NewLine, "\n"], StringSplitOptions.None);
            var metaData = $"\n{relativePath}:";
            result.Lines.Add(metaData);
            result.LineColors.Add(ConsoleColor.Yellow);
            var diffResult = GetDiff(commitToCompareFileText, currentCommitFileText);
            result.Lines.AddRange(diffResult.Lines);
            result.LineColors.AddRange(diffResult.LineColors);
        }

        return result;
    }
}