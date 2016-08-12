using LightBuzz.Archiver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace App5
{
    public enum BackupState
    {
        Initializing, Compressing, WritingMetadata, Finished
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

        protected virtual void OnBackupProgress(BackupEventArgs e)
        {
            if (BackupProgress != null)
                BackupProgress(this, e);
        }

        public static void loadCurrentBackups()
        {
            currentBackups = new List<Backup>();

            //TODO
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
            await FileOperations.CreateDirectory(backupPath);

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
            await CreateZip(sources, System.IO.Path.Combine(backupPath, "data.zip"), System.IO.Compression.CompressionLevel.NoCompression);
            
            OnBackupProgress(new BackupEventArgs(100.0, BackupState.WritingMetadata, "Creating metadata...", "", log));

            Backup currentBackup = new Backup(name);
            foreach (var item in apps)
            {
                currentBackup.Apps.Add(new CompactAppData(item));
            }

            await WriteMetaData(currentBackup, System.IO.Path.Combine(backupPath, "metadata.json"));

            OnBackupProgress(new BackupEventArgs(100.0, BackupState.Finished, "Finalizing...", totalFiles.ToString() + " / " + totalFiles.ToString(), log));
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

        public void Restore(Backup backup)
        {

        }
    }
}
