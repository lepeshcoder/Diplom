using Microsoft.Extensions.DependencyInjection;
using YAVCS.Commands;
using YAVCS.Commands.Contracts;
using YAVCS.Services.Contracts;

namespace YAVCS;
/*!
  \file   App.cs
  \brief  High-level class to run and configure the Application
    
  \author lepesh
  \date   02.01.2024
*/
public static class App
{
    private static Dictionary<string, ICommand> _commands = new();
    private static readonly IServiceProvider Services = DependencyInjectionConfig.Configure(); 
    
    public static void Configure()
    {
        _commands = new Dictionary<string, ICommand>
        {
            { 
                "init", 
                new InitCommand(Services.GetRequiredService<INavigatorService>(),
                                  Services.GetRequiredService<IConfigService>())
            }
        };
    }

    public static void Run(string[] args)
    {
        _commands[args[0]].Execute(args.Skip(1).ToArray());
    }
}