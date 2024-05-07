using YAVCS.Models;

namespace YAVCS.Services.Contracts;

public interface IDiffService
{
    DiffResultModel GetDiff(string[] previousVersion, string[] currentVersion);

    DiffResultModel GetDiff(Dictionary<string, IndexRecord> baseCommitRecords,
        Dictionary<string, IndexRecord> commitToCompareIndexRecords);
}