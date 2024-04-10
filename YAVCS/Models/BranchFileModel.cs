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

    public BranchFileModel(string branchFileAbsolutePath)
    {
        Name = Path.GetFileName(branchFileAbsolutePath);
        CommitHash = File.ReadAllText(branchFileAbsolutePath);
    }

    public override string ToString()
    {
        return CommitHash;
    }
}