using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace App5
{
    static class FileOperations
    {
        private static Dictionary<string, List<IStorageItem>> getContentsCache = new Dictionary<string, List<IStorageItem>>();

        public static async Task IsValidBackupName(string name)
        {
            var isValid = !string.IsNullOrEmpty(name) && name.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) < 0;
            if (!isValid)
                throw new Exception("The entered name contains illegal characters.");

            StorageFolder destPath = await GetFolder(App.BackupDestination);
            if (destPath == null)
            {
                await CreateDirectory(App.BackupDestination);
                destPath = await GetFolder(App.BackupDestination);
            }

            foreach (var item in await destPath.GetFoldersAsync())
            {
                if (item.Name == name)
                    throw new Exception("The name you've entered already exists. Please choose another name.");
            }
        }

        public static async Task<List<IStorageItem>> GetContents(StorageFolder folder)
        {
            if (getContentsCache.ContainsKey(folder.Path))
                return getContentsCache[folder.Path];

            List<IStorageItem> output = new List<IStorageItem>();
            foreach (var item in await folder.GetFilesAsync())
            {
                output.Add(item);
            }
            foreach (var item in await folder.GetFoldersAsync())
            {
                output.Add(item);
                output.AddRange(await GetContents(item));
            }

            try
            {
                getContentsCache.Add(folder.Path, output);
            }
            catch { } //Avoid any possible concurrency problems :P

            return output;
        }

        public static void RemoveFromGetContentsCache(string path)
        {
            path = path.ToLower();

            List<string> keysToBeRemoved = new List<string>();

            foreach (var item in getContentsCache.Keys)
            {
                if (item.ToLower().Contains(path))
                {
                    keysToBeRemoved.Add(item);
                }
            }

            foreach (var item in keysToBeRemoved)
            {
                getContentsCache.Remove(item);
            }
        }

        // Returns the number of files in this and all subdirectories
        public static async Task<int> FolderContentsCount(StorageFolder folder)
        {
            return (from IStorageItem s in await GetContents(folder)
                    where s is StorageFile
                    select s).Count();
            
        }

        public static string GetFileSizeString(double byteCount)
        {
            string size = "0 Bytes";
            if (byteCount >= 1073741824.0)
                size = String.Format("{0:##.##}", byteCount / 1073741824.0) + " GB";
            else if (byteCount >= 1048576.0)
                size = String.Format("{0:##.##}", byteCount / 1048576.0) + " MB";
            else if (byteCount >= 1024.0)
                size = String.Format("{0:##.##}", byteCount / 1024.0) + " KB";
            else if (byteCount > 0 && byteCount < 1024.0)
                size = byteCount.ToString() + " Bytes";

            return size;
        }

        internal static async Task<double> GetSizeOfFiles(List<StorageFile> files)
        {
            double size = 0;
            foreach (var item in files)
            {
                size += (await item.GetBasicPropertiesAsync()).Size;
            }
            return size;
        }

        /// <summary>
        /// Get's the StorageFolder of path. returns null if it doesn't exist.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static async Task<StorageFolder> GetFolder(string path)
        {
            StorageFolder folder = null;
            try
            {
                folder = await StorageFolder.GetFolderFromPathAsync(path);
            }
            catch
            {

            }

            return folder;
        }

        internal static async Task CreateDirectory(string path)
        {
            string curPath = path;
            List<string> directoriesToBeCreated = new List<string>();
            StorageFolder folder = null;
            while (true)
            {
                folder = await GetFolder(curPath);

                if (folder == null)
                {
                    directoriesToBeCreated.Add(System.IO.Path.GetFileName(curPath));
                    curPath = System.IO.Path.GetDirectoryName(curPath);
                }
                else
                {
                    break;
                }
            }

            for (int i = directoriesToBeCreated.Count - 1; i >= 0; i--)
            {
                folder = await folder.CreateFolderAsync(directoriesToBeCreated[i]);
            }
        }

        public class CopyingEventArgs : EventArgs
        {
            public int CurrentFiles { get; }
            public int TotalFiles { get; }
            public int CurrentFolders { get; }
            public int TotalFolders { get; }
            public List<string> Log { get; set; }

            // Constructor. 
            public CopyingEventArgs(int currentFiles, int totalFiles, int currentFolders, int totalFolders, List<string> log)
            {
                CurrentFiles = currentFiles;
                TotalFiles = totalFiles;
                CurrentFolders = currentFolders;
                TotalFolders = totalFolders;
                Log = log;
            }
        }

        public class FolderCopier
        {
            StorageFolder _source, _dest;
            List<IStorageItem> _items;


            public delegate void CopyingEventHandler(object sender, CopyingEventArgs e);
            public event CopyingEventHandler Copying;

            public FolderCopier(StorageFolder sourceFolder, StorageFolder destFolder, List<IStorageItem> items)
            {
                _source = sourceFolder;
                _dest = destFolder;
                _items = items;
            }

            protected virtual void OnCopying(CopyingEventArgs e)
            {
                if (Copying != null)
                    Copying(this, e);
            }


            public async Task<List<string>> Copy()
            {
                List<string> log = new List<string>();

                int srcAddressBeginIndex = _source.Path.LastIndexOf('\\') + 1;

                if (_items == null) //Needs loading
                {
                    OnCopying(new CopyingEventArgs(0, 0, 0, 0, log));
                    _items = await FileOperations.GetContents(_source);
                }

                try
                {
                    await _dest.CreateFolderAsync(_source.Path.Substring(srcAddressBeginIndex));
                }
                catch (Exception ex)
                {
                    throw new Exception("Cannot access disk, please change the backup location from settings and try again. (" + ex.Message + ")");
                }

                List<StorageFolder> folders = (from IStorageItem s in _items
                                               where s is StorageFolder
                                               select (StorageFolder)s).OrderBy(x => x.Path.Count(y => y == '\\')).ToList();
                int foldersCount = folders.Count;


                List<StorageFile> files = (from IStorageItem s in _items
                                           where s is StorageFile
                                           select (StorageFile)s).ToList();
                int filesCount = files.Count;

                int progress = 0;
                foreach (var item in folders)
                {
                    string destFolder = System.IO.Path.Combine(_dest.Path, item.Path.Substring(srcAddressBeginIndex));
                    string parentPath = System.IO.Path.GetDirectoryName(destFolder);
                    StorageFolder parent = await StorageFolder.GetFolderFromPathAsync(parentPath);
                    await parent.CreateFolderAsync(System.IO.Path.GetFileName(destFolder));
                    progress++;
                    OnCopying(new CopyingEventArgs(0, filesCount, progress, foldersCount, log));
                }

                progress = 0;
                foreach (var item in files)
                {
                    try
                    {
                        string destFile = System.IO.Path.Combine(_dest.Path, item.Path.Substring(srcAddressBeginIndex));
                        string parentPath = System.IO.Path.GetDirectoryName(destFile);
                        StorageFolder destFolder = await StorageFolder.GetFolderFromPathAsync(parentPath);
                        await item.CopyAsync(destFolder);
                        progress++;
                        OnCopying(new CopyingEventArgs(progress, filesCount, foldersCount, foldersCount, log));
                    }
                    catch (Exception ex)
                    {
                        log.Add("Can't copy " + item.Path.Substring(srcAddressBeginIndex) + ": " + ex.Message);
                    }
                }

                return log;
            }
        }
    }
}
