namespace YAVCS.Services.Contracts;

public interface IBlobService
{
    void CreateBlob(byte[] data);

    bool IsBlobExist(string hash);

    HashSet<string> GetAllBlobs();

    void DeleteBlob(string blobHash);
}