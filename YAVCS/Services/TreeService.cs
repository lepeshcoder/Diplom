using System.Reflection.Metadata;
using YAVCS.Models;
using YAVCS.Services.Contracts;

namespace YAVCS.Services;

public class TreeService : ITreeService
{
    
    
    private readonly INavigatorService _navigatorService;
    private readonly IIndexService _indexService;
    private readonly IHashService _hashService;

    public TreeService(INavigatorService navigatorService, IIndexService indexService, IHashService hashService)
    {
        _navigatorService = navigatorService;
        _indexService = indexService;
        _hashService = hashService;
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

    private string GetTreeHash(TreeFileModel tree,Dictionary<string,TreeFileModel> treesByRelativePath,string path = "")
    {
        var temp = "";
        var childItems = tree.Childs.Values.ToList();
        foreach (var childItem in childItems)
        {
            if (childItem.Type == ChildItemModel.Types.Blob)
            {
                temp += childItem.Hash;
            }
            else
            {
                var childTreeRelativePath = temp + childItem.Name + Path.DirectorySeparatorChar;
                var childTree = treesByRelativePath[childTreeRelativePath];
                childTree.Hash = GetTreeHash(childTree,treesByRelativePath,childTreeRelativePath);
                temp += childTree.Hash;
            }
        }
        return _hashService.GetHash(temp);
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
            if (directoryName == null)
            {
                // add child file item to root tree
                var childFileItem = new ChildItemModel(fileName, ChildItemModel.Types.Blob, record.BlobHash);
                treesByRelativePath[""].TryAddChild(childFileItem);
            }
            else
            {
                var tempPath = "";
                var pathParts = directoryName.Split(Path.DirectorySeparatorChar);
                foreach (var pathPart in pathParts)
                {
                    var treeRelativePath = tempPath + pathPart + Path.DirectorySeparatorChar;
                    var childTreeItem = new ChildItemModel(pathPart, ChildItemModel.Types.Tree);
                    treesByRelativePath[tempPath].TryAddChild(childTreeItem);
                    if (!treesByRelativePath.ContainsKey(treeRelativePath))
                    {
                        var childTree = new TreeFileModel(pathPart, []);
                        treesByRelativePath.Add(treeRelativePath, childTree);
                    }
                    tempPath = treeRelativePath;
                }
                var childFileItem = new ChildItemModel(fileName, ChildItemModel.Types.Blob, record.BlobHash);
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
}