using spkl.Diffs;

namespace YAVCS.Models;

public class PatchModel
{
    public Dictionary<string, IndexRecord> FilesToAdd = [];
    public Dictionary<string, IndexRecord> FilesToDelete = [];
    public List<IndexRecord> ModifiedFiles = [];
    public List<MyersDiff<string>> Diffs = [];

    public PatchModel(Dictionary<string, IndexRecord> filesToAdd,Dictionary<string, IndexRecord> filesToDelete,
        List<IndexRecord> modifiedFiles, List<MyersDiff<string>> diffs)
    {
        FilesToAdd = filesToAdd;
        FilesToDelete = filesToDelete;
        ModifiedFiles = modifiedFiles;
        Diffs = diffs;
    }
}