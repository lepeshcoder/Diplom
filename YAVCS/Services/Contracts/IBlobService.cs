namespace YAVCS.Services.Contracts;

public interface IBlobService
{
    void CreateBlob(byte[] data);

    bool IsBlobExist(string hash);
}