using LightBuzz.Archiver;
using MahdiGhiasi.AppListManager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System.Display;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace AppDataManageTool
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BackupProgress : Page
    {
        Backup backup;
        BackupManager backupManager = new BackupManager();
        ObservableCollection<ArchiverError> log = new ObservableCollection<ArchiverError>();
        DisplayRequest displayRequest;

        private int cleanedCount;
        private int totalAppsCount = 0;

        public BackupProgress()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            displayRequest = new DisplayRequest();
            displayRequest.RequestActive();

            var message = Newtonsoft.Json.JsonConvert.DeserializeObject<BackupProgressMessage>(e.Parameter.ToString());

            if (message.IsRestore)
            {
                HeaderText.Visibility = Visibility.Collapsed;
                HeaderText2.Visibility = Visibility.Visible;
                WarningMessage.Visibility = Visibility.Collapsed;
                WarningMessage2.Visibility = Visibility.Visible;

                List<CompactAppData> skipApps = new List<CompactAppData>();

                try
                {
                    backup = message.backup;

                    ((App)App.Current).BackRequested += BackupProgress_BackRequested;
                    backupManager.BackupProgress += BackupManager_BackupProgress;

                    LogsView.ItemsSource = log;

                    string notAvailableNames = "";
                    foreach (var item in backup.Apps)
                    {
                        if (LoadAppData.appsData.Count(x => x.FamilyName == item.FamilyName) == 0)
                        {
                            skipApps.Add(item);
                            if (notAvailableNames.Length > 0)
                                notAvailableNames += "\r\n";
                            notAvailableNames += item.DisplayName;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageDialog md = new MessageDialog("1" + ex.Message);
                    await md.ShowAsync();
                }

                try
                {
                    foreach (var item in backup.Apps)
                    {
                        if (!skipApps.Contains(item))
                        {
                            AppData appd = AppDataExtension.FindAppData(item.FamilyName);
                            if (appd.PackageId != item.PackageId)
                            {
                                MessageDialog md = new MessageDialog("Current installed version doesn't match the version backup was created from.\r\n\r\n" +
                                                                     "Current installed version: " + appd.PackageId + "\r\n\r\n" +
                                                                     "Backup: " + item.PackageId + "\r\n\r\n\r\n" +
                                                                     "Do you want to restore this app?",
                                                                     appd.DisplayName + ": Version mismatch");
                                md.Commands.Add(new UICommand("Restore") { Id = 1 });
                                md.Commands.Add(new UICommand("Don't restore") { Id = 0 });
                                md.DefaultCommandIndex = 1;
                                md.CancelCommandIndex = 0;

                                var result = await md.ShowAsync();

                                if (((int)result.Id) == 0)
                                {
                                    skipApps.Add(item);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageDialog md = new MessageDialog("2" + ex.Message);
                    await md.ShowAsync();
                }

                try
                {
                    cleanedCount = -1;
                    totalAppsCount = backup.Apps.Count;
                }
                catch (Exception ex)
                {
                    MessageDialog md = new MessageDialog("3" + ex.Message);
                    await md.ShowAsync();
                }

                await backupManager.Restore(backup, skipApps);

                progressBar1.Value = 100.0;
                messageTextBlock.Text = "Restore completed.";
                HeaderText2.Text = "DONE";
                WarningMessage2.Visibility = Visibility.Collapsed;
                FinalMessage.Visibility = Visibility.Visible;
                progressRing.IsActive = false;
                progressRing.Visibility = Visibility.Collapsed;
            }
            else
            {
                backup = message.backup;

                ((App)App.Current).BackRequested += BackupProgress_BackRequested;
                backupManager.BackupProgress += BackupManager_BackupProgress;

                LogsView.ItemsSource = log;

                List<AppData> appDatas = (from CompactAppData c in backup.Apps
                                          select AppDataExtension.FindAppData(c.FamilyName)).ToList();

                await backupManager.CreateBackup(appDatas, backup.Name);

                progressBar1.Value = 100.0;
                messageTextBlock.Text = "Backup completed.";
                HeaderText.Text = "DONE";
                WarningMessage.Visibility = Visibility.Collapsed;
                FinalMessage.Visibility = Visibility.Visible;
                progressRing.IsActive = false;
                progressRing.Visibility = Visibility.Collapsed;
            }

            ((App)App.Current).BackRequested -= BackupProgress_BackRequested;
            displayRequest.RequestRelease();
        }

        private DateTime lastUpdate = DateTime.MinValue;
        private void BackupManager_BackupProgress(object sender, BackupEventArgs e)
        {
            if ((e.State == BackupState.Compressing) && ((DateTime.Now - lastUpdate) < TimeSpan.FromMilliseconds(100)))
                return;
            if ((e.State == BackupState.ResettingAppData2) || (e.State == BackupState.Finalizing2))
            {
                progressBar1.IsIndeterminate = false;
                double progress = (e.Progress + (cleanedCount * 100.0)) / totalAppsCount;
                progress = Math.Min(Math.Max(progress, 0.0), 100.0);
                progressBar1.Value = progress;
                return;
            }

            messageTextBlock.Text = e.Message;
            message2TextBlock.Text = e.Message2;

            if (e.State == BackupState.ResettingAppData)
            {
                cleanedCount++;
                return;
            }

            if (e.Progress < 0)
                progressBar1.IsIndeterminate = true;
            else
            {
                if (progressBar1.IsIndeterminate)
                    progressBar1.IsIndeterminate = false;
                progressBar1.Value = e.Progress;
            }

            if ((e.Log != null) && (e.Log.Count != LogsView.Items.Count))
            { //Update the list
                for (int i = LogsView.Items.Count; i < e.Log.Count; i++)
                    log.Insert(0, e.Log[i]);

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

    class BackupProgressMessage
    {
        public Backup backup { get; set; }
        public bool IsRestore { get; set; }
    }
}
