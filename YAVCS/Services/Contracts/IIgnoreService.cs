namespace YAVCS.Services.Contracts;

/*!
  \file   IIgnoreService.cs
  \brief  Interface for Config service

    Service for manipulation with Ignore File

  \author lepesh
  \date   05.02.2024
*/

public interface IIgnoreService
{
    // checks whether the file should be included in the index based on the ignore rules in ignore file
    bool CheckIgnoreRules(string itemAbsolutePath);
    
}