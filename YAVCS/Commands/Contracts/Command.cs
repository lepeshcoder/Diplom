namespace YAVCS.Commands.Contracts;

/*!
  \file   Command.cs
  \brief  Abstract class for all commands

  \author lepesh
  \date   03.02.2024
*/

public abstract class Command
{
    // Method that define the commandCase by args
    protected abstract Enum GetCommandCase(string[] args);
}