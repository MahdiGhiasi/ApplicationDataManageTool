using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using LightBuzz.Archiver;
using SharpCompress.Archive.Zip;
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
        Initializing, CopyingFolders, CopyingFiles, Compressing, WritingMetadata, DeletingTemp, Finished
    }


    public class BackupEventArgs : EventArgs
    {
        public double Progress1 { get; set; }
        public double Progress2 { get; set; }
        public BackupState State { get; set; }
        public string Message { get; set; }
        public List<string> Log { get; set; }

        // Constructor. 
        public BackupEventArgs(double progress1, double progress2, BackupState state, string message, List<string> log)
        {
            Progress1 = progress1;
            Progress2 = progress2;
            State = state;
            Message = message;
            Log = log;
        }
    }

    public class BackupManager
    {
        private const double _CopyPercent = 0.45;
        private const double _CompressPercent = 0.5;
        private const double _DeletePercent = 0.04;
        private const double _WriteMetadataPercent = 0.01;

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

        private List<string> log;
        private double totalProgressFactor = 0;
        private string appName = "";
        private int completedCount = 0;
        public async Task CreateBackup(List<AppData> apps, string name)
        {
            log = new List<string>();
            string backupPath = System.IO.Path.Combine(App.BackupDestination, name);
            string packagesBackupPath = System.IO.Path.Combine(backupPath, "Packages");
            await FileOperations.CreateDirectory(backupPath);
            await FileOperations.CreateDirectory(packagesBackupPath);
            totalProgressFactor = (1.0 / apps.Count) * _CopyPercent;
            for (int i = 0; i < apps.Count; i++)
            {
                completedCount = i;

                appName = apps[i].DisplayName + ": ";

                FileOperations.FolderCopier copier = new FileOperations.FolderCopier(await StorageFolder.GetFolderFromPathAsync(apps[i].PackageDataFolder), await StorageFolder.GetFolderFromPathAsync(packagesBackupPath), apps[i].contents);

                copier.Copying += Copier_Copying;

                log.AddRange(await copier.Copy());

                copier.Copying -= Copier_Copying;
            }

            OnBackupProgress(new BackupEventArgs(-1, _CopyPercent * 100.0, BackupState.Compressing, "Creating archive...", log));
            StorageFolder packagesBackupFolder = await StorageFolder.GetFolderFromPathAsync(packagesBackupPath);
            await CreateZip(packagesBackupFolder, System.IO.Path.Combine(backupPath, "data.zip"), System.IO.Compression.CompressionLevel.Optimal);
            
            OnBackupProgress(new BackupEventArgs(-1, (_CopyPercent + _CompressPercent) * 100.0, BackupState.DeletingTemp, "Deleting Temporary data...", log));
            await packagesBackupFolder.DeleteAsync();

            OnBackupProgress(new BackupEventArgs(-1, (_CopyPercent + _CompressPercent + _DeletePercent) * 100.0, BackupState.WritingMetadata, "Creating metadata...", log));

            Backup currentBackup = new Backup(name);
            foreach (var item in apps)
            {
                currentBackup.Apps.Add(new CompactAppData(item));
            }

            await WriteMetaData(currentBackup, System.IO.Path.Combine(backupPath, "metadata.json"));

            OnBackupProgress(new BackupEventArgs(0, 100.0, BackupState.Finished, "Finalizing...", log));
        }

        private async Task WriteMetaData(Backup backup, string metadataFile)
        {
            string data = Newtonsoft.Json.JsonConvert.SerializeObject(backup, Newtonsoft.Json.Formatting.Indented);

            StorageFile metadata = await (await StorageFolder.GetFolderFromPathAsync(System.IO.Path.GetDirectoryName(metadataFile))).CreateFileAsync(System.IO.Path.GetFileName(metadataFile));

            await FileIO.WriteTextAsync(metadata, data);
        }

        private void Copier_Copying(object sender, FileOperations.CopyingEventArgs e)
        {
            List<string> curLog = new List<string>(log);
            curLog.AddRange(e.Log);
            if ((e.CurrentFiles + e.CurrentFolders + e.TotalFiles + e.TotalFolders) == 0)
            {
                OnBackupProgress(new BackupEventArgs(-1, Math.Min(Math.Max((100.0 * completedCount * totalProgressFactor), 0.0), 100.0), BackupState.Initializing, appName + "Finding files...", log));
            }
            else
            {
                string status = appName + "Copying files...";
                if (e.CurrentFiles == 0)
                    status = appName + "Creating folders...";

                double curProgress = 100.0 * ((double)e.CurrentFiles + (double)e.CurrentFolders) / (e.TotalFiles + e.TotalFolders);
                OnBackupProgress(new BackupEventArgs(Math.Min(Math.Max(curProgress, 0.0), 100.0),
                                                     Math.Min(Math.Max((100.0 * completedCount * totalProgressFactor) + curProgress * totalProgressFactor, 0.0), 100.0),
                                                     BackupState.CopyingFiles,
                                                     status,
                                                     curLog));
            }
        }
        

        public async Task CreateZip(StorageFolder backupFolder, string zipFileName, System.IO.Compression.CompressionLevel compressionLevel)
        {
            ArchiverPlus archiver = new ArchiverPlus();
            archiver.CompressingProgress += Archiver_CompressingProgress;

            await archiver.Compress(backupFolder, await (await StorageFolder.GetFolderFromPathAsync(System.IO.Path.GetDirectoryName(zipFileName))).CreateFileAsync(System.IO.Path.GetFileName(zipFileName)), compressionLevel);

            archiver.CompressingProgress -= Archiver_CompressingProgress;
        }

        private void Archiver_CompressingProgress(object sender, CompressingEventArgs e)
        {
            OnBackupProgress(new BackupEventArgs(e.Percent * 100.0, (_CopyPercent + e.Percent * _CompressPercent) * 100.0, BackupState.Compressing, "Creating archive...", log));
        }

        public void Restore(Backup backup)
        {

        }
    }
}
