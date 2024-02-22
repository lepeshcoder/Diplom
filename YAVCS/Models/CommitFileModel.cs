namespace YAVCS.Models;

public class CommitFileModel
{
    public string TreeHash;

    public DateTime CreatedAt;

    public string Message;

    public CommitFileModel(string treeHash, DateTime createdAt, string message)
    {
        TreeHash = treeHash;
        CreatedAt = createdAt;
        Message = message;
    }

    public override string ToString()
    {
        return TreeHash + '\n' + CreatedAt + '\n' + Message;
    }
}