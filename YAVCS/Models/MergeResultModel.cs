namespace YAVCS.Models;

public class MergeResultModel
{
    public Dictionary<string,IndexRecord> IndexRecords;
    public List<string> ConflictPaths;

    public MergeResultModel(Dictionary<string,IndexRecord> indexRecords, List<string> conflictPaths)
    {
        IndexRecords = indexRecords;
        ConflictPaths = conflictPaths;
    }
}