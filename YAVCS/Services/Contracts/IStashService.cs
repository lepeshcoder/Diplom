using YAVCS.Models;

namespace YAVCS.Services.Contracts;

public interface IStashService
{
    // Creates a new stash commit in stashCommits directory and set it as a head 
    // writing it's hash in stash file
    void Push(StashCommitFileModel commit);

    // return head stash commit and rewrite head as a parent of this commit
    // and delete commit from stash commits directory
    StashCommitFileModel? Pop();

    // get all stash commits in Queue order
    IEnumerable<StashCommitFileModel> GetStashCommits();

    string GetStashHeadCommitHash();

    StashCommitFileModel? GetStashCommit(string hash);

    StashCommitFileModel? GetHeadStashCommit();

}