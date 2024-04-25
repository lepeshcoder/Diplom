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
    public string RepositoryRootDirectory => absoluteRepositoryPath;
    public string VcsRootDirectory => absoluteRepositoryPath + Path.DirectorySeparatorChar + FileSystemConstants.VcsRootDirectory;
    public string RefsDirectory => VcsRootDirectory + Path.DirectorySeparatorChar + FileSystemConstants.RefsDirectory;
    public string HeadsDirectory => RefsDirectory + Path.DirectorySeparatorChar + FileSystemConstants.HeadsDirectory;
    public string ObjectsDirectory => VcsRootDirectory + Path.DirectorySeparatorChar + FileSystemConstants.ObjectsDirectory;
    public string IndexFile => VcsRootDirectory + Path.DirectorySeparatorChar + FileSystemConstants.IndexFile;
    public string HeadFile => VcsRootDirectory + Path.DirectorySeparatorChar + FileSystemConstants.HeadFile;
    public string ConfigFile => VcsRootDirectory + Path.DirectorySeparatorChar + FileSystemConstants.ConfigFile;
    public string IgnoreFile => VcsRootDirectory + Path.DirectorySeparatorChar + FileSystemConstants.IgnoreFile;
    public string BlobsDirectory => ObjectsDirectory + Path.DirectorySeparatorChar + FileSystemConstants.BlobsDirectory;
    public string TreesDirectory => ObjectsDirectory + Path.DirectorySeparatorChar + FileSystemConstants.TreesDirectory;
    public string CommitsDirectory => ObjectsDirectory + Path.DirectorySeparatorChar + FileSystemConstants.CommitsDirectory;
    public string LogFile => VcsRootDirectory + Path.DirectorySeparatorChar + FileSystemConstants.LogFile;
    public string DetachedHeadFile => VcsRootDirectory + Path.DirectorySeparatorChar + FileSystemConstants.DetachedHeadFile;
    public string MergeConflictFile => VcsRootDirectory + Path.DirectorySeparatorChar + FileSystemConstants.MergeConflictFile;

}