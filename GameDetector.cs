using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

class GameDetector {
    private static List<(RegistryKey rootkey, string subkey)> registrySearchPaths = new List<(RegistryKey rootkey, string subkey)> {
            (Registry.CurrentUser, "System\\GameConfigStore\\Children\\"),
            (Registry.CurrentUser, "Software\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags\\Compatibility Assistant\\"),
            (Registry.CurrentUser, "Software\\Microsoft\\Windows\\ShellNoRoam\\MUICache\\"),
            (Registry.CurrentUser, "Software\\Classes\\Local Settings\\Software\\Microsoft\\Windows\\Shell\\MuiCache\\"),
            (Registry.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\SharedAccess\\Parameters\\FirewallPolicy\\FirewallRules\\")
        };

    public static async Task<List<(string path, StorageFile customsFile)>> TryFindGameFolders() {
        string searchString = "Release\\ScrapMechanic.exe";
        HashSet<string> smInstallations = new HashSet<string>();

        Action<string> OnSuccess = foundString => {
            string gameFolder = foundString.Split("Release")[0]; //Trim End
            gameFolder = gameFolder.Substring(gameFolder.LastIndexOf(":\\") - 1); //Trim Start
            smInstallations.Add(gameFolder);
        };

        registrySearchPaths.ForEach(path => SearchRegForString(path.rootkey, path.subkey, searchString, OnSuccess));

        List<(string path, StorageFile customsFile)> outputList = new List<(string path, StorageFile customsFile)>();
        Debug.WriteLine("Detected Installations:");
        foreach (string smInstallation in smInstallations) {
            Debug.WriteLine(smInstallation);
            StorageFile customsFile = await IsValidGameFolder(smInstallation);
            if (customsFile != null) outputList.Add((smInstallation, customsFile));
        }
        return outputList;
    }

    private static void SearchRegForString(RegistryKey rootKey, string regkey, string toSearch, Action<string> OnSuccess) {
        try {
            using (RegistryKey registryKey = rootKey.OpenSubKey(regkey)) {
                if (!(registryKey == null)) {
                    //Search in SubKeys
                    registryKey.GetSubKeyNames().ToList().ForEach(subkey => {
                        SearchRegForString(registryKey, subkey, toSearch, OnSuccess);
                    });
                    //Search in Names & Values
                    foreach (string valueName in registryKey.GetValueNames()) {
                        if (valueName.Contains(toSearch)) OnSuccess(valueName);
                        object value = registryKey.GetValue(valueName);
                        if (value != null && value.ToString().Contains(toSearch)) OnSuccess(value.ToString());
                    }
                }
            }
        }
        catch { }
    }
    public static async Task<StorageFile> IsValidGameFolder(String gameFolderString) {
        StorageFolder gameFolder = await StorageFolder.GetFolderFromPathAsync(gameFolderString);
        return await IsValidGameFolder(gameFolder);

    }

    public static async Task<StorageFile> IsValidGameFolder(StorageFolder gameFolder) {
        StorageFile customsFile = null;
        if (gameFolder == null) return null;
        try {
            customsFile = await gameFolder.GetFileAsync("Data\\Character\\customization_options.json");
        }
        catch (Exception) { }
        return customsFile;
    }
}

