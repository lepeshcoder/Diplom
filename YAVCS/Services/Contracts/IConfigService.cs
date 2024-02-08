using YAVCS.Models;

namespace YAVCS.Services.Contracts;

/*!
  \file   IConfigService.cs
  \brief  Interface for Config service

    Service for manipulation with config File
    
  \author lepesh
  \date   01.02.2024
*/


public interface IConfigService
{
    // Try get data from config file, if the format is invalid return null 
    ConfigFileModel? TryGetConfigData();

    // Write new config data to config file
    void ReWriteConfig(ConfigFileModel newConfigData);
}