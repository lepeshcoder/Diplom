using YAVCS.Models;
using YAVCS.Services.Contracts;

namespace YAVCS.Services;

public class BranchService : IBranchService
{

    private readonly INavigatorService _navigatorService;

    public BranchService(INavigatorService navigatorService)
    {
        _navigatorService = navigatorService;
    }

    public BranchFileModel? GetActiveBranch()
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        var headFilePath = vcsRootDirectoryNavigator!.HeadFile;
        var activeBranchName = File.ReadAllText(headFilePath);
        var activeBranchFilePath =  vcsRootDirectoryNavigator.HeadsDirectory + Path.DirectorySeparatorChar + activeBranchName;
        var commitHash = File.ReadAllText(activeBranchFilePath);
        return commitHash == "ZeroCommit" ? null : new BranchFileModel(activeBranchName, commitHash);
    }

    public void SetActiveBranch(BranchFileModel branch)
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        var headFilePath = vcsRootDirectoryNavigator!.HeadFile;
        File.WriteAllText(headFilePath,branch.Name);
    }


    public void UpdateBranch(string name, CommitFileModel commit)
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        var branchFilePath = vcsRootDirectoryNavigator!.HeadsDirectory + Path.DirectorySeparatorChar + name;
        File.WriteAllText(branchFilePath,commit.Hash);
    }

    public void CreateBranch(BranchFileModel newBranch)
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        var branchFilePath = vcsRootDirectoryNavigator!.HeadsDirectory + Path.DirectorySeparatorChar + newBranch.Name;
        File.WriteAllText(branchFilePath,newBranch.ToString());
    }
}