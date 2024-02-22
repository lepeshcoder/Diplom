namespace YAVCS.Models;

public  class ChildItemModel
{
    public enum Types
    {
        Blob,
        Tree
    }
    
    public string Name { get; }
    
    public string Hash { get; set; }

    public Types Type { get; }

    public ChildItemModel(string name, Types type , string hash = "")
    {
        Name = name;
        Type = type;
        Hash = hash;
    }


    public override string ToString()
    {
        return Name + " " + Hash + " " + Type;
    }
}