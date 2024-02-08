using YAVCS.Models;
using YAVCS.Services.Contracts;

namespace YAVCS.Services;

/*!
  \file   ConfigService.cs
  \brief  Service for manipulation with config file

  \author lepesh
  \date   01.02.2024
*/


public class ConfigService : IConfigService
{
    
    // Services 
    private readonly INavigatorService _navigatorService;

    public ConfigService(INavigatorService navigatorService)
    {
        _navigatorService = navigatorService;
    }


    public ConfigFileModel? TryGetConfigData()
    {
        // Get navigator
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        if (vcsRootDirectoryNavigator == null) return null;
        var configFileAbsolutePath = vcsRootDirectoryNavigator.ConfigFile;
        // check if file exists
        if (!File.Exists(configFileAbsolutePath)) return null;
        // read data from file
        var data = File.ReadAllLines(configFileAbsolutePath);
        // check the format
        if (data.Length != 3) return null;
        var userName = data[0];
        var userEmail = data[1];
        var success = DateTime.TryParse(data[2], out var createdAt);
        if (!success) return null;
        return new ConfigFileModel(userName, userEmail, createdAt);
    }

    public void ReWriteConfig(ConfigFileModel newConfigData)
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        var configFileAbsolutePath = vcsRootDirectoryNavigator?.ConfigFile;
        if (configFileAbsolutePath != null) 
            File.WriteAllText(configFileAbsolutePath, newConfigData.ToString());
    }
}