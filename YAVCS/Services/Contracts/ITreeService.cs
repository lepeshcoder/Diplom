using YAVCS.Models;

namespace YAVCS.Services.Contracts;

public interface ITreeService
{
   // create and write tree accourding to index file and returns hash of the root tree
   string CreateTreeByIndex();
}