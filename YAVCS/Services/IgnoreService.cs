using YAVCS.Services.Contracts;

namespace YAVCS.Services;

public class IgnoreService : IIgnoreService
{

    // Services
    private readonly INavigatorService _navigatorService;
    
    // Collection with fast crud for ignore rules
    // include .yavcs directory by default
    private HashSet<string> _ignoreRules = [".yavcs/\n"];

    public IgnoreService(INavigatorService navigatorService)
    {
        _navigatorService = navigatorService;
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        if (vcsRootDirectoryNavigator == null) return;
        var rules = File.ReadAllLines(vcsRootDirectoryNavigator.IgnoreFile);
        foreach (var rule in rules)
        {
            _ignoreRules.Add(rule);
        }
    }

    // Method that checks should item ignored
    // True - Ignore, False - Include
    public bool CheckIgnoreRules(string itemAbsolutePath)
    {
        throw new NotImplementedException();
    }
}