using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace App5
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BackupProgress : Page
    {
        Backup backup;
        BackupManager backupManager = new BackupManager();

        public BackupProgress()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            backup = Newtonsoft.Json.JsonConvert.DeserializeObject<Backup>(e.Parameter.ToString());

            ((App)App.Current).BackRequested += BackupProgress_BackRequested;
            backupManager.BackupProgress += BackupManager_BackupProgress;

            base.OnNavigatedTo(e);

            List<AppData> appDatas = (from CompactAppData c in backup.Apps
                                      select AppData.FindAppData(c.PackageId)).ToList();

            await backupManager.CreateBackup(appDatas, backup.Name);

            progressBar1.Value = 100.0;
            messageTextBlock.Text = "Backup completed.";
            HeaderText.Text = "DONE";
            progressRing.IsActive = false;
            progressRing.Visibility = Visibility.Collapsed;
            ((App)App.Current).BackRequested -= BackupProgress_BackRequested;
        }

        private DateTime lastUpdate = DateTime.MinValue;
        private void BackupManager_BackupProgress(object sender, BackupEventArgs e)
        {
            if ((e.State == BackupState.Compressing) && ((DateTime.Now - lastUpdate) < TimeSpan.FromMilliseconds(100)))
                return;

            messageTextBlock.Text = e.Message;
            message2TextBlock.Text = e.Message2;

            if (e.Progress < 0)
                progressBar1.IsIndeterminate = true;
            else {
                if (progressBar1.IsIndeterminate)
                    progressBar1.IsIndeterminate = false;
                progressBar1.Value = e.Progress;
            }
            
            if (e.Log.Count != LogsView.Items.Count)
            { //Refresh the list
                for (int i = LogsView.Items.Count; i < e.Log.Count; i++)
                    LogsView.Items.Add(e.Log[i]);
                LogsView.ScrollIntoView(LogsView.Items[LogsView.Items.Count - 1]);
            }

            lastUpdate = DateTime.Now;
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            ((App)App.Current).BackRequested -= BackupProgress_BackRequested;
            backupManager.BackupProgress -= BackupManager_BackupProgress;

            base.OnNavigatingFrom(e);
        }

        private void BackupProgress_BackRequested(object sender, Windows.UI.Core.BackRequestedEventArgs e)
        {
            e.Handled = true;
        }
    }
}
