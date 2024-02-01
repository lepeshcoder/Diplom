namespace YAVCS.Models;

/*!
  \file   ConfigFileModel.cs
  \brief  Config File Model

    Config file used to user specific information and secure goals
 
  \author lepesh
  \date   01.02.2024
*/


public class ConfigFileModel
{
    // user nick
    private string _userName;

    // user email
    private string _userEmail;

    // time when repository created (init command performed)
    private DateTime _createdAt;

    public ConfigFileModel(string userName = "user", string userEmail = "email", DateTime createdAt = new())
    {
        _userName = userName;
        _userEmail = userEmail;
        _createdAt = createdAt;
    }

    public override string ToString()
    {
        return _userName + '\n' + _userEmail + '\n' + _createdAt;
    }
}