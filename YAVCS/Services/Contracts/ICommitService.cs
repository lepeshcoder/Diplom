using YAVCS.Models;

namespace YAVCS.Services.Contracts;

public interface ICommitService
{
    CommitFileModel CreateCommit(string treeHash,DateTime createdAt, string message);
}