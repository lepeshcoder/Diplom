using YAVCS.Models;
using YAVCS.Services.Contracts;

namespace YAVCS.Services;

public class CommitService : ICommitService
{
    
    private readonly INavigatorService _navigatorService;
    private readonly IHashService _hashService;
    private readonly IBranchService _branchService;
    private readonly ITreeService _treeService;

    public CommitService(INavigatorService navigatorService, IHashService hashService, IBranchService branchService, ITreeService treeService)
    {
        _navigatorService = navigatorService;
        _hashService = hashService;
        _branchService = branchService;
        _treeService = treeService;
    }

    public CommitFileModel CreateCommit(string treeHash, DateTime createdAt, string message,string parentCommitHash)
    {
        var commitHash = _hashService.GetHash(treeHash + createdAt + message);
        var newCommit = new CommitFileModel(treeHash, createdAt, message,commitHash,parentCommitHash);
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        var commitFilePath = vcsRootDirectoryNavigator!.CommitsDirectory + Path.DirectorySeparatorChar + commitHash;
        File.WriteAllText(commitFilePath,newCommit.ToString());
        return newCommit;
    }

    public CommitFileModel? GetCommitByHash(string commitHash)
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        var commitFilePath = vcsRootDirectoryNavigator!.CommitsDirectory + Path.DirectorySeparatorChar + commitHash;
        return !File.Exists(commitFilePath) ? null : new CommitFileModel(commitFilePath);
    }

    public Dictionary<string,IndexRecord> GetHeadRecordsByPath()
    {
        var activeBranch = _branchService.GetActiveBranch();
        if (activeBranch == null) return new Dictionary<string, IndexRecord>();
        var headCommit = GetCommitByHash(activeBranch.CommitHash);
        return _treeService.GetTreeRecordsByPath(headCommit!.TreeHash);
    }
    
    
    
    
}