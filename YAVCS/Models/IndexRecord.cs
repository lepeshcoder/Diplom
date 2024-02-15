namespace YAVCS.Models;

public class IndexRecord
{
    // path relative to root repository directory
    public string RelativePath { get; }

    // file's blob hash 
    public string BlobHash { get; }
    
    public bool IsNew { get; } 

    public IndexRecord(string relativePath, string blobHash,bool isNew)
    {
        RelativePath = relativePath;
        BlobHash = blobHash;
        IsNew = isNew;
    }
    
    public override string ToString()
    {
        return RelativePath + ' ' + BlobHash + ' ' + IsNew;
    }
}