﻿using YAVCS.Models;
using YAVCS.Services.Contracts;

namespace YAVCS.Services;

public class CommitService : ICommitService
{
    
    private readonly INavigatorService _navigatorService;
    private readonly IHashService _hashService;
    private readonly IBranchService _branchService;
    private readonly ITreeService _treeService;
    private readonly IIndexService _indexService;

    public CommitService(INavigatorService navigatorService, IHashService hashService, IBranchService branchService,
        ITreeService treeService, IIndexService indexService)
    {
        _navigatorService = navigatorService;
        _hashService = hashService;
        _branchService = branchService;
        _treeService = treeService;
        _indexService = indexService;
    }

    public CommitFileModel CreateCommit(string treeHash, DateTime createdAt, string message,List<string> parentCommitHash)
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
        var headCommitHash = _branchService.GetHeadCommitHash();
        var headCommit = GetCommitByHash(headCommitHash);
        return _treeService.GetTreeRecordsByPath(headCommit!.TreeHash);
    }

    public bool IsIndexSameFromHead()
    {
        var headCommitHash = _branchService.GetHeadCommitHash();
        var headCommit = GetCommitByHash(headCommitHash);
        var currentRecords = _indexService.GetRecords().Values.ToHashSet();
        var headRecords = _treeService.GetTreeRecordsByPath(headCommit!.TreeHash).Values.ToHashSet();
        return currentRecords.SetEquals(headRecords);
    }

    public HashSet<string> GetAllCommitsHashes()
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        return Directory.GetFiles(vcsRootDirectoryNavigator!.CommitsDirectory)
            .Select(path=> Path.GetRelativePath(vcsRootDirectoryNavigator.CommitsDirectory,path))
            .ToHashSet();
    }

    public void DeleteCommit(string commitHash)
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        var commitFilePath = vcsRootDirectoryNavigator!.CommitsDirectory + Path.DirectorySeparatorChar + commitHash;
        File.Delete(commitFilePath);
    }
}