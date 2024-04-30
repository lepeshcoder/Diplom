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

    public BranchFileModel GetActiveBranch()
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        var headFilePath = vcsRootDirectoryNavigator!.HeadFile;
        var activeBranchName = File.ReadAllText(headFilePath);
        var activeBranchFilePath =  vcsRootDirectoryNavigator.HeadsDirectory + Path.DirectorySeparatorChar + activeBranchName;
        var commitHash = File.ReadAllText(activeBranchFilePath);
        return new BranchFileModel(activeBranchName, commitHash);
    }

    public void SetActiveBranch(BranchFileModel branch)
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        var headFilePath = vcsRootDirectoryNavigator!.HeadFile;
        File.WriteAllText(headFilePath,branch.Name);
    }


    public void UpdateBranch(string name, string commitHash)
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        var branchFilePath = vcsRootDirectoryNavigator!.HeadsDirectory + Path.DirectorySeparatorChar + name;
        File.WriteAllText(branchFilePath,commitHash);
    }

    public void CreateBranch(BranchFileModel newBranch)
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        var branchFilePath = vcsRootDirectoryNavigator!.HeadsDirectory + Path.DirectorySeparatorChar + newBranch.Name;
        File.WriteAllText(branchFilePath,newBranch.ToString());
    }

    public List<BranchFileModel> GetAllBranches()
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        var branchFiles = Directory.GetFiles(vcsRootDirectoryNavigator!.HeadsDirectory);
        return branchFiles.Select(branchFile => new BranchFileModel(branchFile)).ToList();
    }

    public BranchFileModel? GetBranchByName(string name)
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        var branchFilePath = vcsRootDirectoryNavigator!.HeadsDirectory + Path.DirectorySeparatorChar + name;
        return !File.Exists(branchFilePath) ? null : new BranchFileModel(branchFilePath);
    }

    public void DeleteBranch(string name)
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        var branchFilePath = vcsRootDirectoryNavigator!.HeadsDirectory + Path.DirectorySeparatorChar + name;
        if (File.Exists(branchFilePath))
        {
            File.Delete(branchFilePath);
        }
    }

    public void SetDetachedHead(string hashCommit)
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        File.WriteAllText(vcsRootDirectoryNavigator!.DetachedHeadFile,hashCommit);
    }

    public string GetDetachedHeadCommitHash()
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        return File.ReadAllText(vcsRootDirectoryNavigator!.DetachedHeadFile);
    }

    public bool IsDetachedHead()
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        return File.ReadAllText(vcsRootDirectoryNavigator!.DetachedHeadFile).Length != 0;
    }

    public string GetHeadCommitHash()
    {
        return IsDetachedHead() 
            ? GetDetachedHeadCommitHash() 
            : GetActiveBranch().CommitHash;
    }
}