using YAVCS.Models;

namespace YAVCS.Services.Contracts;

public interface ITreeService
{
   // create and write tree accourding to index file and returns hash of the root tree
   string CreateTreeByIndex();

   string CreateTreeByRecords(Dictionary<string,IndexRecord> recordsByPath);
   
   TreeFileModel GetTreeByHash(string rootTreeHash);

   Dictionary<string, IndexRecord> GetTreeRecordsByPath(string treeHash);

   void CreateTree(TreeFileModel tree);
   
   
}