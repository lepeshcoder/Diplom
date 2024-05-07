using SynchrotronNet;
using YAVCS.Models;

namespace YAVCS.Services.Contracts;

public interface IMergeService
{
    bool IsOnMergeConflict();

    void SetMergeConflictSign(string branchIntoMergeCommitHash, string branchToMergeCommitHash);

    void ResetMergeConflictSign();

    CommitFileModel? GetCommonAncestor(string firstCommitHash, string secondCommitHash);

    string[] GetMergeBranches();

    MergeResultModel Merge(Dictionary<string,IndexRecord> commonAncestorIndexRecords,Dictionary<string, IndexRecord> firstCommitIndexRecords,
        Dictionary<string, IndexRecord> secondCommitIndexRecords,string firstCommitString,string secondCommitString);

    string[] CreateMergeResult(List<Diff.IMergeResultBlock> blocks, string firstBranchName, string secondBranchName);
}