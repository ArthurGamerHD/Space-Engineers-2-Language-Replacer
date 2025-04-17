using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Logging;
using Avalonia.LogicalTree;
using Avalonia.Platform.Storage;

namespace SE2_Language_Replacer;

public partial class MainWindow : Window
{
    const string SE2Exe = "SpaceEngineers2.exe";

    string folderNamePattern = @"\b[a-z]{2}-[A-Z]{2}\b";

    private bool _isFilePickerOpen = false;

    public Dictionary<string, string> Guids = new Dictionary<string, string>();
    
    public string GamePath
    {
        get => GamePathBox.Text ?? string.Empty;
        set
        {
            GamePathBox.Text = value;
            ValidateGamePath();
        }
    }

    public string TranslationPath
    {
        get => TranslationPathBox.Text ?? string.Empty;
        set
        {
            TranslationPathBox.Text = value;
            ValidateTranslationPath();
        }
    }

    public string LocPath => Path.Combine(GamePath.Replace(SE2Exe, ""),
        "..\\GameData\\Vanilla\\Content\\MainMenuData\\Texts");
    
    public string LocDefPath => Path.Combine(LocPath, "en-US.def");
    
    public string CachePath => Path.Combine(GamePath.Replace(SE2Exe, ""),
        "..\\GameData\\Vanilla\\Content\\contentcache.json");
    
    public string CacheReferencePath => Path.Combine(GamePath.Replace(SE2Exe, ""),
        "..\\GameData\\Vanilla\\Content\\contentcache_reference.json");


    public MainWindow()
    {
        InitializeComponent();

        GamePath = $"C:\\Program Files (x86)\\Steam\\steamapps\\common\\SpaceEngineers2\\Game2\\{SE2Exe}";

        CheckIfHasAnyTranslationOnStartup();

        GamePathBox.TextChanged += (_, _) => ValidateGamePath();
        TranslationPathBox.TextChanged += (_, _) => ValidateTranslationPath();
    }

    private void CheckIfHasAnyTranslationOnStartup()
    {
        try
        {
            const string moreThanOneLocFile = "<more than one loc file>";

            string currentDirectory = Directory.GetCurrentDirectory();

            string[] zipFiles = Directory.GetFiles(currentDirectory, "*.zip");
            if (zipFiles.Length == 1)
                TranslationPath = zipFiles[0];

            string? hasLocTextsFile = null;

            foreach (string zipFile in zipFiles)
            {
                if (hasLocTextsFile == moreThanOneLocFile)
                    break;

                using ZipArchive zipArchive = ZipFile.OpenRead(zipFile);

                foreach (ZipArchiveEntry entry in zipArchive.Entries)
                {
                    if (entry.FullName.EndsWith(".loc-texts", StringComparison.OrdinalIgnoreCase))
                    {
                        if (hasLocTextsFile == null)
                            hasLocTextsFile = zipFile;
                        else
                            hasLocTextsFile = moreThanOneLocFile;

                        Console.WriteLine($"Found file: {entry.FullName}");
                        break;
                    }
                }
            }


            TranslationPath = hasLocTextsFile is null or moreThanOneLocFile ? "" : hasLocTextsFile;
        }
        catch
        {
            TranslationPath = "";
        }
    }

    private bool _translationPathValid = false;
    private bool _gamePathValid = false;

    private async void PickGameLocation(object? sender, RoutedEventArgs routedEventArgs)
    {
        GamePath = await PickFile("spaceengineers", SE2Exe) ?? GamePath;
    }

    private async void PickTranslationLocation(object? sender, RoutedEventArgs e)
    {
        TranslationPath = await PickFile("translation file", "*.zip") ?? TranslationPath;
    }

    public void ValidateGamePath()
    {
        if (File.Exists(GamePath) && Directory.Exists(LocPath))
        {
            _gamePathValid = true;
        }
        else
        {
            _gamePathValid = false;
        }

        InvalidGamePath.IsVisible = !_gamePathValid;
        InstallButton.IsEnabled = _gamePathValid && _translationPathValid;
    }


    public void ValidateTranslationPath()
    {
        _translationPathValid = File.Exists(TranslationPath) && FindFolderInZip(TranslationPath, folderNamePattern);

        InvalidTranslationPath.IsVisible = !_translationPathValid;
        InstallButton.IsEnabled = _gamePathValid && _translationPathValid;
    }

    private async Task<string?> PickFile(string fileName, string? pattern)
    {
        if (_isFilePickerOpen)
            return null;

        try
        {
            _isFilePickerOpen = true;

            return (await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
            {
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType(fileName)
                    {
                        Patterns = ["SpaceEngineers2.exe"]
                    }
                ]
            })).FirstOrDefault()?.TryGetLocalPath();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            _isFilePickerOpen = false;
        }
    }

    private void InstallButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_gamePathValid && _translationPathValid)
        {
            InstallTranslation();
        }
        else
        {
            Console.WriteLine("HOW DID YOU MANAGED THAT?");
        }
    }
    
    private void UninstallButton_OnClick(object? sender, RoutedEventArgs e)
    {
        UninstallTranslations();
    }

    private void UninstallTranslations()
    {
        
    }

    private void InstallTranslation()
    {
        ExtractFolderFromZip(TranslationPath, LocPath, ref Guids);
    }

    static bool FindFolderInZip(string zipFilePath, string folderNamePattern)
    {
        Regex regex = new Regex(folderNamePattern);

        var hasFiles = false;
        var hasFolder = false;

        using (ZipArchive archive = ZipFile.OpenRead(zipFilePath))
        {
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (entry.FullName.EndsWith("/") && regex.IsMatch(entry.FullName.TrimEnd('/')))
                {
                    hasFolder = true;
                    if (hasFiles && hasFolder)
                        break;
                }

                if (entry.FullName.EndsWith(".loc-texts", StringComparison.OrdinalIgnoreCase))
                {
                    hasFiles = true;
                    if (hasFiles && hasFolder)
                        break;
                }
            }
        }

        return hasFolder && hasFiles;
    }
    
    void ExtractFolderFromZip(string zipFilePath, string extractionPath, ref Dictionary<string,string> guids)
    {
        
        Regex regex = new Regex(folderNamePattern);

        using (ZipArchive archive = ZipFile.OpenRead(zipFilePath))
        {
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (entry.FullName.EndsWith("/") && regex.IsMatch(entry.FullName.TrimEnd('/')))
                {
                    guids.Clear();
                    string folderName = entry.FullName.TrimEnd('/');
                    
                    foreach (ZipArchiveEntry subEntry in archive.Entries)
                    {
                        if (subEntry.FullName.StartsWith(folderName + "/", StringComparison.OrdinalIgnoreCase) && !subEntry.FullName.EndsWith("/"))
                        {
                            string relativePath = subEntry.FullName.TrimEnd('/').Replace("/", "\\");
                            string destinationPath = Path.Combine(extractionPath, relativePath);
                            
                            if (Path.GetDirectoryName(destinationPath) is not { } path)
                                throw new DirectoryNotFoundException(destinationPath);
                            
                            Directory.CreateDirectory(path);
                            subEntry.ExtractToFile(destinationPath, true);

                            var guid = Guid.NewGuid().ToString();
                            
                            guids.Add(destinationPath, guid);
                        }
                    }

                    Console.WriteLine($"Folder '{folderName}' extracted to '{extractionPath}'.");
                    
                    GenerateDefinition(folderName, guids);
                }
            }
        }
    }
    
    void GenerateDefinition(string id, Dictionary<string, string> guids)
    {
       
        string jsonString = File.ReadAllText(LocDefPath);
        JsonNode? jsonNode = JsonNode.Parse(jsonString);

        if (jsonNode is null)
            throw new JsonException();
        
        jsonNode["$Value"]!["LocalizedName"] = "Custom Localization";
        var resources = jsonNode["$Value"]?["Resources"];

        foreach (var (path, uid) in guids)
        {
            resources?.AsArray().Add(new JsonObject
            {
                ["Key"] = Guid.NewGuid().ToString(),
                ["Value"] = new JsonObject
                {
                    ["ContentId"] = id + "_" + Path.GetFileNameWithoutExtension(path),
                    ["Asset"] = "{G}" + uid
                }
            });
        }

        var guid = Guid.NewGuid().ToString();
        
        string json = jsonNode.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        
        File.WriteAllText(Path.Combine(LocPath, id + ".def"), json);
        
        PopulateCache(id, guids, CachePath);
        PopulateCache(id, guids, CacheReferencePath);
    }
    
    public void PopulateCache(string id, Dictionary<string, string> guids, string cachePath)
    {
        JsonNode? jsonNode = JsonNode.Parse(File.ReadAllText(cachePath));
        
        if (jsonNode is null)
            throw new JsonException();
        
        var mappingArray = jsonNode["$Value"]?["Mapping"]?.AsArray();
        
        List<JsonObject> mappingsToAdd = new List<JsonObject>();
        
        foreach (var mapping in guids)
        {
            mappingsToAdd.Add( new JsonObject
            {
                ["ResourceHandle"] = $"{{G}}{mapping.Value}", // New resource handle
                ["FileHandle"] = mapping.Key.Split("..\\GameData\\Vanilla\\Content\\")[1] // New file handle
            });
        }
        
        var mappingsToRemove = new List<JsonNode>();

        if (mappingArray == null)
            throw new JsonException();

        foreach (var mapping in mappingArray)
        {
            if (mapping == null)
                continue;

            if (mapping["ResourceHandle"]?.ToString() == "{G}" + Constants.DefaultLocGUID)
                mapping["FileHandle"] = $"MainMenuData\\Texts\\{id}.def";

            if (mappingsToAdd.FirstOrDefault(a =>
                    a["FileHandle"]!.ToString().Equals(mapping["FileHandle"]?.ToString())) is { } match)
                mappingsToRemove.Add(mapping);
        }

        mappingsToRemove.ForEach(node => mappingArray.Remove(node));
        mappingsToAdd.ForEach(mappingArray.Add);

        if(!File.Exists(cachePath + ".bkp"))
            File.Copy(cachePath, cachePath + ".bkp");
        
        string newJson = jsonNode.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(cachePath, newJson);
    }
}