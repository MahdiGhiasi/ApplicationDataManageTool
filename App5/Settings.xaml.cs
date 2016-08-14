using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
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
    public sealed partial class Settings : Page
    {
        public Settings()
        {
            this.InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            compressArchives.IsChecked = (bool)localSettings.Values["allowCompress"];
            backupFolder.Text = (string)localSettings.Values["backupDest"];
        }

        private void compressArchives_CheckedChanged(object sender, RoutedEventArgs e)
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            localSettings.Values["allowCompress"] = compressArchives.IsChecked;
        }

        private async void PickBackupFolder_Click(object sender, RoutedEventArgs e)
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            FolderPicker fp = new FolderPicker();
            StorageFolder folder = await fp.PickSingleFolderAsync();

            if (folder != null)
            {
                progress.Visibility = Visibility.Visible;

                backupFolder.Text = folder.Path;
                localSettings.Values["backupDest"] = folder.Path;
                App.BackupDestination = folder.Path;

                BackupManager.BackupLoader bl = new BackupManager.BackupLoader();
                bl.LoadBackupsProgress += Bl_LoadBackupsProgress;
                await bl.LoadCurrentBackups();
                bl.LoadBackupsProgress -= Bl_LoadBackupsProgress;

                progress.Visibility = Visibility.Collapsed;
            }
        }

        private void Bl_LoadBackupsProgress(object sender, LoadingEventArgs e)
        {
            if (e.Current == 0)
            {
                progressStatus.Text = "Loading backups...";
            }
            else
            {
                int percent = (int)Math.Round((100.0 * e.Current) / e.Total);
                progressStatus.Text = "Loading backups " + percent.ToString() + "%";
            }
        }
    }
}
