using System.Security.Cryptography;
using YAVCS.Services.Contracts;

namespace YAVCS.Services;

public class HashService : IHashService
{
    public string GetHash(byte[] data)
    {
        var hashBytes = SHA256.HashData(data);
        var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        return hashString;
    }
}