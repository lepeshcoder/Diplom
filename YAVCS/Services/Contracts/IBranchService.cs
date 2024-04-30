using YAVCS.Models;

namespace YAVCS.Services.Contracts;

public interface IBranchService
{
    BranchFileModel GetActiveBranch();

    void SetActiveBranch(BranchFileModel branch);
    
    void UpdateBranch(string name, string commitHash);

    void CreateBranch(BranchFileModel newBranch);

    List<BranchFileModel> GetAllBranches();

    BranchFileModel? GetBranchByName(string name);

    void DeleteBranch(string name);

    void SetOrigHead(string hashCommit);

    string GetOrigHeadCommitHash();

    bool IsDetachedHead();
    string GetHeadCommitHash();

    void SetPreviousBranch(string branchName);
    string GetPreviousBranchName();

    void SetHead(string commitHash);
}