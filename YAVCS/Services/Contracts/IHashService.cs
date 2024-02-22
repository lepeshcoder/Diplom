namespace YAVCS.Services.Contracts;

public interface IHashService
{
    string GetHash(byte[] data);

    public string GetHash(string data);
}