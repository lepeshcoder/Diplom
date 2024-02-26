namespace YAVCS.Constants;
/*!
  \file   FileSystemConstants.cs
  \brief  File with general folders/files names
  
  Vcs root Directory Format:
  .yavcs/
    refs/
      heads/
      tags/
    objects/
      pack/
    index 
    head
    config
    ignore
      
  \author lepesh
  \date   31.01.2024
*/

public static class FileSystemConstants
{
    // root folder name 
    public const string VcsRootDirectory = ".yavcs";
    // refs folder name
    public const string RefsDirectory = "refs";
    // heads(branches) folder name
    public const string HeadsDirectory = "heads";
    // objects folder name
    public const string ObjectsDirectory = "objects";
    // index file name
    public const string IndexFile = "index";
    // head file name
    public const string HeadFile = "head";
    // config File name
    public const string ConfigFile = "config";
    // ignore File name
    public const string IgnoreFile = "ignore";
    // blobs Directory
    public const string BlobsDirectory = "blobs";
    // trees Directory
    public const string TreesDirectory = "trees";
    // commits Directory 
    public const string CommitsDirectory = "commits";
    // log file
    public const string LogFile = "log";
}