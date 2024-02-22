using System.Collections.Concurrent;
using YAVCS.Models;
using YAVCS.Services.Contracts;

namespace YAVCS.Services;

public class IndexService : IIndexService
{
    
    //services
    private readonly INavigatorService _navigatorService;

    // Dictionary of index records by path
    private Dictionary<string, IndexRecord> _recordsByPath = new();
    
    public IndexService(INavigatorService navigatorService)
    {
        // fill dictionary with data in index file
        _navigatorService = navigatorService;
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        if (vcsRootDirectoryNavigator == null) return;
        var records = File.ReadAllLines(vcsRootDirectoryNavigator.IndexFile);
        foreach (var record in records)
        {
            var parts = record.Split(' ');
            var path = parts[0];
            var hash = parts[1];
            var success = bool.TryParse(parts[2],out var isNew);
            if (!success) throw new Exception("Invalid indexRecord format");
            _recordsByPath.Add(path, new IndexRecord(path,hash,isNew));
        }
    }

    public void AddRecord(IndexRecord record)
    {
        // update dictionary
        _recordsByPath.Add(record.RelativePath,record);
    }

    public void DeleteRecord(string path)
    {
        // update dictionary
        _recordsByPath.Remove(path);
    }

    public bool IsRecordExist(string path)
    {
        return _recordsByPath.ContainsKey(path);
    }

    public IndexRecord? TryGetRecordByPath(string relativePath)
    {
        return _recordsByPath.GetValueOrDefault(relativePath);
    }

    public void SaveChanges()
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        if (vcsRootDirectoryNavigator == null) return;
        var records = _recordsByPath.Values.Select(x => x.ToString()).ToArray();
        File.WriteAllLines(vcsRootDirectoryNavigator.IndexFile,records);
    }

    public Dictionary<string, IndexRecord> GetRecords()
    {
        return _recordsByPath;
    }

    public bool IsIndexEmpty()
    {
        return _recordsByPath.Count == 0;
    }
}