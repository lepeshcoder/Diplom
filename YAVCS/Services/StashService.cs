using YAVCS.Models;
using YAVCS.Services.Contracts;

namespace YAVCS.Services;

public class StashService : IStashService
{
    private readonly INavigatorService _navigatorService;

    public StashService(INavigatorService navigatorService)
    {
        _navigatorService = navigatorService;
    }

    public void Push(StashCommitFileModel commit)
    {
        var previousStashHeadCommitHash = GetStashHeadCommitHash();
        if (string.IsNullOrEmpty(previousStashHeadCommitHash))
        {
            commit.ParentCommitHashes = [];
        }
        else
        {
            commit.ParentCommitHashes = [previousStashHeadCommitHash];
        }
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        var stashCommitFilePath = vcsRootDirectoryNavigator!.StashCommitsDirectory + Path.DirectorySeparatorChar + commit.Hash;
        File.WriteAllText(stashCommitFilePath,commit.ToString());
        SetStashHead(commit.Hash);
    }

    public StashCommitFileModel? Pop()
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        var oldStashHeadCommitHash = GetStashHeadCommitHash();
        if (string.IsNullOrEmpty(oldStashHeadCommitHash))
        {
            return null;
        }
        var oldStashHeadCommit = GetStashCommit(oldStashHeadCommitHash);
        if (oldStashHeadCommit!.ParentCommitHashes.Count == 0)
        {
            SetStashHead("");
        }
        else
        {
            var newStashHeadCommitHash = oldStashHeadCommit.ParentCommitHashes[0];
            SetStashHead(newStashHeadCommitHash);
        }
        var oldStashHeadCommitFilePath = vcsRootDirectoryNavigator!.StashCommitsDirectory + Path.DirectorySeparatorChar + oldStashHeadCommit.Hash;
        File.Delete(oldStashHeadCommitFilePath);
        return oldStashHeadCommit;
    }

    public IEnumerable<StashCommitFileModel> GetStashCommits()
    {
        List<StashCommitFileModel> stashCommits = [];
        var stashHeadCommitHash = GetStashHeadCommitHash();
        if (string.IsNullOrEmpty(stashHeadCommitHash))
        {
            return new List<StashCommitFileModel>();
        }
        var currentCommit = GetStashCommit(stashHeadCommitHash);
        while (currentCommit != null)
        {
            stashCommits.Add(currentCommit);
            currentCommit = GetStashCommit(currentCommit.ParentCommitHashes[0]);
        }
        return stashCommits;
    }

    private void SetStashHead(string stashHeadCommitHash)
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        File.WriteAllText(vcsRootDirectoryNavigator!.StashFile,stashHeadCommitHash);
    }

    public string GetStashHeadCommitHash()
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        return File.ReadAllText(vcsRootDirectoryNavigator!.StashFile);
    }

    public StashCommitFileModel? GetStashCommit(string hash)
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        var stashCommitFilePath = vcsRootDirectoryNavigator!.StashCommitsDirectory + Path.DirectorySeparatorChar + hash;
        return !File.Exists(stashCommitFilePath) ? null : new StashCommitFileModel(stashCommitFilePath);
    }
}