using System.Text.RegularExpressions;
using YAVCS.Services.Contracts;

namespace YAVCS.Services;

public class IgnoreService : IIgnoreService
{

    // Services
    private readonly INavigatorService _navigatorService;
    
    // Collections with fast crud for ignore rules
    // include .yavcs directory by default
    
    private readonly HashSet<string> _ignoreFileRules = [];
    private readonly HashSet<string> _ignoreDirectoryRules = [".yavcs"];
    private readonly HashSet<string> _ignoreExtensionRules = [];

    private const string DirectoryRulePattern = @"^(.+)/$";
    private const string ExtensionRulePattern = @"^\*(\..+)$";
    

    public IgnoreService(INavigatorService navigatorService)
    {
        _navigatorService = navigatorService;
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        if (vcsRootDirectoryNavigator == null) return;
        var rules = File.ReadAllLines(vcsRootDirectoryNavigator.IgnoreFile);
        foreach (var rule in rules)
        {
            var regex = new Regex(ExtensionRulePattern);
            var match = regex.Match(rule);
            if (match.Success)
            {
                _ignoreExtensionRules.Add(match.Groups[1].Value);
                continue;
            }

            regex = new Regex(DirectoryRulePattern);
            match = regex.Match(rule);
            if (match.Success)
            {
                _ignoreDirectoryRules.Add(match.Groups[1].Value);
                continue;
            }

            _ignoreFileRules.Add(rule);
        }
        
    }

    // Method that checks should item ignored
    // True - Ignore, False - Include
    public bool IsItemIgnored(string itemRelativePath)
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        var itemAbsolutePath = vcsRootDirectoryNavigator!.RepositoryRootDirectory + '\\' + itemRelativePath;
        
        if (Directory.Exists(itemAbsolutePath))
        {
            if (_ignoreDirectoryRules.Any(rule => rule.Equals(itemRelativePath)))
            {
                return true;
            }
        }
        // if item is a file
        else if(File.Exists(itemAbsolutePath))
        {
            var itemExtension = Path.GetExtension(itemRelativePath);
            //check file name rules
            if (_ignoreFileRules.Any(rule => rule.Equals(itemRelativePath )))
            {
                return true;
            }
            // check file extension rules
            if (_ignoreExtensionRules.Any(rule => rule.Equals(itemExtension)))
            {
                return true;
            }
        }
        
        return false;
    }
}