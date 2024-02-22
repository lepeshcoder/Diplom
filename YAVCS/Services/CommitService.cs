using YAVCS.Models;
using YAVCS.Services.Contracts;

namespace YAVCS.Services;

public class CommitService : ICommitService
{


    private readonly INavigatorService _navigatorService;
    private readonly IHashService _hashService;

    public CommitService(INavigatorService navigatorService, IHashService hashService)
    {
        _navigatorService = navigatorService;
        _hashService = hashService;
    }

    public void CreateCommit(string treeHash, DateTime createdAt, string message)
    {
        var newCommit = new CommitFileModel(treeHash, createdAt, message);
        var commitHash = _hashService.GetHash(treeHash + createdAt + message);
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        var commitFilePath = vcsRootDirectoryNavigator!.CommitsDirectory + Path.DirectorySeparatorChar + commitHash;
        File.WriteAllText(commitFilePath,newCommit.ToString());
    }
}