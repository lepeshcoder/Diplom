using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using YAVCS.Commands;
using YAVCS.Commands.Contracts;
using YAVCS.Services;
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
                    Services.GetRequiredService<IConfigService>(),
                    Services.GetRequiredService<IBranchService>(),
                    Services.GetRequiredService<ICommitService>(),
                    Services.GetRequiredService<ITreeService>(),
                    Services.GetRequiredService<IHashService>())
            },
            {
                "add",
                new AddCommand(Services.GetRequiredService<INavigatorService>(),
                    Services.GetRequiredService<IHashService>(),
                    Services.GetRequiredService<IBlobService>(),
                    Services.GetRequiredService<IIndexService>(),
                    Services.GetRequiredService<IIgnoreService>())
            },
            {
                "status",
                 new StatusCommand(Services.GetRequiredService<INavigatorService>(),
                     Services.GetRequiredService<IIndexService>(),
                     Services.GetRequiredService<IHashService>(),
                     Services.GetRequiredService<ICommitService>(),
                     Services.GetRequiredService<IBranchService>())
            },
            {
                "unstage",
                new UnStageCommand(Services.GetRequiredService<INavigatorService>(),
                    Services.GetRequiredService<IIndexService>(),
                    Services.GetRequiredService<IBranchService>(),
                    Services.GetRequiredService<ITreeService>(),
                    Services.GetRequiredService<ICommitService>())
            },
            {
                "commit",
                new CommitCommand(Services.GetRequiredService<INavigatorService>(),
                    Services.GetRequiredService<ITreeService>(),
                    Services.GetRequiredService<ICommitService>(),
                    Services.GetRequiredService<IGarbageCollectorService>(),
                    Services.GetRequiredService<IBranchService>(),
                    Services.GetRequiredService<IMergeService>())
            },
            {
                "log",
                new LogCommand(Services.GetRequiredService<INavigatorService>(),
                    Services.GetRequiredService<IBranchService>(),
                    Services.GetRequiredService<ICommitService>())
            },
            {
                "restore",
                new RestoreCommand(Services.GetRequiredService<INavigatorService>(),
                    Services.GetRequiredService<IIndexService>(),
                    Services.GetRequiredService<IBlobService>(),
                    Services.GetRequiredService<IHashService>())
            },
            {
                "reset",
                new ResetCommand(Services.GetRequiredService<IBranchService>(),
                    Services.GetRequiredService<INavigatorService>(),
                    Services.GetRequiredService<ICommitService>(),
                    Services.GetRequiredService<ITreeService>(),
                    Services.GetRequiredService<IIndexService>(),
                    Services.GetRequiredService<IBlobService>())
            },
            {
                "branch",
                new BranchCommand(Services.GetRequiredService<IBranchService>(),
                    Services.GetRequiredService<INavigatorService>())
            },
            {
                "switch",
                new SwitchCommand(Services.GetRequiredService<INavigatorService>(),
                    Services.GetRequiredService<IBranchService>(),
                    Services.GetRequiredService<ITreeService>(),
                    Services.GetRequiredService<ICommitService>())
            },
            {
                "diff",
                new DiffCommand(Services.GetRequiredService<INavigatorService>(),
                    Services.GetRequiredService<ICommitService>(),
                    Services.GetRequiredService<ITreeService>(),
                    Services.GetRequiredService<IBranchService>(),
                    Services.GetRequiredService<IBlobService>())
            },
            {
                "merge",
                new MergeCommand(Services.GetRequiredService<INavigatorService>(),
                    Services.GetRequiredService<IBranchService>(),
                    Services.GetRequiredService<ITreeService>(),
                    Services.GetRequiredService<ICommitService>(),
                    Services.GetRequiredService<IMergeService>(),
                    Services.GetRequiredService<IBlobService>(),
                    Services.GetRequiredService<IHashService>())
            }
            
        };
    }

    public static void Run(string[] args)
    {
        var command = args[0];
        if(!_commands.ContainsKey(command)) Console.WriteLine("No such command");
        else
        {
            try
            {
                _commands[args[0]].Execute(args.Skip(1).ToArray());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}