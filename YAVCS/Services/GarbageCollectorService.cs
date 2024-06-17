using YAVCS.Models;
using YAVCS.Services.Contracts;

namespace YAVCS.Services;

public class GarbageCollectorService : IGarbageCollectorService
{
    private readonly INavigatorService _navigatorService;
    private readonly ITreeService _treeService;
    private readonly IBlobService _blobService;
    private readonly IBranchService _branchService;
    private readonly ICommitService _commitService;

    public GarbageCollectorService(INavigatorService navigatorService, ITreeService treeService,
        IBlobService blobService, IBranchService branchService, ICommitService commitService)
    {
        _navigatorService = navigatorService;
        _treeService = treeService;
        _blobService = blobService;
        _branchService = branchService;
        _commitService = commitService;
    }

    public void CollectGarbage()
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        var commitDirectory = vcsRootDirectoryNavigator!.CommitsDirectory;

        var allBranches = _branchService.GetAllBranches();
        var allCommitsHashes = _commitService.GetAllCommitsHashes();
        // get all unreachable commits
        foreach (var branch in allBranches)
        {
            var branchHeadCommit = _commitService.GetCommitByHash(branch.CommitHash);
            var ancestors = new Queue<string>([branchHeadCommit!.Hash]);
            var commitHashesList = new List<string>();
            while (ancestors.Count != 0)
            {
                var ancestorCommit = _commitService.GetCommitByHash(ancestors.Dequeue());
                commitHashesList.Add(ancestorCommit!.Hash);
                foreach (var ancestorCommitParentHash in ancestorCommit!.ParentCommitHashes)
                {
                    ancestors.Enqueue(ancestorCommitParentHash);
                }
            }

            foreach (var commitHash in commitHashesList)
            {
                allCommitsHashes.Remove(commitHash);
            }
        }

        // remove unreachable commits and it's root trees
        foreach (var commitHash in allCommitsHashes)
        {
            var commitRootTreeHash = _commitService.GetCommitByHash(commitHash)!.TreeHash;
            _treeService.DeleteTree(commitRootTreeHash);
            _commitService.DeleteCommit(commitHash);
        }
        
        //TODO: fix deletion of stash blobs
        // get all blobs hashes from blobs directory 
        var blobsHashes = _blobService.GetAllBlobs();
        var commits = Directory.GetFiles(commitDirectory)
            .Concat(Directory.GetFiles(vcsRootDirectoryNavigator.StashCommitsDirectory));
        // go through each commit and delete blob hashes there are in use
        foreach (var commitFile in commits)
        {
            var commit = new CommitFileModel(commitFile);
            var rootTreeHash = commit.TreeHash;
            var rootTree = _treeService.GetTreeByHash(rootTreeHash);
            CollectTreeGarbage(rootTree,blobsHashes);
        }
        // after deleting only blobs with no references remained
        foreach (var blobHash in blobsHashes)
        {
            _blobService.DeleteBlob(blobHash);
        }
        
    }

    private void CollectTreeGarbage(TreeFileModel tree,HashSet<string> blobsHashes)
    {
        foreach (var child in tree.Childs.Values)
        {
            if (child.Type == (int)ChildItemModel.Types.Blob)
            {
                blobsHashes.Remove(child.Hash);
            }
            else
            {
                var childTree = _treeService.GetTreeByHash(child.Hash);
                CollectTreeGarbage(childTree,blobsHashes);
            }
        }
    }
}