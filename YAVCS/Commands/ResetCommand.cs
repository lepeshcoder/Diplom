using YAVCS.Commands.Contracts;
using YAVCS.Exceptions;
using YAVCS.Services.Contracts;

namespace YAVCS.Commands;

public class ResetCommand : Command,ICommand
{

    private readonly INavigatorService _navigatorService;
    private readonly IBranchService _branchService;
    private readonly ICommitService _commitService;
    private readonly ITreeService _treeService;
    private readonly IIndexService _indexService;
    private readonly IBlobService _blobService;

    public ResetCommand(IBranchService branchService, INavigatorService navigatorService,
        ICommitService commitService, ITreeService treeService, IIndexService indexService, IBlobService blobService)
    {
        _branchService = branchService;
        _navigatorService = navigatorService;
        _commitService = commitService;
        _treeService = treeService;
        _indexService = indexService;
        _blobService = blobService;
    }

    private enum CommandCases
    {
        SyntaxError = 0,
        HelpCase = 1,
        SoftReset = 2,
        MixedReset = 3,
        HardReset = 4
    }
    
    protected override Enum GetCommandCase(string[] args)
    {
        return args switch
        {
            ["--help"] => CommandCases.HelpCase,
            ["--soft", ..] => CommandCases.SoftReset,
            ["--mixed", ..] => CommandCases.MixedReset,
            ["--hard",..] => CommandCases.HardReset,
            _ => CommandCases.SyntaxError
        };
    }

    public string Description => "";
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
                Console.WriteLine("Invalid args format");
                break;
            }
            case CommandCases.SoftReset:
            {
                var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
                if (vcsRootDirectoryNavigator == null)
                {
                    throw new RepositoryNotFoundException("not a part of repository");
                }
                var commitHash = args[1];
                var newHeadCommit = _commitService.GetCommitByHash(commitHash);
                if (newHeadCommit == null)
                {
                    throw new CommitNotFoundException(commitHash);
                }
                var activeBranch = _branchService.GetActiveBranch();
                var activeBranchName = activeBranch?.Name ?? "Master";
                _branchService.UpdateBranch(activeBranchName,newHeadCommit);
                break;
            }
            case CommandCases.MixedReset:
            {
                var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
                if (vcsRootDirectoryNavigator == null)
                {
                    throw new RepositoryNotFoundException("not a part of repository");
                }
                var commitHash = args[1];
                var newHeadCommit = _commitService.GetCommitByHash(commitHash);
                if (newHeadCommit == null)
                {
                    throw new CommitNotFoundException(commitHash);
                }
                var activeBranch = _branchService.GetActiveBranch();
                var activeBranchName = activeBranch?.Name ?? "Master";
                _branchService.UpdateBranch(activeBranchName,newHeadCommit);

                var newHeadCommitIndexRecords = _treeService.GetTreeRecordsByPath(newHeadCommit.TreeHash);
                _indexService.ClearIndex();
                foreach (var indexRecord in newHeadCommitIndexRecords.Values)
                {
                    _indexService.AddRecord(indexRecord);
                }
                _indexService.SaveChanges();
                break;
            }
            case CommandCases.HardReset:
            {
                var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
                if (vcsRootDirectoryNavigator == null)
                {
                    throw new RepositoryNotFoundException("not a part of repository");
                }
                var commitHash = args[1];
                var newHeadCommit = _commitService.GetCommitByHash(commitHash);
                if (newHeadCommit == null)
                {
                    throw new CommitNotFoundException(commitHash);
                }
                // update branch
                var activeBranch = _branchService.GetActiveBranch();
                var activeBranchName = activeBranch?.Name ?? "Master";
                _branchService.UpdateBranch(activeBranchName,newHeadCommit);

                // reset index
                var newHeadCommitIndexRecords = _treeService.GetTreeRecordsByPath(newHeadCommit.TreeHash);
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
                
                
                break;
            }
        }
    }
}