using System.Text;
using spkl.Diffs;
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

    public MergeCommand(INavigatorService navigatorService, IBranchService branchService,
        ITreeService treeService, ICommitService commitService, IMergeService mergeService, IBlobService blobService)
    {
        _navigatorService = navigatorService;
        _branchService = branchService;
        _treeService = treeService;
        _commitService = commitService;
        _mergeService = mergeService;
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
                    _branchService.UpdateBranch(activeBranch.Name,branchToMergeHeadCommit!.Hash);
                    return;
                }
                
                // default (3-way merge) case
                // generate patches from merge base to 2 branches head commits
                var commonAncestorToActiveBranchPatch = GeneratePatch(commonAncestor, activeBranchHeadCommit);
                var commonAncestorToBranchToMergePatch = GeneratePatch(commonAncestor, branchToMergeHeadCommit);
                
                // apply patches using 3-way merge algorithm
                
                // update active branch
                
                
               
                
                
                
                
               
                
                
                
                
                // получаем записи индекса двух ветвей
                // проверка на наличчие виртуального коммита(если есть и нет merge конфликтов) то сливаем ветки
                // создаём виртуальный коммит: Заносим в его индекс все файлы без merge конфликтов
                // перезаписываем файлы с merge конфликтами в форме для решения конфликтов
                
                break;
            }
        }
    }

    private PatchModel GeneratePatch(CommitFileModel baseCommit, CommitFileModel modifiedCommit)
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
            .Where(pair => baseCommitIndexRecords.ContainsKey(pair.Key))
            .Select(pair => pair.Value)
            .ToList();

        var diffs = new List<MyersDiff<string>>();
        
        foreach (var modifiedFile in modifiedFiles)
        {
            var relativePath = modifiedFile.RelativePath;
            var baseCommitFileBytes = _blobService.GetBlobData(baseCommitIndexRecords[relativePath].BlobHash);
            var modifiedCommitFileBytes = _blobService.GetBlobData(modifiedCommitIndexRecords[relativePath].BlobHash); 
            var baseCommitFileText =  Encoding.UTF8.GetString(baseCommitFileBytes)
                .Split([Environment.NewLine], StringSplitOptions.None);
            var modifiedCommitFileText = Encoding.UTF8.GetString(modifiedCommitFileBytes)
                .Split([Environment.NewLine], StringSplitOptions.None);
            diffs.Add(new MyersDiff<string>(baseCommitFileText, modifiedCommitFileText));
        }

        return new PatchModel(newFiles, deletedFiles, modifiedFiles, diffs);

    }

    private CommitFileModel ApplyPatch(CommitFileModel baseCommit, CommitFileModel patchFromCommit, PatchModel patch)
    {
        /*var baseCommitIndexRecords = _treeService.GetTreeRecordsByPath(baseCommit.TreeHash);
        var patchFromCommitIndexRecords = _treeService.GetTreeRecordsByPath(patchFromCommit.TreeHash);
        // add newFiles to Index
        foreach (var fileToAdd in patch.FilesToAdd.Values)
        {
            baseCommitIndexRecords.Add(fileToAdd.RelativePath,fileToAdd); 
        }
        // remove deleted files from index
        foreach (var fileToDelete in patch.FilesToDelete.Values)
        {
            baseCommitIndexRecords.Remove(fileToDelete.RelativePath);
        }

        for (var i = 0; i < patch.ModifiedFiles.Count; i++)
        {
            var relativePath = patch.ModifiedFiles[i].RelativePath;
            var baseCommitFileBytes = _blobService.GetBlobData(baseCommitIndexRecords[relativePath].BlobHash);
            var patchFromCommitFileBytes = _blobService.GetBlobData(patchFromCommitIndexRecords[relativePath].BlobHash);
            var baseCommitFileText =  Encoding.UTF8.GetString(baseCommitFileBytes)
                .Split([Environment.NewLine], StringSplitOptions.None);
            var patchFromCommitFileText = Encoding.UTF8.GetString(patchFromCommitFileBytes)
                .Split([Environment.NewLine], StringSplitOptions.None);
            
        }
        
        var virtualCommitRootTree = */
        throw new NotImplementedException();

    }
}