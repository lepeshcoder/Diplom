namespace YAVCS.Models;

public class TreeFileModel
{

    public string Hash { get; set; }
    public string Name { get; }

    // for fast contains method
    public Dictionary<string,ChildItemModel> Childs { get; }

    public TreeFileModel(string  name, Dictionary<string,ChildItemModel> childs, string hash = "")
    {
        Name = name;
        Childs = childs;
        Hash = hash;
    }

    public void TryAddChild(ChildItemModel childItem)
    {
        Childs.TryAdd(childItem.Name, childItem);
    }

    public override string ToString()
    {
        return Childs.Values.Aggregate(Name + '\n', (current, child) => current + child + '\n');
    }
}