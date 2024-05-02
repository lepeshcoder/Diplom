using YAVCS.Models;

namespace YAVCS.Services.Contracts;

public interface IMergeService
{
    bool IsOnMergeConflict();

    void SetMergeConflictSign(string branchIntoMergeCommitHash, string branchToMergeCommitHash);

    void ResetMergeConflictSign();

    CommitFileModel? GetCommonAncestor(string firstCommitHash, string secondCommitHash);

    string[] GetMergeBranches();
}