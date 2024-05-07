namespace YAVCS.Models;

public class StashCommitFileModel : CommitFileModel
{

    public string BaseCommitHash;
    public StashCommitFileModel(string treeHash, DateTime createdAt, string message, string hash, List<string> parentCommitHashes, string baseCommitHash) 
        : base(treeHash, createdAt, message, hash, parentCommitHashes)
    {
        BaseCommitHash = baseCommitHash;
    }

    public StashCommitFileModel(string commitFilePath) : base(commitFilePath)
    {
        var lines = File.ReadAllLines(commitFilePath);
        BaseCommitHash = lines[^1];
    }


    public override string ToString()
    {
        return base.ToString() + "\n" + BaseCommitHash;
    }
}