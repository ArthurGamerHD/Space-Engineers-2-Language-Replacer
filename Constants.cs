using System.IO;

namespace SE2_Language_Replacer;

public static class Constants
{

    public const string DefaultGamePath = "steamapps\\common\\SpaceEngineers2";
    
    public const string SteamLibraryFile = "libraryfolders.vdf";
    
    public const string SteamApps = "steamapps";
    
    public const string Se2Exe = "SpaceEngineers2.exe";
    
    public const string GameFolder = "Game2\\";
    
    public const string ContentFolder = "GameData\\Vanilla\\Content\\";
    
    public const string DefaultLocGuid = "df143f5b-70d0-4ccd-8da2-fc1336a83772";
    
    public const string FolderNamePattern = @"\b[a-z]{2}-[A-Z]{2}\b";
        
    public const string TextsFolder = "MainMenuData\\Texts\\";
    
    public const string DefaultLangFile = "en-US.def";
    
    public const string CacheFile = "contentcache.json";
    
    public const string CacheReferenceFile = "contentcache_reference.json";
    
    public const string LocalizationFileExtension = ".loc-texts";
    
    public const string LogFileName = ".\\last.log";
    
    public const string SteamRegistryPath32 = @"SOFTWARE\Valve\Steam";
    public const string SteamRegistryPath64 = @"SOFTWARE\Wow6432Node\Valve\Steam";
    
    public const string SteamRegistryInstall = "InstallPath";

    public const string SpaceEngineers2AppId = "1133870";

    public const string GithubLink = "https://github.com/ArthurGamerHD/Space-Engineers-2-Tradu--o-pt_BR/issues";
}