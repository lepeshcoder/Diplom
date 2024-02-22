namespace YAVCS.Services.Contracts;

public interface ICommitService
{
    void CreateCommit(string treeHash,DateTime createdAt, string message);
}