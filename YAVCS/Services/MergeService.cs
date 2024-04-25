using YAVCS.Models;
using YAVCS.Services.Contracts;

namespace YAVCS.Services;

public class MergeService : IMergeService
{
    private readonly INavigatorService _navigatorService;
    private Dictionary<string,CommitFileModel> _commitsByHash = [];

    public MergeService(INavigatorService navigatorService)
    {
        _navigatorService = navigatorService;
    }

    public bool IsOnMergeConflict()
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        return File.Exists(vcsRootDirectoryNavigator!.MergeConflictFile);
    }

    public void SetMergeConflictSign()
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        using (var fs = File.Create(vcsRootDirectoryNavigator!.MergeConflictFile )) {};
    }

    public void ResetMergeConflictSign()
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        File.Delete(vcsRootDirectoryNavigator!.MergeConflictFile);
    }

    public CommitFileModel? GetCommonAncestor(string firstCommitHash, string secondCommitHash)
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        foreach (var commitFilePath in Directory.GetFiles(vcsRootDirectoryNavigator!.CommitsDirectory))
        {
            var commit = new CommitFileModel(commitFilePath);
            _commitsByHash.Add(commit.Hash, commit);
        }
        
        Queue<string> ancestors1 = [];
        Queue<string> ancestors2 = [];
        ancestors1.Enqueue(firstCommitHash);
        ancestors2.Enqueue(secondCommitHash);
        HashSet<string> visited1 = [];
        HashSet<string> visited2 = [];

        while (ancestors1.Count > 0 || ancestors2.Count > 0)
        {
            if (ancestors1.Count > 0)
            {
                var current1 = ancestors1.Dequeue();
                if (visited2.Contains(current1))
                {
                    return _commitsByHash[current1];
                }
                visited1.Add(current1);
                var commit1 = _commitsByHash[current1];
                foreach (var parent in commit1.ParentCommitHashes)
                {
                    if (!visited1.Contains(parent))
                    {
                        ancestors1.Enqueue(parent);
                    }
                }
            }

            if (ancestors2.Count > 0)
            {
                var current2 = ancestors2.Dequeue();
                if (visited1.Contains(current2))
                {
                    return _commitsByHash[current2];
                }
                visited2.Add(current2);
                var commit2 = _commitsByHash[current2];
                foreach (var parent in commit2.ParentCommitHashes)
                {
                    if (!visited2.Contains(parent))
                    {
                        ancestors2.Enqueue(parent);
                    }
                }
            }
        }

        return null;
    }
}