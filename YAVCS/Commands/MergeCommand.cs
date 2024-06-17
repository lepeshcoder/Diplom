using System.Text;
using SynchrotronNet;
using Verano.Diff3Way;
using YAVCS.Commands.Contracts;
using YAVCS.Models;
using YAVCS.Services.Contracts;

namespace YAVCS.Commands;

public class MergeCommand : Command,ICommand
{
    private readonly INavigatorService _navigatorService;
    private readonly IBranchService _branchService;
    private readonly ITreeService _treeService;
    private readonly ICommitService _commitService;
    private readonly IMergeService _mergeService;
    private readonly IBlobService _blobService;
    private readonly IHashService _hashService;
  
    public MergeCommand(INavigatorService navigatorService, IBranchService branchService,
        ITreeService treeService, ICommitService commitService, IMergeService mergeService,
        IBlobService blobService, IHashService hashService)
    {
        _navigatorService = navigatorService;
        _branchService = branchService;
        _treeService = treeService;
        _commitService = commitService;
        _mergeService = mergeService;
        _blobService = blobService;
        _hashService = hashService;
    }

    private enum CommandCases
    {
        SyntaxError = 0,
        HelpCase = 1,
        DefaultCase = 2,
        AbortCase = 3
    }
    
    protected override Enum GetCommandCase(string[] args)
    {
        return args switch
        {
            ["--help"] => CommandCases.HelpCase,
            ["--abort"] => CommandCases.AbortCase,
            _ when args.Length == 1 => CommandCases.DefaultCase,
            _ => CommandCases.SyntaxError
        };
    }
    public string Description => "Merge 2 branches into one\n" +
                                 "Format:\n" +
                                 "1) Merge 2 branches: yavcs merge branchName\n" +
                                 "2) Reset to state before merge: yavcs merge --abort\n";
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
            case CommandCases.AbortCase:
            {
                if (!_mergeService.IsOnMergeConflict())
                {
                    throw new Exception("Cannot abort merge, there are not any merge Conflicts");
                }

                var headCommitHash = _branchService.GetHeadCommitHash();
                var headCommit = _commitService.GetCommitByHash(headCommitHash);
                _treeService.ResetIndexToState(headCommit!.TreeHash);
                _treeService.ResetWorkingDirectoryToState(headCommit.TreeHash);
                _mergeService.ResetMergeConflictSign();
                break;
            }
            case CommandCases.DefaultCase:
            {
                var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
                if (vcsRootDirectoryNavigator == null)
                {
                    throw new Exception("not a part of repository");
                }

                if (_branchService.IsDetachedHead())
                {
                    throw new ArgumentException("Cannot merge in detached head state");
                }
                
                var branchToMergeName = args[0];
                var branchToMerge = _branchService.GetBranchByName(branchToMergeName);
                if (branchToMerge == null)
                {
                    throw new ArgumentException($"Branch {branchToMergeName} Doesn't exist");
                }

                if (!_commitService.IsIndexSameFromHead())
                {
                    throw new Exception("cannot merge with local changes");
                }

                var activeBranch = _branchService.GetActiveBranch();
                
                // Get head commits of 2 branches
                var activeBranchHeadCommit = _commitService.GetCommitByHash(activeBranch.CommitHash);
                var branchToMergeHeadCommit = _commitService.GetCommitByHash(branchToMerge.CommitHash);
                //get common ancestor
                var commonAncestor = _mergeService.GetCommonAncestor(activeBranch.CommitHash, branchToMerge.CommitHash);

                // already up to date (useless merge) case
                if (commonAncestor!.Hash == branchToMergeHeadCommit!.Hash)
                {
                    Console.WriteLine("Already up to date");
                    return;
                }
                
                // fast-forward case
                if (commonAncestor.Hash == activeBranchHeadCommit!.Hash)
                {
                    Console.WriteLine("Fast forward merge");
                    _branchService.UpdateBranch(activeBranch.Name,branchToMergeHeadCommit.Hash);
                    _treeService.ResetIndexToState(branchToMergeHeadCommit.TreeHash);
                    _treeService.ResetWorkingDirectoryToState(branchToMergeHeadCommit.TreeHash);
                    return;
                }
                
                // default (3-way merge) case
                // generate patches from merge base to 2 branches head commits
                var commonAncestorToActiveBranchPatch = GeneratePatch(commonAncestor, activeBranchHeadCommit,activeBranch.Name);
                var commonAncestorToBranchToMergePatch = GeneratePatch(commonAncestor, branchToMergeHeadCommit,branchToMerge.Name);
                
                // apply patches using 3-way merge algorithm
                var mergeResult = ApplyPatches3Way(
                    commonAncestor,
                    commonAncestorToActiveBranchPatch,
                    commonAncestorToBranchToMergePatch);    

                // if merge has no conflicts
                if (mergeResult.ConflictPaths.Count == 0)
                {
                    var mergeCommitRootTreeHash = _treeService.CreateTreeByRecords(mergeResult.IndexRecords);
                    var mergeCommit = _commitService.CreateCommit(
                        mergeCommitRootTreeHash,
                        DateTime.Now,
                        $"merge {branchToMerge.Name} into {activeBranch.Name}",
                        [activeBranchHeadCommit.Hash, branchToMergeHeadCommit.Hash]);
                    
                    _branchService.UpdateBranch(activeBranch.Name,mergeCommit.Hash);
                    _treeService.ResetIndexToState(mergeCommitRootTreeHash);
                    _treeService.ResetWorkingDirectoryToState(mergeCommitRootTreeHash);
                }
                else
                {
                    Console.WriteLine("Merge Failed, there are some merge Conflicts in:\n");
                    foreach (var conflictPath in mergeResult.ConflictPaths)
                    {
                        Console.WriteLine(conflictPath);
                    }
                    _mergeService.SetMergeConflictSign(activeBranchHeadCommit.Hash,branchToMergeHeadCommit.Hash);
                }
                break;
            }
        }
    }

    private PatchModel GeneratePatch(CommitFileModel baseCommit, CommitFileModel modifiedCommit,string modifiedBranchName)
    {
        var baseCommitIndexRecords = _treeService.GetTreeRecordsByPath(baseCommit.TreeHash);
        var modifiedCommitIndexRecords = _treeService.GetTreeRecordsByPath(modifiedCommit.TreeHash);
       
        var newFiles  = modifiedCommitIndexRecords
            .Where(pair => !baseCommitIndexRecords.ContainsKey(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        
        var deletedFiles = baseCommitIndexRecords
            .Where(pair => !modifiedCommitIndexRecords.ContainsKey(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        
        var modifiedFiles = modifiedCommitIndexRecords
            .Where(pair => baseCommitIndexRecords.ContainsKey(pair.Key) &&
                           pair.Value.BlobHash != baseCommitIndexRecords[pair.Key].BlobHash)
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        
        
        return new PatchModel(newFiles, deletedFiles, modifiedFiles,modifiedBranchName);
    }

    private MergeResultModel ApplyPatches3Way(CommitFileModel baseCommit, PatchModel firstBranchPatch, PatchModel secondBranchPatch)
    {
        var baseCommitIndexRecords = _treeService.GetTreeRecordsByPath(baseCommit.TreeHash);

        var onlyAddedInFirstBranchKeys = firstBranchPatch.FilesToAdd.Keys.Except(secondBranchPatch.FilesToAdd.Keys);
        var onlyAddedInSecondBranchKeys = secondBranchPatch.FilesToAdd.Keys.Except(firstBranchPatch.FilesToAdd.Keys);
        var bothAddedKeys = firstBranchPatch.FilesToAdd.Keys.Intersect(secondBranchPatch.FilesToAdd.Keys);

        foreach (var onlyAddedInFirstBranchKey in onlyAddedInFirstBranchKeys)
        {
            var record = firstBranchPatch.FilesToAdd[onlyAddedInFirstBranchKey];
            baseCommitIndexRecords.TryAdd(record.RelativePath, record);
        }
        foreach (var onlyAddedInSecondBranchKey in onlyAddedInSecondBranchKeys)
        {
            var record = secondBranchPatch.FilesToAdd[onlyAddedInSecondBranchKey];
            baseCommitIndexRecords.TryAdd(record.RelativePath, record);
        }
        
        // file that modify only in first branch
        var onlyModifiedInFirstBranchKeys = firstBranchPatch.ModifiedFiles.Keys.Except(secondBranchPatch.ModifiedFiles.Keys);
        var onlyModifiedInSecondBranchKeys = secondBranchPatch.ModifiedFiles.Keys.Except(firstBranchPatch.ModifiedFiles.Keys);
        var bothModifiedFilesKeys = firstBranchPatch.ModifiedFiles.Keys.Intersect(secondBranchPatch.ModifiedFiles.Keys);

        foreach (var modifiedFileKey in onlyModifiedInFirstBranchKeys)
        {
            var indexRecord = firstBranchPatch.ModifiedFiles[modifiedFileKey];
            baseCommitIndexRecords.Remove(modifiedFileKey);
            baseCommitIndexRecords.Add(modifiedFileKey,indexRecord);
        }
        foreach (var modifiedFileKey in onlyModifiedInSecondBranchKeys)
        {
            var indexRecord = secondBranchPatch.ModifiedFiles[modifiedFileKey];
            baseCommitIndexRecords.Remove(modifiedFileKey);
            baseCommitIndexRecords.Add(modifiedFileKey,indexRecord);
        }
         
        foreach (var fileToDelete in firstBranchPatch.FilesToDelete.Values)
        {
            baseCommitIndexRecords.Remove(fileToDelete.RelativePath);
        }
        foreach (var fileToDelete in secondBranchPatch.FilesToDelete.Values)
        {
            baseCommitIndexRecords.Remove(fileToDelete.RelativePath);
        }
        
     

        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        var conflictsPaths = new List<string>();

        foreach (var bothAddedKey in bothAddedKeys)
        {
             var firstBranchFileBytes =
                _blobService.GetBlobData(firstBranchPatch.FilesToAdd[bothAddedKey].BlobHash);
            var secondBranchFileBytes =
                _blobService.GetBlobData(secondBranchPatch.FilesToAdd[bothAddedKey].BlobHash);
            var firstBranchFileLines = Encoding.UTF8.GetString(firstBranchFileBytes)
                .Split([Environment.NewLine,"\n"], StringSplitOptions.None);
            var secondBranchFileLines = Encoding.UTF8.GetString(secondBranchFileBytes)
                .Split([Environment.NewLine,"\n"], StringSplitOptions.None);
            string[] baseCommitFileLines = [];

            var merge = Diff.diff3_merge(firstBranchFileLines, baseCommitFileLines, secondBranchFileLines, true);
            var isConflict = merge.Exists(block => block is Diff.MergeConflictResultBlock resultBlock &&
                                                   resultBlock.LeftLines.Length != 0 &&
                                                   resultBlock.RightLines.Length != 0);
            var mergedFile = _mergeService.CreateMergeResult(merge,firstBranchPatch.BranchName,secondBranchPatch.BranchName); 
            
            if (isConflict)
            {
                var relativePath = firstBranchPatch.FilesToAdd[bothAddedKey].RelativePath;
                var absolutePath = vcsRootDirectoryNavigator!.RepositoryRootDirectory + Path.DirectorySeparatorChar + relativePath;
                File.Delete(absolutePath);
                File.WriteAllLines(absolutePath,mergedFile);
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
                baseCommitIndexRecords.Add(bothAddedKey,new IndexRecord(bothAddedKey,mergedFileHash));
            }
        }
        
        foreach (var bothModifiedFileKey in bothModifiedFilesKeys)
        {
            var firstBranchFileBytes =
                _blobService.GetBlobData(firstBranchPatch.ModifiedFiles[bothModifiedFileKey].BlobHash);
            var secondBranchFileBytes =
                _blobService.GetBlobData(secondBranchPatch.ModifiedFiles[bothModifiedFileKey].BlobHash);
            var baseCommitFileBytes =
                _blobService.GetBlobData(baseCommitIndexRecords[bothModifiedFileKey].BlobHash);
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
            var mergedFile = _mergeService.CreateMergeResult(merge,firstBranchPatch.BranchName,secondBranchPatch.BranchName); 

           
            if (isConflict)
            {
                var relativePath = baseCommitIndexRecords[bothModifiedFileKey].RelativePath;
                var absolutePath = vcsRootDirectoryNavigator!.RepositoryRootDirectory + Path.DirectorySeparatorChar + relativePath;
                File.Delete(absolutePath);
                File.WriteAllLines(absolutePath,mergedFile);
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
                baseCommitIndexRecords.Remove(bothModifiedFileKey);
                baseCommitIndexRecords.Add(bothModifiedFileKey,new IndexRecord(bothModifiedFileKey,mergedFileHash));
            }
        }
        
        return new MergeResultModel(baseCommitIndexRecords,conflictsPaths);
    }

   
}