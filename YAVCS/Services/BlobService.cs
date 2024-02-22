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
        var blobPath = _navigatorService.TryGetRepositoryRootDirectory()!.BlobsDirectory + '/' + blobHash;
        File.WriteAllBytes(blobPath,data);
    }

    public bool IsBlobExist(string hash)
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        if (vcsRootDirectoryNavigator == null) return false;
        return File.Exists(vcsRootDirectoryNavigator.BlobsDirectory + '/' + hash);
    }
}