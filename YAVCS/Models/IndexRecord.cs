namespace YAVCS.Models;

public class IndexRecord : IEquatable<IndexRecord>
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

    public bool Equals(IndexRecord? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return RelativePath == other.RelativePath && BlobHash == other.BlobHash;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((IndexRecord)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(RelativePath, BlobHash);
    }
}