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


public class InitCommand : Command,ICommand
{

    private enum CommandCases
    {
        SyntaxError = 0,
        HelpCase = 1,
        DefaultCase = 2
    }
    
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
        // Get command case by args
        var commandCase = GetCommandCase(args);
        // Perform command by case
        switch (commandCase)
        {
            case CommandCases.SyntaxError:
            {
                Console.WriteLine("invalid args");
                break;
            }
            case CommandCases.HelpCase:
            {
                Console.WriteLine(Description);
                break;
            }
            case CommandCases.DefaultCase:
            {
                var workingDirectory = Environment.CurrentDirectory;
                var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
                // if directory is a part of repository 
                if (vcsRootDirectoryNavigator != null)
                {
                    Console.WriteLine("Repository already exists in " + vcsRootDirectoryNavigator.VcsRootDirectory);
                    return;
                }
                // otherwise create .yavcs directory
                CreateVcsRootDirectory(workingDirectory);
                break;
            }
        }
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
        using (var fs = File.Create(vcsRootDirectoryNavigator.HeadFile)) {};
        using (var fs = File.Create(vcsRootDirectoryNavigator.IndexFile)) {};
        using (var fs = File.Create(vcsRootDirectoryNavigator.ConfigFile)) {};
        using (var fs = File.Create(vcsRootDirectoryNavigator.IgnoreFile)) {};
        // Write default data to config file
        _configService.ReWriteConfig(new ConfigFileModel("user","email",DateTime.Now));
    }

    protected override Enum GetCommandCase(string[] args)
    {
        return args switch
        {
            ["--help"] => CommandCases.HelpCase,
            [] => CommandCases.DefaultCase,
            _ => CommandCases.SyntaxError
        };
    }
}