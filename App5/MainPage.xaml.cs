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
using Windows.System.Display;
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

namespace AppDataManageTool
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        Dictionary<string, AppData> AppNames = new Dictionary<string, AppData>();
        LoadAppData loadAppData = new LoadAppData();
        BackupManager.BackupLoader backupLoader = new BackupManager.BackupLoader();
        DisplayRequest displayRequest;

        public MainPage()
        {
            this.InitializeComponent();

            loadAppData.LoadingProgress += LoadAppData_LoadingProgress;
            backupLoader.LoadBackupsProgress += BackupLoader_LoadBackupsProgress;

            ((App)App.Current).BackRequested += MainPage_BackRequested;

            InitSettings();
        }

        private async void InitSettings()
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            if ((localSettings.Values["allowCompress"] != null) && (localSettings.Values["allowCompress"].GetType() == typeof(bool)))
            {
                App.AllowCompress = (bool)localSettings.Values["allowCompress"];
            }
            else
            {
                localSettings.Values["allowCompress"] = App.AllowCompress;
            }


            if ((localSettings.Values["backupDest"] != null) && (localSettings.Values["backupDest"].GetType() == typeof(string)))
            {
                App.BackupDestination = (string)localSettings.Values["backupDest"];
            }
            else
            {
                localSettings.Values["backupDest"] = App.BackupDestination;
            }

            await FileOperations.CreateDirectoryIfNotExists(App.BackupDestination);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            ((App)App.Current).BackRequested -= MainPage_BackRequested;
            base.OnNavigatingFrom(e);
        }

        private void MainPage_BackRequested(object sender, Windows.UI.Core.BackRequestedEventArgs e)
        {
            App.Current.Exit();
        }

        private void BackupLoader_LoadBackupsProgress(object sender, LoadingEventArgs e)
        {
            if (e.Current == 0)
            {
                progressStatus.Text = "Loading current backups...";
            }
            else
            {
                int percent = (int)Math.Round((100.0 * e.Current) / e.Total);
                progressStatus.Text = "Loading current backups " + percent.ToString() + "%";
            }
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
                displayRequest = new DisplayRequest();
                displayRequest.RequestActive();

                progress.Visibility = Visibility.Visible;
                progressRing.IsActive = true;

                App.appsData = await loadAppData.LoadApps();

                await backupLoader.LoadCurrentBackups();

                progress.Visibility = Visibility.Collapsed;
                progressRing.IsActive = false;

                Frame.Background = Header.Background;

                displayRequest.RequestRelease();
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

        private void Secret3_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if ((App.secretCodeCounter == 12) || (App.secretCodeCounter == 120))
                App.secretCodeCounter *= 10;
            else
                App.secretCodeCounter = 0;

            System.Diagnostics.Debug.WriteLine("SECRET3");
        }
    }
}
