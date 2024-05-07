using System.Reflection.Metadata;
using YAVCS.Models;
using YAVCS.Services.Contracts;

namespace YAVCS.Services;

public class TreeService : ITreeService
{
    
    
    private readonly INavigatorService _navigatorService;
    private readonly IIndexService _indexService;
    private readonly IHashService _hashService;
    private readonly IBlobService _blobService;
    private readonly IIgnoreService _ignoreService;

    public TreeService(INavigatorService navigatorService, IIndexService indexService,
        IHashService hashService, IBlobService blobService, IIgnoreService ignoreService)
    {
        _navigatorService = navigatorService;
        _indexService = indexService;
        _hashService = hashService;
        _blobService = blobService;
        _ignoreService = ignoreService;
    }
    
    public string CreateTreeByIndex()
    {
        // get index records
        var indexRecords = _indexService.GetRecords().Values.ToList();
        var treesByRelativePath = FillTreesDictionary(indexRecords);
        var rootTree = treesByRelativePath[""];
        var rootTreeHash = GetTreeHash(rootTree,treesByRelativePath);
        //writing after GetTreeHash because all tree hashes computes recursively
        foreach (var tree in treesByRelativePath.Values)
        {
            WriteTree(tree);
        }
        return rootTreeHash;
    }

    public string CreateTreeByWorkingDirectory()
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        var rootTreeHash = FillTreeByWorkingDirectory(vcsRootDirectoryNavigator!.RepositoryRootDirectory);
        return rootTreeHash;
    }

    public string CreateTreeByRecords(Dictionary<string,IndexRecord> recordsByPath)
    {
        var treesByRelativePath = FillTreesDictionary(recordsByPath.Values.ToList());
        var rootTree = treesByRelativePath[""];
        var rootTreeHash = GetTreeHash(rootTree,treesByRelativePath);
        //writing after GetTreeHash because all tree hashes computes recursively
        foreach (var tree in treesByRelativePath.Values)
        {
            WriteTree(tree);
        }
        return rootTreeHash;
    }

    public TreeFileModel GetTreeByHash(string treeHash)
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        var treeFile = vcsRootDirectoryNavigator!.TreesDirectory + Path.DirectorySeparatorChar + treeHash;
        var lines = File.ReadAllLines(treeFile);
        var treeName = lines[0];
        var childs = new Dictionary<string, ChildItemModel>();
        for (var i = 1; i < lines.Length; i++)
        {
            var parts = lines[i].Split(' ');
            var childName = parts[0];
            var childHash = parts[1];
            var childType = int.Parse(parts[2]);
            childs.Add(childName,new ChildItemModel(childName,childType,childHash));
        }
        return new TreeFileModel(treeName, childs, treeHash);
    }

    public Dictionary<string, IndexRecord> GetTreeRecordsByPath(string treeHash)
    {
        var tree = GetTreeByHash(treeHash);
        var recordsByPath = new Dictionary<string, IndexRecord>();
        ParseTree(tree, recordsByPath);
        return recordsByPath;
    }

    public void CreateTree(TreeFileModel tree)
    {
        WriteTree(tree);
    }

    public void ResetWorkingDirectoryToState(string treeHash)
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        //reset working tree
        var allDirectories = Directory.GetDirectories(vcsRootDirectoryNavigator!.RepositoryRootDirectory);
        var allFiles = Directory.GetFiles(vcsRootDirectoryNavigator.RepositoryRootDirectory);

        foreach (var file in allFiles)
        {
            File.Delete(file);   
        }

        foreach (var directory in allDirectories)
        {
            if(directory != vcsRootDirectoryNavigator.VcsRootDirectory)
                Directory.Delete(directory,true);
        }

        var treeRecords = GetTreeRecordsByPath(treeHash);
        foreach (var indexRecord in treeRecords.Values)
        {
            var absolutePath = vcsRootDirectoryNavigator.RepositoryRootDirectory +
                               Path.DirectorySeparatorChar + indexRecord.RelativePath;
                    
            var directoryName = Path.GetDirectoryName(absolutePath);
            if (directoryName != null)
            {
                Directory.CreateDirectory(directoryName);
            }
            using (var fs = File.Create(absolutePath)) {};
            File.WriteAllBytes(absolutePath,_blobService.GetBlobData(indexRecord.BlobHash));
        }
    }

    public void ResetIndexToState(string treeHash)
    {
        // reset index
        var newHeadCommitIndexRecords = GetTreeRecordsByPath(treeHash);
        _indexService.ClearIndex();
        foreach (var indexRecord in newHeadCommitIndexRecords.Values)
        {
            _indexService.AddRecord(indexRecord);
        } 
        _indexService.SaveChanges();
    }

    public void DeleteTree(string commitRootTreeHash)
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        var treeChilds = GetTreeByHash(commitRootTreeHash).Childs;
        var treePath = vcsRootDirectoryNavigator!.TreesDirectory + Path.DirectorySeparatorChar + commitRootTreeHash;
        File.Delete(treePath);
        foreach (var child in treeChilds.Values.Where(child => child.Type == (int)ChildItemModel.Types.Tree))
        {
            DeleteTree(child.Hash);
        }
    }

    private string GetTreeHash(TreeFileModel tree,Dictionary<string,TreeFileModel> treesByRelativePath,string path = "")
    {
        var temp = "";
        var childItems = tree.Childs.Values.ToList();
        foreach (var childItem in childItems)
        {
            if (childItem.Type == (int)ChildItemModel.Types.Blob)
            {
                temp += childItem.Hash + childItem.Name;
            }
            else
            {
                var childTreeRelativePath = path + childItem.Name + Path.DirectorySeparatorChar;
                var childTree = treesByRelativePath[childTreeRelativePath];
                childItem.Hash = GetTreeHash(childTree,treesByRelativePath,childTreeRelativePath);
                temp += childItem.Hash;
            }
        }
        tree.Hash = _hashService.GetHash(temp);
        return tree.Hash;
    }

    private Dictionary<string, TreeFileModel> FillTreesDictionary(List<IndexRecord> records)
    {
        // create dictionary with trees, where key is a relative to repositoryRootDirectory to provide uniqueness 
        // of trees that contain only it's name and added repository root directory
        var treesByRelativePath = new Dictionary<string, TreeFileModel> {
            { "", new TreeFileModel("",[]) } 
        };
        foreach (var record in records)
        {
            var fileName = Path.GetFileName(record.RelativePath);
            var directoryName = Path.GetDirectoryName(record.RelativePath);
            // case when file is in repository root directory
            if (directoryName == "")
            {
                // add child file item to root tree
                var childFileItem = new ChildItemModel(fileName, (int)ChildItemModel.Types.Blob, record.BlobHash);
                treesByRelativePath[""].TryAddChild(childFileItem);
            }
            else
            {
                var tempPath = "";
                var pathParts = directoryName!.Split(Path.DirectorySeparatorChar);
                foreach (var pathPart in pathParts)
                {
                    var treeRelativePath = tempPath + pathPart + Path.DirectorySeparatorChar;
                    var childTreeItem = new ChildItemModel(pathPart, (int)ChildItemModel.Types.Tree);
                    treesByRelativePath[tempPath].TryAddChild(childTreeItem);
                    if (!treesByRelativePath.ContainsKey(treeRelativePath))
                    {
                        var childTree = new TreeFileModel(pathPart, []);
                        treesByRelativePath.Add(treeRelativePath, childTree);
                    }
                    tempPath = treeRelativePath;
                }
                var childFileItem = new ChildItemModel(fileName, (int)ChildItemModel.Types.Blob, record.BlobHash);
                treesByRelativePath[tempPath].TryAddChild(childFileItem);
            }
        }
        return treesByRelativePath;
    }

    private void WriteTree(TreeFileModel tree)
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        var treeFile = vcsRootDirectoryNavigator!.TreesDirectory + Path.DirectorySeparatorChar + tree.Hash;
        File.WriteAllText(treeFile,tree.ToString());
    }

   void ParseTree(TreeFileModel tree, Dictionary<string, IndexRecord> records, string path = "")
    {
        foreach (var child in tree.Childs)
        {
            var childType = child.Value.Type;
            if (childType == (int)ChildItemModel.Types.Blob)
            {
                var childPath = path + child.Value.Name;
                if (!_ignoreService.IsItemIgnored(childPath))
                {
                    records.Add(childPath, new IndexRecord(childPath, child.Value.Hash));
                }
            }
            else
            {
                var childTree = GetTreeByHash(child.Value.Hash);
                var newPath = path + child.Value.Name + Path.DirectorySeparatorChar;
                if (!_ignoreService.IsItemIgnored(newPath))
                {
                    ParseTree(childTree, records, newPath);
                }
            }
        }
    }

    private string FillTreeByWorkingDirectory(string absoluteDirectoryPath)
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        var directoryRelativePath =
            Path.GetRelativePath(vcsRootDirectoryNavigator!.RepositoryRootDirectory, absoluteDirectoryPath);
        var tree = new TreeFileModel(directoryRelativePath, []);
        var treeHashString = "";
        foreach (var itemPath in Directory.GetFileSystemEntries(absoluteDirectoryPath))
        {
            var itemRelativePath = Path.GetRelativePath(absoluteDirectoryPath, itemPath);
            if (File.Exists(itemPath))
            {
                var fileBytes = File.ReadAllBytes(itemPath);
                var blobHash = _hashService.GetHash(fileBytes);
                if (!_blobService.IsBlobExist(blobHash))
                {
                    _blobService.CreateBlob(fileBytes);
                }
                tree.Childs.Add(itemRelativePath,new ChildItemModel(itemRelativePath,(int)ChildItemModel.Types.Blob,blobHash));
                treeHashString += itemPath + blobHash;
            }
            else if (Directory.Exists(itemPath))
            {
                var childTreeHash = FillTreeByWorkingDirectory(itemPath);
                tree.Childs.Add(itemRelativePath,new ChildItemModel(itemRelativePath,(int)ChildItemModel.Types.Tree,childTreeHash));
                treeHashString += childTreeHash;
            }
        }
        tree.Hash = _hashService.GetHash(treeHashString);
        WriteTree(tree);
        return tree.Hash;
    }
}