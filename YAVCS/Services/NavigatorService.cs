using YAVCS.Constants;
using YAVCS.Services.Contracts;

namespace YAVCS.Services;

/*!
  \file   NavigatorService.cs
  \brief  Description in INavigatorService.cs

  \author lepesh
  \date   01.02.2024
*/

public class NavigatorService : INavigatorService
{
    // optimization(caching) field 
    private VcsRootDirectoryNavigator? _vcsRootDirectoryNavigator;
    
    // description in interface
    public VcsRootDirectoryNavigator? TryGetRepositoryRootDirectory()
    {
        // return cache if it is not null
        if (_vcsRootDirectoryNavigator != null)
        {
            return _vcsRootDirectoryNavigator;
        }
        // else is it repository root directory then cache and return it otherwise go to it's parent
        var currentDirectory = new DirectoryInfo(Environment.CurrentDirectory);
        while (currentDirectory != null)
        {
            if (IsRepositoryRootDirectory(currentDirectory.FullName))
            {
                _vcsRootDirectoryNavigator = new VcsRootDirectoryNavigator(currentDirectory.FullName);
                return _vcsRootDirectoryNavigator;
            }
            currentDirectory = Directory.GetParent(currentDirectory.FullName);
        }
        // if no directory before the root of the file system is a repository return null 
        return null;
    }

    // checks whether the directory is repository root directory
    private bool IsRepositoryRootDirectory(string currentDirectoryFullName)
    {
        // check that .yavcs folder exists in directory
        return Directory.Exists(currentDirectoryFullName + Path.DirectorySeparatorChar + FileSystemConstants.VcsRootDirectory);
    }
}