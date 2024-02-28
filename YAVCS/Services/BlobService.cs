using YAVCS.Services.Contracts;

namespace YAVCS.Services;

public class BlobService : IBlobService
{
    // services
    private readonly INavigatorService _navigatorService;
    private readonly IHashService _hashService;

    public BlobService(INavigatorService navigatorService, IHashService hashService)
    {
        _navigatorService = navigatorService;
        _hashService = hashService;
    }

    public void CreateBlob(byte[] data)
    {
        var blobHash = _hashService.GetHash(data);
        var blobPath = _navigatorService.TryGetRepositoryRootDirectory()!.BlobsDirectory + Path.DirectorySeparatorChar + blobHash;
        File.WriteAllBytes(blobPath,data);
    }

    public bool IsBlobExist(string hash)
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        if (vcsRootDirectoryNavigator == null) return false;
        return File.Exists(vcsRootDirectoryNavigator.BlobsDirectory + Path.DirectorySeparatorChar + hash);
    }

    public HashSet<string> GetAllBlobs()
    {
        var blobsHashes = new HashSet<string>();
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        foreach (var blobFile in Directory.GetFiles(vcsRootDirectoryNavigator!.BlobsDirectory))
        {
            var hash = Path.GetFileName(blobFile);
            blobsHashes.Add(hash);
        }
        return blobsHashes;
    }

    public void DeleteBlob(string blobHash)
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        var blobFilePath = vcsRootDirectoryNavigator!.BlobsDirectory + Path.DirectorySeparatorChar + blobHash;
        File.Delete(blobFilePath);
    }
}