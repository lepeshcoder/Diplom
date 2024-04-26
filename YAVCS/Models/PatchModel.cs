using spkl.Diffs;

namespace YAVCS.Models;

public class PatchModel
{
    public string BranchName; 
    public Dictionary<string, IndexRecord> FilesToAdd = [];
    public Dictionary<string, IndexRecord> FilesToDelete = [];
    public Dictionary<string, IndexRecord> ModifiedFiles = [];

    public PatchModel(Dictionary<string, IndexRecord> filesToAdd,Dictionary<string, IndexRecord> filesToDelete,
        Dictionary<string, IndexRecord> modifiedFiles, string branchName)
    {
        FilesToAdd = filesToAdd;
        FilesToDelete = filesToDelete;
        ModifiedFiles = modifiedFiles;
        BranchName = branchName;
    }
}