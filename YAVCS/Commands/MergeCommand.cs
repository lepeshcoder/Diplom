using System.Text;
using spkl.Diffs;
using Verano.Diff3Way;
using YAVCS.Commands.Contracts;
using YAVCS.Exceptions;
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
    private readonly IIndexService _indexService;

    public MergeCommand(INavigatorService navigatorService, IBranchService branchService,
        ITreeService treeService, ICommitService commitService, IMergeService mergeService,
        IBlobService blobService, IHashService hashService, IIndexService indexService)
    {
        _navigatorService = navigatorService;
        _branchService = branchService;
        _treeService = treeService;
        _commitService = commitService;
        _mergeService = mergeService;
        _blobService = blobService;
        _hashService = hashService;
        _indexService = indexService;
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
    public string Description => "Merge 2 branches into one";
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

                var activeBranch = _branchService.GetActiveBranch();
                
                if (_mergeService.IsOnMergeConflict())
                {
                    // проверям устранены ли конфликты в файлах
                    // создаём виртуальный коммит
                    // перемещам уазатель ветки на этот виртуальный коммит
                }
                
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
                    _mergeService.SetMergeConflictSign();
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
                           pair.Value.BlobHash != modifiedCommitIndexRecords[pair.Key].BlobHash)
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        
        
        return new PatchModel(newFiles, deletedFiles, modifiedFiles,modifiedBranchName);
    }

    private MergeResultModel ApplyPatches3Way(CommitFileModel baseCommit, PatchModel firstBranchPatch, PatchModel secondBranchPatch)
    {
        var baseCommitIndexRecords = _treeService.GetTreeRecordsByPath(baseCommit.TreeHash);

        foreach (var fileToAdd in firstBranchPatch.FilesToAdd.Values)
        {
            baseCommitIndexRecords.TryAdd(fileToAdd.RelativePath, fileToAdd);
        }
        foreach (var fileToAdd in secondBranchPatch.FilesToAdd.Values)
        {
            baseCommitIndexRecords.TryAdd(fileToAdd.RelativePath, fileToAdd);
        }
        foreach (var fileToDelete in firstBranchPatch.FilesToDelete.Values)
        {
            baseCommitIndexRecords.Remove(fileToDelete.RelativePath);
        }
        foreach (var fileToDelete in secondBranchPatch.FilesToDelete.Values)
        {
            baseCommitIndexRecords.Remove(fileToDelete.RelativePath);
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

        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        var conflictsPaths = new List<string>();
        foreach (var bothModifiedFileKey in bothModifiedFilesKeys)
        {
            var firstBranchFileBytes =
                _blobService.GetBlobData(firstBranchPatch.ModifiedFiles[bothModifiedFileKey].BlobHash);
            var secondBranchFileBytes =
                _blobService.GetBlobData(secondBranchPatch.ModifiedFiles[bothModifiedFileKey].BlobHash);
            var baseCommitFileBytes =
                _blobService.GetBlobData(baseCommitIndexRecords[bothModifiedFileKey].BlobHash);
            var firstBranchFileLines = Encoding.UTF8.GetString(firstBranchFileBytes)
                .Split([Environment.NewLine], StringSplitOptions.None);
            var secondBranchFileLines = Encoding.UTF8.GetString(secondBranchFileBytes)
                .Split([Environment.NewLine], StringSplitOptions.None);
            var baseCommitFileLines = Encoding.UTF8.GetString(baseCommitFileBytes)
                .Split([Environment.NewLine], StringSplitOptions.None);
            var merge = new Merge();
            var result =  merge.Merge3Way(firstBranchFileLines, baseCommitFileLines, secondBranchFileLines, 
                firstBranchPatch.BranchName, "base", secondBranchPatch.BranchName);
           
            if (result.IsConflict)
            {
                var relativePath = baseCommitIndexRecords[bothModifiedFileKey].RelativePath;
                var absolutePath = vcsRootDirectoryNavigator!.RepositoryRootDirectory + Path.DirectorySeparatorChar + relativePath;
                File.Delete(absolutePath);
                File.WriteAllLines(absolutePath,result.Result);
                conflictsPaths.Add(relativePath);
            }
            else
            {
                var mergedFileText = string.Join('\n', result.Result);
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