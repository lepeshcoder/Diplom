namespace YAVCS.Services.Contracts;

/*!
  \file   INavigatorService.cs
  \brief  Interface for Navigator Service

    provide method for finding repository root directory
        
  \author lepesh
  \date   01.02.2024
*/

public interface INavigatorService
{
    /// <summary>
    /// using to find repository root directory
    /// </summary>
    /// <param name="workingDirectoryAbsolutePath"> Working directory from that tool perform.</param>
    /// <returns>
    /// Return VcsRootDirectoryNavigator instance if work directory is a part of a repository
    /// otherwise return null.</returns>
    /// <remarks>
    /// Дополнительные заметки и информация о методе.
    /// </remarks>
    VcsRootDirectoryNavigator? TryGetRepositoryRootDirectory(string workingDirectoryAbsolutePath);
}