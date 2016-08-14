using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace App5
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        Dictionary<string, AppData> AppNames = new Dictionary<string, AppData>();
        LoadAppData loadAppData = new LoadAppData();
        BackupManager.BackupLoader backupLoader = new BackupManager.BackupLoader();

        public MainPage()
        {
            this.InitializeComponent();

            loadAppData.LoadingProgress += LoadAppData_LoadingProgress;
            backupLoader.LoadBackupsProgress += BackupLoader_LoadBackupsProgress;
        }

        private void BackupLoader_LoadBackupsProgress(object sender, LoadingEventArgs e)
        {
            int percent = (int)Math.Round((100.0 * e.Current) / e.Total);
            progressStatus.Text = "Loading current backups " + percent.ToString() + "%";
        }

        private void LoadAppData_LoadingProgress(object sender, LoadingEventArgs e)
        {
            int percent = (int)Math.Round((100.0 * e.Current) / e.Total);
            progressStatus.Text = "Loading apps " + percent.ToString() + "%";
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (App.appsData == null)
            {
                progress.Visibility = Visibility.Visible;
                progressRing.IsActive = true;

                App.appsData = await loadAppData.LoadApps();

                await backupLoader.LoadCurrentBackups();

                progress.Visibility = Visibility.Collapsed;
                progressRing.IsActive = false;

                Frame.Background = Header.Background;
            }

            AppDataView.PageStatus_CurrentApp = null;
            AppDataView.PageStatus_IsShowingDetails = false;
        }

        private void appDataViewButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(AppDataView));
        }

        private void appDataBackupsButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(Backups));
        }

        private void appDataSettingsButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(Settings));
        }

        private void appDataAboutButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(About));
        }
    }
}
