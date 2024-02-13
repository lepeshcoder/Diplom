namespace YAVCS.Models;

public class IndexRecord
{
    // path relative to root repository directory
    public string RelativePath { get; }

    // file's blob hash 
    public string BlobHash { get; }

    public IndexRecord(string relativePath, string blobHash)
    {
        RelativePath = relativePath;
        BlobHash = blobHash;
    }
    
    public override string ToString()
    {
        return RelativePath + ' ' + BlobHash;
    }
}