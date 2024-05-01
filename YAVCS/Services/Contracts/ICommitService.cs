using YAVCS.Models;

namespace YAVCS.Services.Contracts;

public interface ICommitService
{
    CommitFileModel CreateCommit(string treeHash,DateTime createdAt, string message,List<string> parentCommitHashes);

    CommitFileModel? GetCommitByHash(string commitHash);

    Dictionary<string,IndexRecord> GetHeadRecordsByPath();

    bool IsIndexSameFromHead();

    HashSet<string> GetAllCommitsHashes();
    void DeleteCommit(string commitHash);
}