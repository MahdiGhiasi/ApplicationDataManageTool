using LightBuzz.Archiver;
using MahdiGhiasi.AppListManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace AppDataManageTool
{
    public enum BackupState
    {
        Initializing, Compressing, WritingMetadata, Finished,
        ResettingAppData, Decompressing
    }


    public class BackupEventArgs : EventArgs
    {
        public double Progress { get; set; }
        public BackupState State { get; set; }
        public string Message { get; set; }
        public string Message2 { get; set; }
        public List<ArchiverError> Log { get; set; }

        // Constructor. 
        public BackupEventArgs(double progress, BackupState state, string message, string message2, List<ArchiverError> log)
        {
            Progress = progress;
            State = state;
            Message = message;
            Message2 = message2;
            Log = log;
        }
    }

    public class BackupManager
    {
        public delegate void BackupEventHandler(object sender, BackupEventArgs e);
        public event BackupEventHandler BackupProgress;

        public static List<Backup> currentBackups;

        public static readonly string[] deletableFolders = new string[] { "AC", "AppData", "LocalCache", "LocalState", "RoamingState", "Settings", "SystemAppData", "TempState" };

        protected virtual void OnBackupProgress(BackupEventArgs e)
        {
            if (BackupProgress != null)
                BackupProgress(this, e);
        }

        public class BackupLoader
        {
            public delegate void LoadBackupsEventHandler(object sender, LoadingEventArgs e);
            public event LoadBackupsEventHandler LoadBackupsProgress;

            protected virtual void OnBackupProgress(LoadingEventArgs e)
            {
                if (LoadBackupsProgress != null)
                    LoadBackupsProgress(this, e);
            }

            public async Task LoadCurrentBackups()
            {
                currentBackups = new List<Backup>();

                StorageFolder backupLocation;

                try {
                     backupLocation = await StorageFolder.GetFolderFromPathAsync(App.BackupDestination);
                }
                catch
                {
                    return;
                }

                var backups = await backupLocation.GetFoldersAsync();

                for (int i = 0; i < backups.Count; i++)
                {
                    OnBackupProgress(new LoadingEventArgs(i, backups.Count));

                    StorageFolder folder = backups[i];
                    StorageFile metadata = await folder.TryGetItemAsync("metadata.json") as StorageFile;
                    StorageFile data = await folder.TryGetItemAsync("data.zip") as StorageFile;

                    if ((metadata != null) && (data != null)) //It's a valid backup.
                    {
                        string metadataText = await FileIO.ReadTextAsync(metadata);

                        Backup b = Newtonsoft.Json.JsonConvert.DeserializeObject<Backup>(metadataText);
                        currentBackups.Add(b);
                    }
                }
                OnBackupProgress(new LoadingEventArgs(backups.Count, backups.Count));
            }
        }

        public static string GenerateBackupName()
        {
            return "Backup " + DateTime.Now.ToString("yyyy-dd-M  HH-mm-ss-fff");
        }

        public async Task CreateBackup(AppData app, string name)
        {
            List<AppData> l = new List<AppData>();
            l.Add(app);

            await CreateBackup(l, name);
        }

        private List<ArchiverError> log;
        private Dictionary<string, string> familyToDisplayNames;
        int totalFiles = 1;
        public async Task CreateBackup(List<AppData> apps, string name)
        {
            log = new List<ArchiverError>();
            familyToDisplayNames = new Dictionary<string, string>();
            string backupPath = System.IO.Path.Combine(App.BackupDestination, name);
            string packagesBackupPath = System.IO.Path.Combine(backupPath, "Packages");
            await FileOperations.CreateDirectoryIfNotExists(backupPath);

            List<StorageFolder> sources = new List<StorageFolder>();

            totalFiles = 0;
            for (int i = 0; i < apps.Count; i++)
            {
                var item = apps[i];
                StorageFolder fol = await StorageFolder.GetFolderFromPathAsync(item.PackageDataFolder);

                OnBackupProgress(new BackupEventArgs(-1, BackupState.Initializing, item.DisplayName + ": Looking for files...", apps.Count == 1 ? "" : ((i + 1).ToString() + " / " + apps.Count), log));
                totalFiles += await FileOperations.FolderContentsCount(fol);

                sources.Add(fol);

                familyToDisplayNames.Add(item.FamilyName, item.DisplayName);
            }

            OnBackupProgress(new BackupEventArgs(0, BackupState.Compressing, "Copying...", "", log));
            await CreateZip(sources, System.IO.Path.Combine(backupPath, "data.zip"), App.AllowCompress ? System.IO.Compression.CompressionLevel.Optimal : System.IO.Compression.CompressionLevel.NoCompression);
            
            OnBackupProgress(new BackupEventArgs(100.0, BackupState.WritingMetadata, "Creating metadata...", "", log));

            Backup currentBackup = new Backup(name, Backup.GenerateAppSubtitle(apps));
            currentBackup.SetDeviceInfo();
            foreach (var item in apps)
            {
                currentBackup.Apps.Add(new CompactAppData(item));
            }

            await WriteMetaData(currentBackup, System.IO.Path.Combine(backupPath, "metadata.json"));

            OnBackupProgress(new BackupEventArgs(100.0, BackupState.Finished, "Finalizing...", totalFiles.ToString() + " / " + totalFiles.ToString(), log));

            currentBackups.Add(currentBackup);
        }

        private async Task WriteMetaData(Backup backup, string metadataFile)
        {
            string data = Newtonsoft.Json.JsonConvert.SerializeObject(backup, Newtonsoft.Json.Formatting.Indented);

            StorageFile metadata = await (await StorageFolder.GetFolderFromPathAsync(System.IO.Path.GetDirectoryName(metadataFile))).CreateFileAsync(System.IO.Path.GetFileName(metadataFile));

            await FileIO.WriteTextAsync(metadata, data);
        }

        public async Task CreateZip(List<StorageFolder> folders, string zipFileName, System.IO.Compression.CompressionLevel compressionLevel)
        {
            ArchiverPlus archiver = new ArchiverPlus();
            archiver.CompressingProgress += Archiver_CompressingProgress;

            await archiver.Compress(folders, await (await StorageFolder.GetFolderFromPathAsync(System.IO.Path.GetDirectoryName(zipFileName))).CreateFileAsync(System.IO.Path.GetFileName(zipFileName)), compressionLevel);

            archiver.CompressingProgress -= Archiver_CompressingProgress;
        }

        private void Archiver_CompressingProgress(object sender, CompressingEventArgs e)
        {
            double percent = (100.0 * e.ProcessedFilesCount) / totalFiles;
            string status = "Copying...";
            if (familyToDisplayNames.ContainsKey(e.CurrentRootFolder))
                status = familyToDisplayNames[e.CurrentRootFolder] + ": Copying...";
            OnBackupProgress(new BackupEventArgs(percent, BackupState.Compressing, status, e.ProcessedFilesCount.ToString() + " / " + totalFiles.ToString() , e.Log));
            log = e.Log;
        }

        public async Task ResetAppData(AppData app)
        {
            StorageFolder dataFolder = await StorageFolder.GetFolderFromPathAsync(app.PackageDataFolder);
            List<StorageFile> files = (from IStorageItem s in (await FileOperations.GetContents(dataFolder))
                                        where s is StorageFile
                                        select (StorageFile)s).ToList();

            int count = files.Count;
            int current = 0;
            foreach (var item in files)
            {
                try
                {
                    string relativePath = item.Path.Substring(app.PackageDataFolder.Length + 1).Replace('/', '\\');
                    if (!relativePath.Contains("\\"))
                        continue;

                    relativePath = relativePath.Substring(0, relativePath.IndexOf("\\"));

                    if (deletableFolders.Contains(relativePath))
                        await item.DeleteAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message + " :: " + item.Path);
                }
                current++;
                OnBackupProgress(new BackupEventArgs(((double)current) / count, BackupState.ResettingAppData, current.ToString() + " / " + count.ToString(), "", null));
            }

            OnBackupProgress(new BackupEventArgs(100.0, BackupState.Finished, current.ToString() + " / " + count.ToString(), "", null));
            App.GetAppDataEx(app).ResetSizeData();
        }

        List<ArchiverError> restoreLog = new List<ArchiverError>();

        public async Task Restore(Backup backup, List<CompactAppData> skipApps)
        {
            int counter = 1;
            foreach (var item in backup.Apps)
            {
                if (!skipApps.Contains(item))
                {
                    OnBackupProgress(new BackupEventArgs(-1, BackupState.ResettingAppData, "Clearing current state of " + item.DisplayName, counter.ToString() + " / " + (backup.Apps.Count - skipApps.Count).ToString(), restoreLog));
                    await ResetAppData(AppDataExtension.GetAppDataFromCompactAppData(item));
                    counter++;
                }
            }
            OnBackupProgress(new BackupEventArgs(-1, BackupState.Initializing, "Loading backup file...", "", restoreLog));
            StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(System.IO.Path.Combine(App.BackupDestination, backup.Name));
            StorageFile file = await folder.GetFileAsync("data.zip");
            ArchiverPlus archiver = new ArchiverPlus();


            Dictionary<string, StorageFolder> dests = new Dictionary<string, StorageFolder>();
            familyToDisplayNames = new Dictionary<string, string>();

            foreach (var item in backup.Apps)
            {
                if (!skipApps.Contains(item))
                {
                    FileOperations.RemoveFromGetContentsCache(item.FamilyName);

                    dests[item.FamilyName] = await StorageFolder.GetFolderFromPathAsync(System.IO.Path.GetDirectoryName(await LoadAppData.GetDataFolder(AppDataExtension.GetAppDataFromCompactAppData(item))));

                    familyToDisplayNames.Add(item.FamilyName, item.DisplayName);
                }
                else
                {
                    dests[item.FamilyName] = null; //Skip
                }
            }

            archiver.DecompressingProgress += Archiver_DecompressingProgress;

            await archiver.DecompressSpecial(file, dests);

            archiver.DecompressingProgress -= Archiver_DecompressingProgress;

            OnBackupProgress(new BackupEventArgs(100.0, BackupState.Finished, "Restore completed.", "", restoreLog));
        }

        private void Archiver_DecompressingProgress(object sender, DecompressingEventArgs e)
        {
            string status = "Restoring...";
            if (familyToDisplayNames.ContainsKey(e.CurrentRootFolder))
                status = familyToDisplayNames[e.CurrentRootFolder] + ": Restoring...";
            OnBackupProgress(new BackupEventArgs(e.Percent, BackupState.ResettingAppData,status, e.ProcessedEntries.ToString() + " / " + e.TotalEntries, e.Log));
            restoreLog = e.Log;
        }


        internal static async Task DeleteBackup(Backup currentBackup)
        {
            await (await StorageFolder.GetFolderFromPathAsync(System.IO.Path.Combine(App.BackupDestination, currentBackup.Name))).DeleteAsync();
            currentBackups.Remove(currentBackup);
        }
    }
}
