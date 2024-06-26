﻿using YAVCS.Commands.Contracts;
using YAVCS.Services.Contracts;

namespace YAVCS.Commands;

public class RestoreCommand : Command,ICommand
{
    private readonly INavigatorService _navigatorService;
    private readonly IIndexService _indexService;
    private readonly IBlobService _blobService;
    private readonly IHashService _hashService;

    public RestoreCommand(INavigatorService navigatorService, IIndexService indexService,
        IBlobService blobService, IHashService hashService)
    {
        _navigatorService = navigatorService;
        _indexService = indexService;
        _blobService = blobService;
        _hashService = hashService;
    }

    protected override Enum GetCommandCase(string[] args)
    {
        return args switch
        {
            ["--help"] => CommandCases.HelpCase,
            _ when args.Length == 1 => CommandCases.DefaultCase,
            _ => CommandCases.SyntaxError
        };
    }
    
    private enum CommandCases
    {
        SyntaxError = 0,
        HelpCase = 1,
        DefaultCase = 2
    }

    public string Description => "Restore local changed\n" +
                                 "Fornat:\n" +
                                 "1) Restore local changes: yavcs restore Path\n";
    public void Execute(string[] args)
    {
        var commandCase = GetCommandCase(args);
        switch (commandCase)
        {
            case CommandCases.HelpCase:
            {
                Console.WriteLine(Description);
                break;
            }
            case CommandCases.SyntaxError:
            {
                throw new Exception("Invalid args format");
            }
            case CommandCases.DefaultCase:
            {
                var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
                if (vcsRootDirectoryNavigator == null)
                {
                    throw new Exception("not a part of repository");
                }
                var itemRelativePath = args[0];
                var itemAbsolutePath = Environment.CurrentDirectory + Path.DirectorySeparatorChar + itemRelativePath;
                
                RestoreFile(itemAbsolutePath);
                
                break;
            }
        }
    }


    private void RestoreFile(string fileAbsolutePath)
    {
        var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
        // try get index record with this path
        var fileRelativePath = Path.GetRelativePath(vcsRootDirectoryNavigator!.RepositoryRootDirectory, fileAbsolutePath);
        var indexRecord = _indexService.TryGetRecordByPath(fileRelativePath);
        // if file no track
        if (indexRecord == null)
        {
            throw new Exception($"item {fileRelativePath} not tracked");
        }

        if (File.Exists(fileAbsolutePath))
        {
            var data = File.ReadAllBytes(fileAbsolutePath);
            var newHash = _hashService.GetHash(data);
            if (newHash == indexRecord.BlobHash)
            {
                throw new Exception($"item {fileRelativePath} not modified");
            }
            var restoredData = _blobService.GetBlobData(indexRecord.BlobHash);
            File.WriteAllBytes(fileAbsolutePath,restoredData);
        }
        else
        {
            using (var fs = File.Create(fileAbsolutePath)) {}
            var restoredData = _blobService.GetBlobData(indexRecord.BlobHash);
            File.WriteAllBytes(fileAbsolutePath,restoredData);
        }
    }

}