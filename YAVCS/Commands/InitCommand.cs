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
    private readonly IBranchService _branchService;
    private readonly ICommitService _commitService;
    private readonly ITreeService _treeService;
    private readonly IHashService _hashService;

    public InitCommand(INavigatorService navigatorService, IConfigService configService, 
        IBranchService branchService, ICommitService commitService, ITreeService treeService, IHashService hashService)
    {
        _navigatorService = navigatorService;
        _configService = configService;
        _branchService = branchService;
        _commitService = commitService;
        _treeService = treeService;
        _hashService = hashService;
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
                    Console.WriteLine("Repository already exists in " + vcsRootDirectoryNavigator.RepositoryRootDirectory);
                    return;
                }
                // otherwise create .yavcs directory
                CreateVcsRootDirectory(workingDirectory);
                Console.WriteLine($"Repository successfully created at {workingDirectory}");
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
        Directory.CreateDirectory(vcsRootDirectoryNavigator.BlobsDirectory);
        Directory.CreateDirectory(vcsRootDirectoryNavigator.TreesDirectory);
        Directory.CreateDirectory(vcsRootDirectoryNavigator.CommitsDirectory);
        Directory.CreateDirectory(vcsRootDirectoryNavigator.HeadsDirectory);
        using (var fs = File.Create(vcsRootDirectoryNavigator.HeadFile)) {};
        using (var fs = File.Create(vcsRootDirectoryNavigator.IndexFile)) {};
        using (var fs = File.Create(vcsRootDirectoryNavigator.ConfigFile)) {};
        using (var fs = File.Create(vcsRootDirectoryNavigator.IgnoreFile)) {};
        using (var fs = File.Create(vcsRootDirectoryNavigator.DetachedHeadFile )) {};

        // create zeroCommit
        var zeroCommitTree = new TreeFileModel("zeroCommitTree", new Dictionary<string, ChildItemModel>(),
            _hashService.GetHash("ZeroCommit"));
        _treeService.CreateTree(zeroCommitTree);
        var zeroCommit = _commitService.CreateCommit(zeroCommitTree.Hash, DateTime.Now, "ZeroCommit", []);
        
        
        // Write default data to config file
        _configService.ReWriteConfig(new ConfigFileModel("user","email",DateTime.Now));

        var masterBranch = new BranchFileModel("Master", zeroCommit.Hash);
        _branchService.CreateBranch(masterBranch);
        _branchService.SetActiveBranch(masterBranch);
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