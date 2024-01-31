using YAVCS.Commands.Contracts;

namespace YAVCS.Commands;

/*!
  \file   InitCommand.cs
  \brief  Init Command Logic
  
  Init - Create a repository if it doesn't exist,
  otherwise return exception "Repository already exists"

  \author lepesh
  \date   31.01.2024
*/


public class InitCommand : ICommand
{
    public string Description { get; } = "Init - Create a repository if it doesn't exist, otherwise return exception \"Repository already exists\"";

    public void Execute(string[] args)
    {
        throw new NotImplementedException();
    }
}