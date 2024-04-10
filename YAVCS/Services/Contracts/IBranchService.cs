﻿using YAVCS.Models;

namespace YAVCS.Services.Contracts;

public interface IBranchService
{
    BranchFileModel? GetActiveBranch();

    void SetActiveBranch(BranchFileModel branch);
    
    void UpdateBranch(string name, CommitFileModel commit);

    void CreateBranch(BranchFileModel newBranch);

    List<BranchFileModel> GetAllBranches();

    BranchFileModel? GetBranchByName(string name);

    void DeleteBranch(string name);
}