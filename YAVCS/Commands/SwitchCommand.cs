﻿using YAVCS.Commands.Contracts;
using YAVCS.Exceptions;
using YAVCS.Services.Contracts;

namespace YAVCS.Commands;

public class SwitchCommand : Command,ICommand
{

    private readonly INavigatorService _navigatorService;
    private readonly IBranchService _branchService;
    private readonly ITreeService _treeService;
    private readonly IIndexService _indexService;
    private readonly IBlobService _blobService;
    private readonly ICommitService _commitService;

    public SwitchCommand(INavigatorService navigatorService, IBranchService branchService,
        ITreeService treeService, IIndexService indexService, IBlobService blobService, ICommitService commitService)
    {
        _navigatorService = navigatorService;
        _branchService = branchService;
        _treeService = treeService;
        _indexService = indexService;
        _blobService = blobService;
        _commitService = commitService;
    }

    private enum CommandCases
    {
        SyntaxError = 0,
        HelpCase = 1,
        DefaultCase = 2
    }
    
    protected override Enum GetCommandCase(string[] args)
    {
        return args switch
        {
            ["--help"] => CommandCases.HelpCase,
            _ when args.Length == 1 => CommandCases.DefaultCase,
            _ => CommandCases.SyntaxError
        };
    }

    public string Description => "Switch between branches";
    
    public void Execute(string[] args)
    {
        var commandCase = GetCommandCase(args);
        switch (commandCase)
        {
            case CommandCases.HelpCase:
            {
                Console.WriteLine(Description);
                break;
            }
            case CommandCases.SyntaxError:
            {
                Console.WriteLine("Invalid args Format");
                break;
            }
            case CommandCases.DefaultCase:
            {
                var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
                if (vcsRootDirectoryNavigator == null)
                {
                    throw new RepositoryNotFoundException("not a part of repository");
                }
                var branchToSwitchName = args[0];
                var branchToSwitch = _branchService.GetBranchByName(branchToSwitchName);
                
                if (branchToSwitch == null)
                {
                    throw new ArgumentException($"Branch {branchToSwitchName} doesn't exist");
                }

                var activeBranch = _branchService.GetActiveBranch();
                var activeBranchName = activeBranch?.Name ?? "Master";
                if (activeBranchName == branchToSwitchName)
                {
                    throw new ArgumentException($"Already on branch {activeBranchName}");
                }

                var activeBranchCommitHash = activeBranch?.CommitHash ?? "ZeroCommit";

                if (branchToSwitch.CommitHash == "ZeroCommit")
                {
                    throw new ArgumentException("Cannot Switch to ZeroCommit");
                }

                var branchToSwitchHeadCommit = _commitService.GetCommitByHash(branchToSwitch.CommitHash);
                
                if (activeBranchCommitHash != branchToSwitch.CommitHash)
                {
                    // reset index
                    var newHeadCommitIndexRecords = _treeService.GetTreeRecordsByPath(branchToSwitchHeadCommit.TreeHash);
                    _indexService.ClearIndex();
                    foreach (var indexRecord in newHeadCommitIndexRecords.Values)
                    {
                        _indexService.AddRecord(indexRecord);
                    } 
                    _indexService.SaveChanges();
                
                    //reset working tree
                    var allDirectories = Directory.GetDirectories(vcsRootDirectoryNavigator.RepositoryRootDirectory);
                    var allFiles = Directory.GetFiles(vcsRootDirectoryNavigator.RepositoryRootDirectory);

                    foreach (var file in allFiles)
                    {
                        File.Delete(file);   
                    }

                    foreach (var directory in allDirectories)
                    {
                        if(directory != vcsRootDirectoryNavigator.VcsRootDirectory)
                            Directory.Delete(directory,true);
                    }

                    foreach (var indexRecord in newHeadCommitIndexRecords.Values)
                    {
                        var absolutePath = vcsRootDirectoryNavigator.RepositoryRootDirectory +
                                           Path.DirectorySeparatorChar + indexRecord.RelativePath;
                    
                        var directoryName = Path.GetDirectoryName(absolutePath);
                        if (directoryName != null)
                        {
                            Directory.CreateDirectory(directoryName);
                        }
                        using (var fs = File.Create(absolutePath)) {};
                        File.WriteAllBytes(absolutePath,_blobService.GetBlobData(indexRecord.BlobHash));
                    }
                }
                _branchService.SetActiveBranch(branchToSwitch);
                break;
            }
        }
    }
}