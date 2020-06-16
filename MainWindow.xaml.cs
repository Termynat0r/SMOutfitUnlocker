using ModernWpf.Controls;
using Nukepayload2.UI.Win32;
using Nukepayload2.UI.Xaml;
using SMOutfitUnlocker_WPF;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SMSaveOutfitUnlocker {

    public partial class MainWindow : Window {
        private IEnumerable<byte> uuidStorage;
        private StorageFolder saveFolder;

        public MainWindow() {
            AccentColorListener();

            InitializeComponent();

            StartWindowBlur();

            CreateBackupCheckBox.IsChecked = true;

            FillGamePathList();

            FillSaveGameList();
        }

        private void StartWindowBlur() {
            Timer timer = new Timer(1000);
            timer.Elapsed += (source, e) => {
                timer.Dispose();
                Dispatcher.Invoke(() => {
                    var wcF = new WindowCompositionFactory();
                    if (Win32ApiInformation.IsWindowAcrylicApiPresent()) {
                        var composition = wcF.TryCreateForCurrentView();
                        if (composition != null) {
                            //if (composition.TrySetAcrylic(this, true, Color.FromRgb(31, 31, 31))) {
                            //    Debug.WriteLine("Acrylic activated!");
                            //    Background = Brushes.Transparent;
                            //}
                            //else Debug.WriteLine("Acrylic failed!");
                            if (composition.TrySetBlur(this, true)) Debug.WriteLine("Blur activated!");
                            else Debug.WriteLine("Blur failed!");
                        } else Debug.WriteLine("Compositon is null!");
                    } else Debug.WriteLine("NoAcrylicAPI found!");
                });
            };
            timer.Start();
        }

        private void AccentColorListener() {
            //Do once
            var color = ModernWpf.ThemeManager.Current.AccentColor;
            ModernWpf.ThemeManager.Current.AccentColor = color;
            //Keep updating
            Timer colorChecker = new Timer(1000);
            colorChecker.Elapsed += (source, e) => {
                var newColor = GetAccentColor.AccentColor.Get();
                if (color != newColor) {
                    color = newColor;
                    Dispatcher.Invoke(() => {
                        ModernWpf.ThemeManager.Current.AccentColor = color;
                    });
                }
            };
            colorChecker.AutoReset = true;
            colorChecker.Start();
        }

        private async void FillGamePathList() {
            List<(string path, StorageFile customsFile)> gameInstallations = await GameDetector.TryFindGameFolders();
            foreach ((string path, StorageFile customsFile) in gameInstallations) {
                MenuFlyoutItem flyout = new MenuFlyoutItem {
                    Tag = customsFile,
                    Text = path
                };
                flyout.Click += ChooseGamePath_Click;
                GamePathListMenu.Items.Add(flyout);
            }

            //Manual Game Path Selection
            MenuFlyoutItem lastFlyout = new MenuFlyoutItem();
            lastFlyout.Text = "Choose other";
            lastFlyout.Click += ChooseGamePath_Click;
            GamePathListMenu.Items.Add(lastFlyout);
        }

        private async void ChooseGamePath_Click(object sender, RoutedEventArgs e) {
            MenuFlyoutItem sourceItem = (MenuFlyoutItem) sender;
            StorageFile customsFile = null;

            //Open Picker
            if (sourceItem.Tag == null) {
                FolderPicker fP = new FolderPicker();
                fP.FileTypeFilter.Add("*");
                fP.SuggestedStartLocation = PickerLocationId.ComputerFolder;

                do {
                    StorageFolder gameFolder = null;
                    try {
                        gameFolder = await fP.PickSingleFolderAsync();
                    }
                    catch (Exception) { };
                    if (gameFolder == null) break;
                    customsFile = await GameDetector.IsValidGameFolder(gameFolder);
                    if (customsFile == null) {
                        await ShowDialog("Error", "Game location could not be verified. Please choose again.");
                    }
                } while (customsFile == null);
            } else {
                customsFile = (StorageFile) sourceItem.Tag;
            }

            //Only open if selection valid
            if (customsFile != null) {
                string onError = null;
                (uuidStorage, onError) = await Deserializer.DeserializeCustomizationOptionsAsync(customsFile);
                if (uuidStorage == null || onError != null) {
                    await ShowDialog("Error", onError);
                    return;
                }

                //Enable Unlockbutton
                if (saveFolder != null && uuidStorage != null) UnlockButton.IsEnabled = true;
                GamePathButton.Content = "Extracted " + this.uuidStorage.Count() / 16 + " outfit options ✔️";
            }
        }

        private async void FillSaveGameList() {
            String path = Environment.ExpandEnvironmentVariables("%APPDATA%") + "\\Axolot Games\\Scrap Mechanic\\User\\";
            IReadOnlyList<StorageFolder> savegames = null;
            try {
                StorageFolder savefileParentfolder = await StorageFolder.GetFolderFromPathAsync(path);
                savegames = await savefileParentfolder.GetFoldersAsync();
            }
            catch (Exception e) {
                Debug.Print(e.StackTrace);
            }

            if (savegames == null || savegames.Count == 0) {
                SaveGameButton.Content = "No savegames found :(";
                SaveGameButton.IsEnabled = false;
            }

            foreach (StorageFolder savegame in savegames) {
                try {
                    if (!savegame.Name.StartsWith("User_")) throw new Exception();

                    //Successfull
                    MenuFlyoutItem flyout = new MenuFlyoutItem {
                        Tag = savegame,
                        Text = savegame.Name.Split("_")[1]
                    };
                    flyout.Click += SaveGameChosen_Click;
                    SaveGameListMenu.Items.Add(flyout);
                }
                catch (Exception) { }
            }
        }

        private void SaveGameChosen_Click(object sender, RoutedEventArgs e) {
            MenuFlyoutItem flyout = (MenuFlyoutItem) sender;
            saveFolder = (StorageFolder) flyout.Tag;
            SaveGameButton.Content = flyout.Text + " ✔️";

            //Enable Unlockbutton
            if (saveFolder != null && uuidStorage != null) UnlockButton.IsEnabled = true;
        }

        private async void StartUnlock_Click(object sender, RoutedEventArgs e) {
            UnlockButton.IsEnabled = false;
            CreateBackupCheckBox.IsEnabled = false;
            GamePathButton.IsEnabled = false;
            SaveGameButton.IsEnabled = false;

            // Create Backup
            if (CreateBackupCheckBox.IsChecked == true) {
                Debug.WriteLine("Creating backup file");
                StorageFile existingUnlockFile = await saveFolder.GetFileAsync("unlock");
                if (existingUnlockFile != null) await existingUnlockFile.RenameAsync("unlock_backup", NameCollisionOption.GenerateUniqueName);
            }

            //Generate Content
            UInt32 steam32ID = AppLogic.Steam64IDtoReversedSteam32(saveFolder.Name.Split("_")[1]);
            byte[] unlockFileContent = AppLogic.GenerateUnlockFileContent(steam32ID, uuidStorage);

            //Create file
            StorageFile newUnlockFile = await saveFolder.CreateFileAsync("unlock", CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteBytesAsync(newUnlockFile, unlockFileContent);

            await ShowDialog("Finished", "You can now start your game.");
        }

        private Task<ContentDialogResult> ShowDialog(string title, string content) {
            contentDialog.Title = title;
            contentDialog.Content = content;
            return contentDialog.ShowAsync();
        }
    }
}
