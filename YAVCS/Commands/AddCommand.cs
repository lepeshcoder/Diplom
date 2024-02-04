using YAVCS.Commands.Contracts;

namespace YAVCS.Commands;

/*!
  \file   AddCommand.cs
  \brief  Add command logic

    Add - add file or directory to staging area(index)
    
  \author lepesh
  \date   03.02.2024
*/

public class AddCommand : Command,ICommand
{
    private enum CommandCases : int
    {
        SyntaxError = 0,
        AddAll = 1,
        AddItem = 2
    }
    
    public string Description => "add file or directory to staging area(index)";

    public void Execute(string[] args)
    {
        throw new NotImplementedException();
    }

    protected override Enum GetCommandCase(string[] args)
    {
        throw new NotImplementedException();
    }

}