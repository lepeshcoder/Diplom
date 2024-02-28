namespace YAVCS.Models;

public class BranchFileModel
{
    public string Name;

    public string CommitHash;

    public BranchFileModel(string name, string commitHash)
    {
        Name = name;
        CommitHash = commitHash;
    }

    public override string ToString()
    {
        return CommitHash;
    }
}