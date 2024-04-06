namespace YAVCS.Models;

public class CommitFileModel
{
    public string TreeHash;

    public DateTime CreatedAt;

    public string Message;

    public string Hash;

    public string ParentCommitHash;

    public CommitFileModel(string treeHash, DateTime createdAt, string message,string hash,string parentCommitHash)
    {
        TreeHash = treeHash;
        CreatedAt = createdAt;
        Message = message;
        Hash = hash;
        ParentCommitHash = parentCommitHash;
    }

    public CommitFileModel(string commitFilePath)
    {
        var data = File.ReadAllLines(commitFilePath);
        TreeHash = data[0];
        CreatedAt = DateTime.Parse(data[1]);
        Message = data[2];
        ParentCommitHash = data[3];
        Hash = Path.GetFileName(commitFilePath);
    }
    
    public override string ToString()
    {
        return TreeHash + '\n' + CreatedAt + '\n' + Message + '\n' + ParentCommitHash;
    }
    
}