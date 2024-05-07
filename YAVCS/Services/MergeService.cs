using System.Text;
using SynchrotronNet;
using YAVCS.Models;
using YAVCS.Services.Contracts;

namespace YAVCS.Services;

public class MergeService : IMergeService
{
    private readonly INavigatorService _navigatorService;
    private readonly IBlobService _blobService;
    private readonly IHashService _hashService;
    private Dictionary<string,CommitFileModel> _commitsByHash = [];

    public MergeService(INavigatorService navigatorService, IBlobService blobService, IHashService hashService)
    {
        _navigatorService = navigatorService;
        _blobService = blobService;
        _hashService = hashService;
    }

    public bool IsOnMergeConflict()
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        return File.Exists(vcsRootDirectoryNavigator!.MergeConflictFile);
    }

    public void SetMergeConflictSign(string branchIntoMergeCommitHash, string branchToMergeCommitHash)
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        File.WriteAllLines(vcsRootDirectoryNavigator!.MergeConflictFile,[branchIntoMergeCommitHash,branchToMergeCommitHash]);
    }

    public void ResetMergeConflictSign()
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        File.Delete(vcsRootDirectoryNavigator!.MergeConflictFile);
    }

    public CommitFileModel? GetCommonAncestor(string firstCommitHash, string secondCommitHash)
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        foreach (var commitFilePath in Directory.GetFiles(vcsRootDirectoryNavigator!.CommitsDirectory))
        {
            var commit = new CommitFileModel(commitFilePath);
            _commitsByHash.Add(commit.Hash, commit);
        }
        
        Queue<string> ancestors1 = [];
        Queue<string> ancestors2 = [];
        ancestors1.Enqueue(firstCommitHash);
        ancestors2.Enqueue(secondCommitHash);
        HashSet<string> visited1 = [];
        HashSet<string> visited2 = [];

        while (ancestors1.Count > 0 || ancestors2.Count > 0)
        {
            if (ancestors1.Count > 0)
            {
                var current1 = ancestors1.Dequeue();
                if (visited2.Contains(current1))
                {
                    return _commitsByHash[current1];
                }
                visited1.Add(current1);
                var commit1 = _commitsByHash[current1];
                foreach (var parent in commit1.ParentCommitHashes)
                {
                    if (!visited1.Contains(parent))
                    {
                        ancestors1.Enqueue(parent);
                    }
                }
            }

            if (ancestors2.Count > 0)
            {
                var current2 = ancestors2.Dequeue();
                if (visited1.Contains(current2))
                {
                    return _commitsByHash[current2];
                }
                visited2.Add(current2);
                var commit2 = _commitsByHash[current2];
                foreach (var parent in commit2.ParentCommitHashes)
                {
                    if (!visited2.Contains(parent))
                    {
                        ancestors2.Enqueue(parent);
                    }
                }
            }
        }

        return null;
    }

    public string[] GetMergeBranches()
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        return File.ReadAllLines(vcsRootDirectoryNavigator!.MergeConflictFile);
    }

    public MergeResultModel Merge(Dictionary<string,IndexRecord> commonAncestorIndexRecords,Dictionary<string, IndexRecord> firstCommitIndexRecords,
        Dictionary<string, IndexRecord> secondCommitIndexRecords,string firstCommitString,string secondCommitString)
    {
        var commonAncestorToActiveBranchPatch = GeneratePatch(commonAncestorIndexRecords, firstCommitIndexRecords,firstCommitString);
        var commonAncestorToBranchToMergePatch = GeneratePatch(commonAncestorIndexRecords, secondCommitIndexRecords,secondCommitString);
                
        // apply patches using 3-way merge algorithm
        var mergeResult = ApplyPatches3Way(
            commonAncestorIndexRecords,
            commonAncestorToActiveBranchPatch,
            commonAncestorToBranchToMergePatch);

        return mergeResult;
    }

    private MergeResultModel ApplyPatches3Way(Dictionary<string,IndexRecord> commonAncestor,
        PatchModel commonAncestorToActiveBranchPatch, PatchModel commonAncestorToBranchToMergePatch)
    {
        var onlyAddedInFirstBranchKeys = commonAncestorToActiveBranchPatch.FilesToAdd.Keys.Except(commonAncestorToBranchToMergePatch.FilesToAdd.Keys);
        var onlyAddedInSecondBranchKeys = commonAncestorToBranchToMergePatch.FilesToAdd.Keys.Except(commonAncestorToActiveBranchPatch.FilesToAdd.Keys);
        var bothAddedKeys = commonAncestorToActiveBranchPatch.FilesToAdd.Keys.Intersect(commonAncestorToBranchToMergePatch.FilesToAdd.Keys);

        foreach (var onlyAddedInFirstBranchKey in onlyAddedInFirstBranchKeys)
        {
            var record = commonAncestorToActiveBranchPatch.FilesToAdd[onlyAddedInFirstBranchKey];
            commonAncestor.TryAdd(record.RelativePath, record);
        }
        foreach (var onlyAddedInSecondBranchKey in onlyAddedInSecondBranchKeys)
        {
            var record = commonAncestorToBranchToMergePatch.FilesToAdd[onlyAddedInSecondBranchKey];
            commonAncestor.TryAdd(record.RelativePath, record);
        }
        
        // file that modify only in first branch
        var onlyModifiedInFirstBranchKeys = commonAncestorToActiveBranchPatch.ModifiedFiles.Keys.Except(commonAncestorToBranchToMergePatch.ModifiedFiles.Keys);
        var onlyModifiedInSecondBranchKeys = commonAncestorToBranchToMergePatch.ModifiedFiles.Keys.Except(commonAncestorToActiveBranchPatch.ModifiedFiles.Keys);
        var bothModifiedFilesKeys = commonAncestorToActiveBranchPatch.ModifiedFiles.Keys.Intersect(commonAncestorToBranchToMergePatch.ModifiedFiles.Keys);

        foreach (var modifiedFileKey in onlyModifiedInFirstBranchKeys)
        {
            var indexRecord = commonAncestorToActiveBranchPatch.ModifiedFiles[modifiedFileKey];
            commonAncestor.Remove(modifiedFileKey);
            commonAncestor.Add(modifiedFileKey,indexRecord);
        }
        foreach (var modifiedFileKey in onlyModifiedInSecondBranchKeys)
        {
            var indexRecord = commonAncestorToBranchToMergePatch.ModifiedFiles[modifiedFileKey];
            commonAncestor.Remove(modifiedFileKey);
            commonAncestor.Add(modifiedFileKey,indexRecord);
        }
         
        foreach (var fileToDelete in commonAncestorToActiveBranchPatch.FilesToDelete.Values)
        {
            commonAncestor.Remove(fileToDelete.RelativePath);
        }
        foreach (var fileToDelete in commonAncestorToBranchToMergePatch.FilesToDelete.Values)
        {
            commonAncestor.Remove(fileToDelete.RelativePath);
        }
        
     

        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        var conflictsPaths = new List<string>();

        foreach (var bothAddedKey in bothAddedKeys)
        {
             var firstBranchFileBytes =
                _blobService.GetBlobData(commonAncestorToActiveBranchPatch.FilesToAdd[bothAddedKey].BlobHash);
            var secondBranchFileBytes =
                _blobService.GetBlobData(commonAncestorToBranchToMergePatch.FilesToAdd[bothAddedKey].BlobHash);
            var firstBranchFileLines = Encoding.UTF8.GetString(firstBranchFileBytes)
                .Split([Environment.NewLine,"\n"], StringSplitOptions.None);
            var secondBranchFileLines = Encoding.UTF8.GetString(secondBranchFileBytes)
                .Split([Environment.NewLine,"\n"], StringSplitOptions.None);
            string[] baseCommitFileLines = [];

            var merge = Diff.diff3_merge(firstBranchFileLines, baseCommitFileLines, secondBranchFileLines, true);
            var isConflict = merge.Exists(block => block is Diff.MergeConflictResultBlock resultBlock &&
                                                   resultBlock.LeftLines.Length != 0 &&
                                                   resultBlock.RightLines.Length != 0);
            var mergedFile = CreateMergeResult(merge,commonAncestorToActiveBranchPatch.BranchName,commonAncestorToBranchToMergePatch.BranchName); 
            
            if (isConflict)
            {
                var relativePath = commonAncestorToActiveBranchPatch.FilesToAdd[bothAddedKey].RelativePath;
                var absolutePath = vcsRootDirectoryNavigator!.RepositoryRootDirectory + Path.DirectorySeparatorChar + relativePath;
                var mergedFileText = string.Join('\n', mergedFile);
                var mergedFileHash = _hashService.GetHash(mergedFileText);
                if (!_blobService.IsBlobExist(mergedFileHash))
                {
                    _blobService.CreateBlob(Encoding.UTF8.GetBytes(mergedFileText));
                }
                commonAncestor.Add(bothAddedKey,new IndexRecord(bothAddedKey,mergedFileHash));
                conflictsPaths.Add(relativePath);
            }
            else
            {
                var mergedFileText = string.Join('\n', mergedFile);
                var mergedFileHash = _hashService.GetHash(mergedFileText);
                if (!_blobService.IsBlobExist(mergedFileHash))
                {
                    _blobService.CreateBlob(Encoding.UTF8.GetBytes(mergedFileText));
                }
                commonAncestor.Add(bothAddedKey,new IndexRecord(bothAddedKey,mergedFileHash));
            }
        }
        
        foreach (var bothModifiedFileKey in bothModifiedFilesKeys)
        {
            var firstBranchFileBytes =
                _blobService.GetBlobData(commonAncestorToActiveBranchPatch.ModifiedFiles[bothModifiedFileKey].BlobHash);
            var secondBranchFileBytes =
                _blobService.GetBlobData(commonAncestorToBranchToMergePatch.ModifiedFiles[bothModifiedFileKey].BlobHash);
            var baseCommitFileBytes =
                _blobService.GetBlobData(commonAncestor[bothModifiedFileKey].BlobHash);
            var firstBranchFileLines = Encoding.UTF8.GetString(firstBranchFileBytes)
                .Split([Environment.NewLine,"\n"], StringSplitOptions.None);
            var secondBranchFileLines = Encoding.UTF8.GetString(secondBranchFileBytes)
                .Split([Environment.NewLine,"\n"], StringSplitOptions.None);
            var baseCommitFileLines = Encoding.UTF8.GetString(baseCommitFileBytes)
                .Split([Environment.NewLine,"\n"], StringSplitOptions.None);
            
            var merge = Diff.diff3_merge(firstBranchFileLines, baseCommitFileLines, secondBranchFileLines, true);
            var isConflict = merge.Exists(block => block is Diff.MergeConflictResultBlock resultBlock &&
                                                   (resultBlock.LeftLines.Length != 0 &&
                                                    resultBlock.RightLines.Length != 0));
            var mergedFile = CreateMergeResult(merge,commonAncestorToActiveBranchPatch.BranchName,commonAncestorToBranchToMergePatch.BranchName); 

           
            if (isConflict)
            {
                var relativePath = commonAncestor[bothModifiedFileKey].RelativePath;
                var absolutePath = vcsRootDirectoryNavigator!.RepositoryRootDirectory + Path.DirectorySeparatorChar + relativePath;
                var mergedFileText = string.Join('\n', mergedFile);
                var mergedFileHash = _hashService.GetHash(mergedFileText);
                if (!_blobService.IsBlobExist(mergedFileHash))
                {
                    _blobService.CreateBlob(Encoding.UTF8.GetBytes(mergedFileText));
                }
                commonAncestor.Add(bothModifiedFileKey,new IndexRecord(bothModifiedFileKey,mergedFileHash));
                conflictsPaths.Add(relativePath);
            }
            else
            {
                var mergedFileText = string.Join('\n', mergedFile);
                var mergedFileHash = _hashService.GetHash(mergedFileText);
                if (!_blobService.IsBlobExist(mergedFileHash))
                {
                    _blobService.CreateBlob(Encoding.UTF8.GetBytes(mergedFileText));
                }
                commonAncestor.Remove(bothModifiedFileKey);
                commonAncestor.Add(bothModifiedFileKey,new IndexRecord(bothModifiedFileKey,mergedFileHash));
            }
        }
        
        return new MergeResultModel(commonAncestor,conflictsPaths);
    }

    public string[] CreateMergeResult(List<Diff.IMergeResultBlock> blocks,string firstBranchName,string secondBranchName)
    {
        var result = new List<string>();
        foreach (var block in blocks)
        {
    
            if (block is Diff.MergeConflictResultBlock resultBlock)
            {
                if (resultBlock.LeftLines.Length == 0)
                {
                    result.AddRange(resultBlock.RightLines);
                }
                else if (resultBlock.RightLines.Length == 0)
                {
                    result.AddRange(resultBlock.LeftLines);
                }
                else
                {
                    result.Add("<<<<<<" + firstBranchName);
                    result.AddRange(resultBlock.LeftLines);
                    result.Add("======" + "");
                    result.AddRange(resultBlock.RightLines);
                    result.Add(">>>>>>" + secondBranchName);
                }
            }
            else
            {
                result.AddRange((block as Diff.MergeOKResultBlock)!.ContentLines);
            }
        }
        return result.ToArray();
    }

    private PatchModel GeneratePatch(Dictionary<string, IndexRecord> commonAncestorIndexRecords, Dictionary<string, IndexRecord> commitIndexRecords, string commitName)
    {
        var newFiles  = commitIndexRecords
            .Where(pair => !commonAncestorIndexRecords.ContainsKey(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        
        var deletedFiles = commonAncestorIndexRecords
            .Where(pair => !commitIndexRecords.ContainsKey(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        
        var modifiedFiles = commitIndexRecords
            .Where(pair => commonAncestorIndexRecords.ContainsKey(pair.Key) &&
                           pair.Value.BlobHash != commonAncestorIndexRecords[pair.Key].BlobHash)
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        
        
        return new PatchModel(newFiles, deletedFiles, modifiedFiles, commitName);
    }
}