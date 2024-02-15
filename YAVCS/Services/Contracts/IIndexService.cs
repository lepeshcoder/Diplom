using YAVCS.Models;

namespace YAVCS.Services.Contracts;

public interface IIndexService
{
    void AddRecord(IndexRecord record);
    void DeleteRecord(string relativePath);
    bool IsRecordExist(string relativePath);
    IndexRecord? TryGetRecordByPath(string relativePath);
    void SaveChanges();
    Dictionary<string,IndexRecord> GetRecords();
}