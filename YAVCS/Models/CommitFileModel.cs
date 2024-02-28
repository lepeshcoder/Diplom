namespace YAVCS.Models;

public class CommitFileModel
{
    public string TreeHash;

    public DateTime CreatedAt;

    public string Message;

    public string Hash;

    public CommitFileModel(string treeHash, DateTime createdAt, string message,string hash)
    {
        TreeHash = treeHash;
        CreatedAt = createdAt;
        Message = message;
        Hash = hash;
    }

    public CommitFileModel(string commitFilePath)
    {
        var data = File.ReadAllLines(commitFilePath);
        TreeHash = data[0];
        CreatedAt = DateTime.Parse(data[1]);
        Message = data[2];
    }
    
    public override string ToString()
    {
        return TreeHash + '\n' + CreatedAt + '\n' + Message;
    }
    
}