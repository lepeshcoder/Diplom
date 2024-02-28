using YAVCS.Models;
using YAVCS.Services.Contracts;

namespace YAVCS.Services;

public class GarbageCollectorService : IGarbageCollectorService
{
    private readonly INavigatorService _navigatorService;
    private readonly ITreeService _treeService;
    private readonly IBlobService _blobService;

    public GarbageCollectorService(INavigatorService navigatorService, ITreeService treeService, IBlobService blobService)
    {
        _navigatorService = navigatorService;
        _treeService = treeService;
        _blobService = blobService;
    }

    public void CollectGarbage()
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        var commitDirectory = vcsRootDirectoryNavigator!.CommitsDirectory;
        // get all blobs hashes from blobs directory 
        var blobsHashes = _blobService.GetAllBlobs();
        // go through each commit and delete blob hashes there are in use
        foreach (var commitFile in Directory.GetFiles(commitDirectory))
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