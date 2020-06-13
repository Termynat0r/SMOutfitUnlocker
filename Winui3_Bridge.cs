using Microsoft.WindowsAPICodePack.Dialogs;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
internal class FileHelper {
    public static string GenerateUniqueFilename(string fileName, string fullPath) {
        string uniqueName = fileName;
        int i = 0;
        while(File.Exists(Path.Combine(fullPath, uniqueName))) {
            uniqueName = Path.GetFileNameWithoutExtension(fileName) + " (" + (++i) + ")" + Path.GetExtension(fileName);
        }
        return uniqueName;
    }
}
public class StorageFile {
    private string fullPath;

    public string Name {
        get {
            return (new FileInfo(fullPath)).Name;
        }
    }
    public string Path {
        get {
            return (new FileInfo(fullPath)).Directory.FullName;
        }
    }

    private StorageFile(string file) {
        this.fullPath = (new FileInfo(file)).FullName;
    }

    public static Task<StorageFile> GetFileFromPathAsync(string file) {
        return Task.Run(() => {
            StorageFile sF = new StorageFile(file);
            if (File.Exists(sF.fullPath)) return sF;
            else return null;
        });
    }

    public Task RenameAsync(string newFileName, NameCollisionOption nameCollisionOption) {
        return Task.Run(() => {
            string newFileFullPath = System.IO.Path.Combine(this.Path, newFileName);
            bool exists = File.Exists(newFileFullPath);
            string uniqueName = FileHelper.GenerateUniqueFilename(newFileName, this.Path);
            switch (nameCollisionOption) {
                case NameCollisionOption.FailIfExists:
                    if (exists) return;
                    break;
                case NameCollisionOption.GenerateUniqueName:
                    if (exists) newFileFullPath = System.IO.Path.Combine(this.Path, uniqueName);
                    break;
                case NameCollisionOption.ReplaceExisting:
                    if (exists) File.Delete(newFileFullPath);
                    break;
            }

            File.Move(fullPath, newFileFullPath);
            this.fullPath = newFileFullPath;
        });
    }

    public class IRandomAccessStream {
        private readonly Stream stream;
        public IRandomAccessStream(Stream stream) {
            this.stream = stream;
        }
        public Stream AsStreamForRead() {
            return stream;
        }
    }


    public Task<IRandomAccessStream> OpenAsync(FileAccessMode fileAccessMode) {
        return Task.Run(() => {
            FileStream stream = null;
            switch (fileAccessMode) {
                case FileAccessMode.Read: stream = File.OpenRead(fullPath); break;
                case FileAccessMode.ReadWrite: stream = File.OpenWrite(fullPath); break;
            }
            return new IRandomAccessStream(stream);
        });
    }
}

public enum NameCollisionOption {
    FailIfExists,
    GenerateUniqueName,
    ReplaceExisting
}

public enum FileAccessMode {
    Read,
    ReadWrite
}

public class StorageFolder {
    private readonly string fullPath;

    public string Name {
        get {
            return (new DirectoryInfo(fullPath)).Name;
        }
    }

    private StorageFolder(string folder) {
        fullPath = (new DirectoryInfo(folder)).FullName;
    }

    public static Task<StorageFolder> GetFolderFromPathAsync(string path) {
        return Task.Run(() => {
            StorageFolder sF = new StorageFolder(path);
            if (Directory.Exists(sF.fullPath)) return sF;
            else return null;
        });
    }

    public Task<IReadOnlyList<StorageFolder>> GetFoldersAsync() {
        return Task.Run(() => {
            List<StorageFolder> folders = new List<StorageFolder>();
            Directory.GetDirectories(fullPath).ToList().ForEach(entry => folders.Add(new StorageFolder(entry)));
            return (IReadOnlyList<StorageFolder>) folders;
        });
    }

    public Task<StorageFile> GetFileAsync(string fileName) {
        return StorageFile.GetFileFromPathAsync(Path.Combine(fullPath, fileName));
    }
    
   

    public Task<StorageFile> CreateFileAsync(string fileName, CreationCollisionOption collisionOption) {
        return Task.Run(async () => {
            bool exists = File.Exists(Path.Combine(fullPath, fileName));
            string uniqueName = FileHelper.GenerateUniqueFilename(fileName, fullPath);
            string newFileFullPath = Path.Combine(fullPath, fileName); ;
            switch (collisionOption) {
                case CreationCollisionOption.FailIfExists:
                    if (exists) return null;
                    break;
                case CreationCollisionOption.GenerateUniqueName:
                    if (exists) newFileFullPath = Path.Combine(fullPath, uniqueName);
                    break;
                case CreationCollisionOption.OpenIfExists:
                    if (exists) return await StorageFile.GetFileFromPathAsync(Path.Combine(fullPath, fileName));
                    break;
                case CreationCollisionOption.ReplaceExisting:
                    //Do nothing
                    break;
            }

            if (newFileFullPath != null && newFileFullPath.Length > 0) File.Create(newFileFullPath).Close();
            return await StorageFile.GetFileFromPathAsync(newFileFullPath);
        });
    }
}

public enum CreationCollisionOption {
    FailIfExists,
    GenerateUniqueName,
    OpenIfExists,
    ReplaceExisting
}

public class FileIO {
    public static Task WriteBytesAsync(StorageFile file, byte[] buffer) {
        return Task.Run(() => {
            File.WriteAllBytes(Path.Combine(file.Path, file.Name), buffer);
        });
    }
}

public enum PickerLocationId {
    ComputerFolder
}

public class FolderPicker {
    public List<string> FileTypeFilter = new List<string>();
    public PickerLocationId SuggestedStartLocation;

    public Task<StorageFolder> PickSingleFolderAsync() {        
        CommonOpenFileDialog dialog = new CommonOpenFileDialog();
        //dialog.InitialDirectory = "C:\\Users";
        dialog.IsFolderPicker = true;
        var result = dialog.ShowDialog();
        return Task.Run(() => {
            if (result == CommonFileDialogResult.Ok) {
                Debug.WriteLine("Chosen " + dialog.FileName);
                return StorageFolder.GetFolderFromPathAsync(dialog.FileName);
            }
            return null;
        });
    }
}

public class MenuFlyoutItem : MenuItem {
    public object Text {
        get {
            return Header;
        }
        set {
            Header = value;
        }
    }

    public MenuFlyoutItem() {
        Padding = new Thickness(10);
        HorizontalAlignment = HorizontalAlignment.Right;
    }
}