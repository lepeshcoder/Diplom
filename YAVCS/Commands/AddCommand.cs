using YAVCS.Commands.Contracts;
using YAVCS.Models;
using YAVCS.Services.Contracts;

namespace YAVCS.Commands;

/*!
  \file   AddCommand.cs
  \brief  Add command logic

    Add - add file or directory to staging area(index)
    
  \author lepesh
  \date   03.02.2024
*/

public class AddCommand : Command,ICommand
{
    // services
    private readonly INavigatorService _navigatorService;
    private readonly IHashService _hashService;
    private readonly IBlobService _blobService;
    private readonly IIndexService _indexService;

    public AddCommand(INavigatorService navigatorService, IHashService hashService, IBlobService blobService, IIndexService indexService)
    {
        _navigatorService = navigatorService;
        _hashService = hashService;
        _blobService = blobService;
        _indexService = indexService;
    }

    private enum CommandCases
    {
        SyntaxError = 0,
        HelpCase = 1,
        AddAll = 2,
        AddItem = 3
    }
    
    public string Description => "add file or directory to staging area(index)";

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
                Console.WriteLine("Invalid args format");
                break;
            }
            case CommandCases.AddItem:
            {
                // check for repository exists
                var vcsRootDirectoryNavigator = _navigatorService.TryGetRepositoryRootDirectory();
                if(vcsRootDirectoryNavigator == null) Console.WriteLine("Repository doesn't exist");
                // check for item exists
                var itemRelativePath = args[0];
                var itemAbsolutePath = Environment.CurrentDirectory + '/' + itemRelativePath;
                if (File.Exists(itemAbsolutePath))
                {
                    StageFile(itemRelativePath);
                }
                else if (Directory.Exists(itemAbsolutePath))
                {
                    StageDirectory(itemRelativePath);
                }
                else Console.WriteLine($"{itemAbsolutePath} doesn't exist");
                break;
            }
            case CommandCases.AddAll:
            {
                //TODO:
                break;
            }
        }
    }

    protected override Enum GetCommandCase(string[] args)
    {
        return args switch
        {
            ["--help"] => CommandCases.HelpCase,
            ["--all"] or ["--a"] => CommandCases.AddAll,
            _ when args.Length == 1 => CommandCases.AddItem,
            _ => CommandCases.SyntaxError
        };
    }

    private void StageFile(string relativePath)
    {
        // read bytes and get new hash
        var currentDirectory = Environment.CurrentDirectory;
        var absolutePath = currentDirectory + '/' + relativePath;
        var byteData = File.ReadAllBytes(absolutePath);
        var newHash = _hashService.GetHash(byteData);
        var oldRecord = _indexService.TryGetRecordByPath(relativePath);
        // if record with the same path already exist
        if (oldRecord != null)
        {
            // if file isn't modify
            if (oldRecord.BlobHash == newHash)
            {
                throw new Exception("File is already staged and not modified");
            }
            else
            {
                // create new blob if it doesn't exist
                if (!_blobService.IsBlobExist(newHash))
                {
                    _blobService.CreateBlob(byteData);
                }
                // change blobHash in old record
                _indexService.DeleteRecord(oldRecord.RelativePath);
                _indexService.AddRecord(new IndexRecord(oldRecord.RelativePath,newHash));
            }
        }
        // if no record with the same path
        else
        {
            // create blob if it doesn't exist
            if (!_blobService.IsBlobExist(newHash))
            {
                _blobService.CreateBlob(byteData);
            }
            // add record to index
            _indexService.AddRecord(new IndexRecord(relativePath,newHash));
        }
    }

    private void StageDirectory(string relativePath)
    {
        throw new NotImplementedException();
    }

}