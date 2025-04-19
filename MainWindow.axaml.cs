using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Dialogs;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Logging;
using Avalonia.LogicalTree;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using SE2_Language_Replacer.Lib;

namespace SE2_Language_Replacer;

public partial class MainWindow : Window
{
    private bool _isFilePickerOpen;
    private Dictionary<string, string> _guids = new();

    static ILogger? _log;

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

    public MainWindow()
    {
        if (!Design.IsDesignMode)
            throw new InvalidOperationException("Empty Constructor should only be used in Design Mode.");

        InitializeComponent();
        InitializeContext();
    }


    private bool _translationPathValid;
    private bool _gamePathValid;

    public MainWindow(ILogger? log = null)
    {
        InitializeComponent();

#if DEBUG
        this.AttachDevTools();
#endif

        InitializeContext(log);
    }

    public void InitializeContext(ILogger? log = null)
    {
        _log = log;

        TryFindSpaceEngineers2Path();
        CheckIfHasAnyTranslationOnStartup();

        GamePathBox.TextChanged += (_, _) => ValidateGamePath();
        TranslationPathBox.TextChanged += (_, _) => ValidateTranslationPath();
    }

    private static string GetContentPath(string root) => Path.Combine(root, Constants.ContentFolder);
    private static string GetLocPath(string root) => Path.Combine(GetContentPath(root), Constants.TextsFolder);
    private static string GetLocDefPath(string root) => Path.Combine(GetLocPath(root), Constants.DefaultLangFile);

    private void CheckIfHasAnyTranslationOnStartup()
    {
        try
        {
            const string moreThanOneLocFile = "<more than one loc file>";

            string currentDirectory = Directory.GetCurrentDirectory();
            string[] zipFiles = Directory.GetFiles(currentDirectory, "*.zip");

            if (zipFiles.Length > 5)
                return; // to much zip files, let's not poke every single one of them searching for translation folders

            string? hasLocTextsFile = null;

            foreach (string zipFile in zipFiles)
            {
                if (hasLocTextsFile == moreThanOneLocFile)
                    break;

                using ZipArchive zipArchive = ZipFile.OpenRead(zipFile);

                foreach (ZipArchiveEntry entry in zipArchive.Entries)
                {
                    if (entry.FullName.EndsWith(Constants.LocalizationFileExtension,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        if (hasLocTextsFile == null)
                        {
                            hasLocTextsFile = zipFile;
                            _log?.LogInformation($"Found translation file: {Path.GetFileName(zipFile)}");
                        }

                        else // more than one file found, user must decide which one to use
                        {
                            hasLocTextsFile = moreThanOneLocFile;
                            _log?.LogWarning(
                                $"Found more than one translation file in current directory, aborting auto-fill");
                        }

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

    private void TryFindSpaceEngineers2Path()
    {
        string? steamPath = TryGetSteamFolder();

        if (steamPath is null)
            return;

        var lib = GetLibForAppId(Path.Combine(steamPath, Constants.SteamApps, Constants.SteamLibraryFile),
            Constants.SpaceEngineers2AppId);

        if (!string.IsNullOrEmpty(lib))
        {
            GamePath = Path.Combine(lib, Constants.DefaultGamePath);

            _log?.LogInformation("Game path found at \"{path}\\\"", GamePath);

            if (!_gamePathValid)
                _log?.LogWarning(
                    "Path does not appears to be a valid \"Space Engineers 2\" Installation. Manual intervention is required");
        }
    }

    private string? TryGetSteamFolder()
    {
        if (ReadRegistryValue(Constants.SteamRegistryPath32, Constants.SteamRegistryInstall) is {} installPath && !string.IsNullOrEmpty(installPath))
        {
            _log?.LogInformation($"32-bit Steam found at \"{installPath}\\\"");
            return installPath;
        }
        
        if (ReadRegistryValue(Constants.SteamRegistryPath64, Constants.SteamRegistryInstall) is {} installPath64 && !string.IsNullOrEmpty(installPath64))
        {
            _log?.LogInformation($"64-bit Steam found at\"{installPath64}\\\"");
            return installPath64;
        }

        _log?.LogWarning($"Steam can't be found");
        return null;
    }

    static string? GetLibForAppId(string library, string appId)
    {
        var content = File.ReadAllText(library);
        string libraryFolderPattern = @"\""(\d+)\""\s*{([^}]*)}";
        MatchCollection libraryMatches = Regex.Matches(content, libraryFolderPattern);
        foreach (Match libraryMatch in libraryMatches)
        {
            string libraryContent = libraryMatch.Groups[2].Value;
            if (libraryContent.Contains($"\"{appId}\""))
            {
                string pathPattern = @"\""path\""\s+\""(.+?)\""";
                Match pathMatch = Regex.Match(libraryContent, pathPattern);
                if (pathMatch.Success)
                {
                    var lib = pathMatch.Groups[1].Value.Replace("\\\\", "\\");
                    _log?.LogInformation("Game with AppID {appId} found at \"{path}\\\"", appId, lib);
                    return lib;
                }
            }
        }

        return null;
    }

    static string? ReadRegistryValue(string subKey, string valueName)
    {
        RegistryView[] registryViews = [RegistryView.Registry32, RegistryView.Registry64];

        try
        {
            foreach (var view in registryViews)
            {
                using RegistryKey key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                using RegistryKey? subKeyHandle = key.OpenSubKey(subKey);
                var value = subKeyHandle?.GetValue(valueName);
                if (value is string s)
                    return s;
            }
        }
        catch (Exception ex)
        {
            _log?.LogError($"Error reading registry value: {ex.Message}");
        }

        return null; // Neither 32 bits or 64 bits key were found
    }

    private async void PickGameLocation(object? sender, RoutedEventArgs routedEventArgs)
    {
        GamePath = await PickFile("spaceengineers", Constants.Se2Exe) is { } sePath
            ? sePath.Replace(Constants.GameFolder + Constants.Se2Exe, "")
            : GamePath;
    }

    private async void PickTranslationLocation(object? sender, RoutedEventArgs e)
    {
        TranslationPath = await PickFile(".zip", "*.zip") ?? TranslationPath;
    }

    public void ValidateGamePath()
    {
        if (File.Exists(Path.Combine(GamePath, "Game2\\" + Constants.Se2Exe)) && Directory.Exists(GetLocPath(GamePath)))
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
        _translationPathValid =
            File.Exists(TranslationPath) && FindFolderInZip(TranslationPath, Constants.FolderNamePattern);

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
            _log?.LogError(e.Message);
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
            var task = new Task(
                () =>
                {
                    Dispatcher.UIThread.Post(() => ProgressBar.IsVisible = true);

                    bool success = false;

                    try
                    {
                        InstallTranslation();
                        success = true;
                    }
                    catch (Exception exception)
                    {
                        _log?.LogError(exception.Message);
                    }

                    Dispatcher.UIThread.Post(() =>
                    {
                        ProgressBar.IsVisible = false;

                        if (success)
                        {
                            new SuccessDialog().ShowDialog(this);
                            _log?.LogInformation("Successfully installed localization.");
                        }

                        else
                        {
                            new FailDialog().ShowDialog(this);
                            _log?.LogError("Failure. Please send this file to {github}.", Constants.GithubLink);
                        }
                    });
                });

            task.Start();
        }
        else
        {
            _log?.LogCritical("HOW DID YOU MANAGED THAT?");
        }
    }

    private void UninstallButton_OnClick(object? sender, RoutedEventArgs e) => UninstallTranslations();

    private void UninstallTranslations()
    {
        /* eh, some time in the future, maybe */
    }

    private void InstallTranslation()
    {
        string translationPath = string.Empty;
        string gamePath = string.Empty;
        Dispatcher.UIThread.Invoke(() =>
        {
            translationPath = TranslationPath;
            gamePath = GamePath;
        });

        ExtractFolderFromZip(translationPath, gamePath, ref _guids);
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

    static void ExtractFolderFromZip(string zipFilePath, string gamePath, ref Dictionary<string, string> guids)
    {
        Regex regex = new Regex(Constants.FolderNamePattern);

        string extractionPath = GetLocPath(gamePath);

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
                        if (subEntry.FullName.StartsWith(folderName + "/", StringComparison.OrdinalIgnoreCase) &&
                            !subEntry.FullName.EndsWith("/"))
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

                    _log?.LogInformation($"Folder '{folderName}' extracted to '{extractionPath}'.");

                    GenerateDefinition(folderName, guids, gamePath);
                }
            }
        }
    }

    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    static void GenerateDefinition(string id, Dictionary<string, string> guids, string gamePath)
    {
        string jsonString = File.ReadAllText(GetLocDefPath(gamePath));
        JsonNode? jsonNode = JsonNode.Parse(jsonString);

        var contentPath = GetContentPath(gamePath);

        if (jsonNode is null)
            throw new JsonException();

        jsonNode["$Value"]!["LocalizedName"] = "Custom Localization";
        var resources = jsonNode["$Value"]?["Resources"];

        foreach (var (path, uid) in guids)
        {
            _log?.LogInformation($"Creating new entry for: {path[contentPath.Length..]} - {uid}");

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

#if ADD_AS_NEW // i'm limited by the technology of my time, this is not yet possible
        var guid = Guid.NewGuid().ToString();
#else
        var guid = "df143f5b-70d0-4ccd-8da2-fc1336a83772";
#endif
        jsonNode["$Value"]!["Guid"] = guid;

        string json = jsonNode.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

        File.WriteAllText(Path.Combine(GetLocPath(gamePath), id + ".def"), json);

        PopulateCache(id, guid, guids, gamePath, Constants.CacheFile);
        PopulateCache(id, guid, guids, gamePath, Constants.CacheReferenceFile);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
    public static void PopulateCache(string id, string guid, Dictionary<string, string> guids, string gamePath,
        string cacheFile)
    {
        string cachePath = Path.Combine(GetContentPath(gamePath), cacheFile);

        JsonNode? jsonNode = JsonNode.Parse(File.ReadAllText(cachePath));

        if (jsonNode is null)
            throw new JsonException();

        var mappingArray = jsonNode["$Value"]?["Mapping"]?.AsArray();

        List<JsonObject> mappingsToAdd = new List<JsonObject>();

#if ADD_AS_NEW
        /*mappingsToAdd.Add(new JsonObject
        {
            ["ResourceHandle"] = $"{{G}}{guid}",
            ["FileHandle"] = $"MainMenuData\\Texts\\{id}.def"
        });*/
#endif

        foreach (var mapping in guids)
        {
            var fileHandlerGuid = $"{{G}}{mapping.Value}";
            var fileHandlerPath = mapping.Key.Substring(GetContentPath(gamePath).Length);

            mappingsToAdd.Add(new JsonObject
            {
                ["ResourceHandle"] = fileHandlerGuid,
                ["FileHandle"] = fileHandlerPath
            });

            _log?.LogInformation($"Adding new entry on {cacheFile}: {fileHandlerPath} - {fileHandlerGuid}");
        }

#if ADD_AS_NEW
        var blob = jsonNode["$Value"]?["BlobData"]?[
            "VRage:Keen.VRage.Library.Filesystem.StorageManagers.Content.FileHashMetadataBlob"]?["$Value"]?.AsArray();

        blob?.Add(new JsonObject // fake data, i don't care about validation, just want the game to load it
        {
            ["$Key"] = $"{{G}}{guid}",
            ["$Value"] = new JsonObject
            {
                ["Xx64Hash"] = 3141592653589793238,
                ["FileLength"] = 1618
            }
        });
#endif

        var mappingsToRemove = new List<JsonNode>();

        if (mappingArray == null)
            throw new JsonException();

        foreach (var mapping in mappingArray)
        {
            if (mapping == null)
                continue;
#if !ADD_AS_NEW
            if (mapping["ResourceHandle"]?.ToString() == "{G}" + Constants.DefaultLocGuid)
                mapping["FileHandle"] = $"MainMenuData\\Texts\\{id}.def";
#endif

            if (mappingsToAdd.FirstOrDefault(a =>
                    a["FileHandle"]!.ToString().Equals(mapping["FileHandle"]?.ToString())) is { } match)
                mappingsToRemove.Add(mapping);
        }

        mappingsToRemove.ForEach(node => mappingArray.Remove(node));
        mappingsToAdd.ForEach(mappingArray.Add);

        if (!File.Exists(cachePath + ".bkp"))
            File.Copy(cachePath, cachePath + ".bkp");

        string newJson = jsonNode.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(cachePath, newJson);
    }

    private void Log_OnClick(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo(Constants.LogFileName) { UseShellExecute = true });
    }
}