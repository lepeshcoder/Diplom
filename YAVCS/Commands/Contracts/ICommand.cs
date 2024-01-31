namespace YAVCS.Commands.Contracts;

/*!
  \file   ICommand.cs
  \brief  interface for commands(init,add,commit etc.)
  
  \author lepesh
  \date   31.01.2024
*/

public interface ICommand
{
    string Description { get; }  
    void Execute(string[] args);
}