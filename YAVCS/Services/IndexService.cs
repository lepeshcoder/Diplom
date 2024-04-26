using System.Collections.Concurrent;
using YAVCS.Models;
using YAVCS.Services.Contracts;

namespace YAVCS.Services;

public class IndexService : IIndexService
{
    
    //services
    private readonly INavigatorService _navigatorService;
    private readonly ITreeService _treeService;

    // Dictionary of index records by path
    private Dictionary<string, IndexRecord> _recordsByPath = new();
    
    public IndexService(INavigatorService navigatorService, ITreeService treeService)
    {
        // fill dictionary with data in index file
        _navigatorService = navigatorService;
        _treeService = treeService;
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        if (vcsRootDirectoryNavigator == null) return;
        var records = File.ReadAllLines(vcsRootDirectoryNavigator.IndexFile);
        foreach (var record in records)
        {
            var parts = record.Split(' ');
            var path = parts[0];
            var hash = parts[1];
            _recordsByPath.Add(path, new IndexRecord(path,hash));
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

    public void ClearIndex()
    {
        _recordsByPath.Clear();
        SaveChanges();
    }

    public void ResetIndexToState(string treeHash)
    {
        // reset index
        var newHeadCommitIndexRecords = _treeService.GetTreeRecordsByPath(treeHash);
        ClearIndex();
        foreach (var indexRecord in newHeadCommitIndexRecords.Values)
        {
            AddRecord(indexRecord);
        } 
        SaveChanges();
    }
}