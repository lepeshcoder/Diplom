using System.Net;
using YAVCS.Commands.Contracts;
using YAVCS.Models;
using YAVCS.Services.Contracts;

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
    
    public string Description => "Init - Create a repository if it doesn't exist, otherwise return exception \"Repository already exists\"";

    // Services for command logic
    private readonly INavigatorService _navigatorService;
    private readonly IConfigService _configService;

    public InitCommand(INavigatorService navigatorService, IConfigService configService)
    {
        _navigatorService = navigatorService;
        _configService = configService;
    }

    public void Execute(string[] args)
    {
        // give command format 
        if (args[0] == "--help")
        {
            Console.WriteLine(Description);
            return;
        }
        var workingDirectory = Environment.CurrentDirectory;
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory(workingDirectory);
        // if directory is a part of repository 
        if (vcsRootDirectoryNavigator != null)
        {
            Console.WriteLine("Repository already exists");
            return;
        }
        // otherwise create .yavcs directory
        CreateVcsRootDirectory(workingDirectory);
    }

    // create .yavcs directory and all inner directories/files
    private void CreateVcsRootDirectory(string repositoryRootDirectoryAbsolutePath)
    {
        // create vcsRootDirectoryNavigator instance for simple navigation
        var vcsRootDirectoryNavigator = new VcsRootDirectoryNavigator(repositoryRootDirectoryAbsolutePath);
        // create .yavcs directory
        Directory.CreateDirectory(vcsRootDirectoryNavigator.VcsRootDirectory);
        // create inner directories and files
        Directory.CreateDirectory(vcsRootDirectoryNavigator.RefsDirectory);
        Directory.CreateDirectory(vcsRootDirectoryNavigator.ObjectsDirectory);
        Directory.CreateDirectory(vcsRootDirectoryNavigator.HeadsDirectory);
        File.Create(vcsRootDirectoryNavigator.HeadFile);
        File.Create(vcsRootDirectoryNavigator.IndexFile);
        File.Create(vcsRootDirectoryNavigator.ConfigFile);
        // Write default data to config file
        _configService.ReWriteConfig(vcsRootDirectoryNavigator.ConfigFile,
            new ConfigFileModel("user","email",DateTime.Now));
    }
}