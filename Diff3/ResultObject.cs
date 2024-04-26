using System.Collections.Generic;

namespace Verano.Diff3Way;

public class ResultObject
{
    public List<string> Result;
    public bool IsConflict;

    public ResultObject(List<string> result, bool isConflict)
    {
        Result = result;
        IsConflict = isConflict;
    }
}