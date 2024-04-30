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
    
    public void SetOrigHead(string hashCommit)
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        File.WriteAllText(vcsRootDirectoryNavigator!.OrigHeadFile,hashCommit);
    }

    public string GetOrigHeadCommitHash()
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        return File.ReadAllText(vcsRootDirectoryNavigator!.OrigHeadFile);
    }

    public bool IsDetachedHead()
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        return File.ReadAllText(vcsRootDirectoryNavigator!.OrigHeadFile).Length != 0;
    }

    public string GetHeadCommitHash()
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        return IsDetachedHead() 
            ? File.ReadAllText(vcsRootDirectoryNavigator!.HeadFile)
            : GetActiveBranch().CommitHash;
    }

    public void SetPreviousBranch(string branchName)
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        File.WriteAllText(vcsRootDirectoryNavigator!.PreviousBranchFile,branchName);
    }

    public string GetPreviousBranchName()
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        return File.ReadAllText(vcsRootDirectoryNavigator!.PreviousBranchFile);
    }

    public void SetHead(string commitHash)
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        File.WriteAllText(vcsRootDirectoryNavigator!.HeadFile,commitHash);
    }
}