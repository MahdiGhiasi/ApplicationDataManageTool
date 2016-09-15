using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
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
    public sealed partial class Settings : Page
    {
        public Settings()
        {
            this.InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            loadAppsEveryTime.IsOn = (bool)localSettings.Values["loadAppsEveryTime"];
            compressArchives.IsOn = (bool)localSettings.Values["allowCompress"];
            backupFolder.Text = (string)localSettings.Values["backupDest"];
        }

        private void compressArchives_Toggled(object sender, RoutedEventArgs e)
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            localSettings.Values["allowCompress"] = compressArchives.IsOn;
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

                await ReloadBackups();

                progress.Visibility = Visibility.Collapsed;
            }
        }

        private async System.Threading.Tasks.Task ReloadBackups()
        {
            BackupManager.BackupLoader bl = new BackupManager.BackupLoader();
            bl.LoadBackupsProgress += Bl_LoadBackupsProgress;
            await bl.LoadCurrentBackups();
            bl.LoadBackupsProgress -= Bl_LoadBackupsProgress;
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

        private async void Secret4_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if ((App.secretCodeCounter == 1200) || (App.secretCodeCounter == 1201) || (App.secretCodeCounter == 1202) || (App.secretCodeCounter == 1203))
                App.secretCodeCounter++;
            else if (App.secretCodeCounter == 1204)
            {
                App.hiddenMode = true;

                MessageDialog md = new MessageDialog("Hidden features enabled :)");
                await md.ShowAsync();
            }
            else
                App.secretCodeCounter = 0;

            System.Diagnostics.Debug.WriteLine("SECRET4");
        }

        private void loadAppsEveryTime_Toggled(object sender, RoutedEventArgs e)
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            localSettings.Values["loadAppsEveryTime"] = loadAppsEveryTime.IsOn;
        }

        private async void ReloadAppList_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (App.updateCacheInProgress)
            {
                MessageDialog md = new MessageDialog("Please wait until startup app list cache update is finished, and then try again.");
                await md.ShowAsync();
                return;
            }

            progress.Visibility = Visibility.Visible;

            progressStatus.Text = "Deleting icons cache...";

            await LoadAppData.DeleteAppListCache();

            var displayRequest = new DisplayRequest();
            displayRequest.RequestActive();
            ((App)App.Current).BackRequested += BlockBack;
            LoadAppData lad = new LoadAppData();
            lad.LoadingProgress += Lad_LoadingProgress;

            StorageFolder localCacheFolder = ApplicationData.Current.LocalCacheFolder;
            var logosFolder = await localCacheFolder.TryGetItemAsync("Logos");

            if ((logosFolder != null) && (logosFolder is StorageFolder))
            {
                await (logosFolder as StorageFolder).DeleteAsync(StorageDeleteOption.PermanentDelete);
            }

            App.appsData.Clear();
            App.familyNameAppData.Clear();

            await lad.LoadApps();

            try
            {
                await LoadAppData.SaveAppList();
            }
            catch (Exception ex)
            {
                MessageDialog md = new MessageDialog("Failed to save cache data. (" + ex.Message + ")");
                await md.ShowAsync();
            }

            await ReloadBackups();

            lad.LoadingProgress -= Lad_LoadingProgress;
            ((App)App.Current).BackRequested -= BlockBack;
            displayRequest.RequestRelease();


            progress.Visibility = Visibility.Collapsed;
        }

        private void Lad_LoadingProgress(object sender, LoadingEventArgs e)
        {
            int percent = (int)Math.Round((100.0 * e.Current) / e.Total);
            progressStatus.Text = "Loading apps " + percent.ToString() + "%";
        }

        private void BlockBack(object sender, Windows.UI.Core.BackRequestedEventArgs e)
        {
            e.Handled = true;
        }
    }
}
