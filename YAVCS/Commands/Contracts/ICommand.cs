namespace YAVCS.Commands.Contracts;

/*!
  \file   ICommand.cs
  \brief  interface for commands(init,add,commit etc.)
  
  \author lepesh
  \date   31.01.2024
*/

public interface ICommand
{
    // Short command Description 
    string Description { get; }
    // Method that perform all command logic
    void Execute(string[] args);
}