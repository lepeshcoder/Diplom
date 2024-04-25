using YAVCS.Models;

namespace YAVCS.Services.Contracts;

public interface IMergeService
{
    bool IsOnMergeConflict();

    void SetMergeConflictSign();

    void ResetMergeConflictSign();

    CommitFileModel? GetCommonAncestor(string firstCommitHash, string secondCommitHash);
}