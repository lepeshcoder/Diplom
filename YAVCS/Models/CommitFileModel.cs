namespace YAVCS.Models;

public class CommitFileModel
{
    public string TreeHash;

    public DateTime CreatedAt;

    public string Message;

    public string Hash;

    public List<string> ParentCommitHashes = [];

    public CommitFileModel(string treeHash, DateTime createdAt, string message,string hash,List<string> parentCommitHashes)
    {
        TreeHash = treeHash;
        CreatedAt = createdAt;
        Message = message;
        Hash = hash;
        ParentCommitHashes = parentCommitHashes;
    }

    public CommitFileModel(string commitFilePath)
    {
        var data = File.ReadAllLines(commitFilePath);
        TreeHash = data[0];
        CreatedAt = DateTime.Parse(data[1]);
        Message = data[2];
        ParentCommitHashes.AddRange(data.Skip(3));
        Hash = Path.GetFileName(commitFilePath);
    }
    
    public override string ToString()
    {
        return TreeHash + '\n' + CreatedAt + '\n' + Message + '\n' + string.Join("\n",ParentCommitHashes);
    }
    
}