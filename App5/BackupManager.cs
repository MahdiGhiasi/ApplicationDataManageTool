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
        Initializing, CopyingFolders, CopyingFiles, Compressing, WritingMetadata
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
            totalProgressFactor = (1.0 / apps.Count) / 2.0;
            for (int i = 0; i < apps.Count; i++)
            {
                completedCount = i;

                appName = apps[i].DisplayName + ": ";

                FileOperations.FolderCopier copier = new FileOperations.FolderCopier(await StorageFolder.GetFolderFromPathAsync(apps[i].PackageDataFolder), await StorageFolder.GetFolderFromPathAsync(packagesBackupPath), apps[i].contents);

                copier.Copying += Copier_Copying;

                log.AddRange(await copier.Copy());

                copier.Copying -= Copier_Copying;
            }
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

        public void Restore(Backup backup)
        {

        }
    }
}
