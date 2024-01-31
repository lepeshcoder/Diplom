using YAVCS.Constants;

namespace YAVCS;

/*!
  \file   VcsRootDirectoryNavigator.cs
  \brief  Class with VCS root directory navigation properties 

  primary constructor take 1 argument - absolute path to repository root directory
  Root directory format in FileSystemConstants.cs description

  \author lepesh
  \date   31.01.2024
*/

public class VcsRootDirectoryNavigator(string absoluteRepositoryPath)
{
    public string VcsRootDirectory => absoluteRepositoryPath + '/' + FileSystemConstants.VcsRootDirectory;
    public string RefsDirectory => VcsRootDirectory + '/' + FileSystemConstants.RefsDirectory;
    public string HeadsDirectory => RefsDirectory + '/' + FileSystemConstants.HeadsDirectory;
    public string ObjectsDirectory => VcsRootDirectory + '/' + FileSystemConstants.ObjectsDirectory;
    public string IndexFile => VcsRootDirectory + '/' + FileSystemConstants.IndexFile;
    public string HeadFile => VcsRootDirectory + '/' + FileSystemConstants.HeadFile;
    public string ConfigFile => VcsRootDirectory + '/' + FileSystemConstants.ConfigFile;
}