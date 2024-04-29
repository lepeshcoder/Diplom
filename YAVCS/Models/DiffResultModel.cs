namespace YAVCS.Models;

public class DiffResultModel
{
    public List<string> Lines;
    public List<ConsoleColor> LineColors;

    public DiffResultModel(List<string> lines, List<ConsoleColor> lineColors)
    {
        Lines = lines;
        LineColors = lineColors;
    }
}