using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Popups;
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
    public sealed partial class AppDataView : Page
    {

        ObservableCollection<AppData> appsData = new ObservableCollection<AppData>();
        AppData currentApp = null;

        public AppDataView()
        {
            this.InitializeComponent();

            AppDetails.Visibility = Visibility.Collapsed;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ((App)App.Current).BackRequested += AppDataView_BackRequested;

            listView.ItemsSource = appsData;
            foreach (var item in App.appsData)
            {
                appsData.Add(item);
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            ((App)App.Current).BackRequested -= AppDataView_BackRequested;

            base.OnNavigatingFrom(e);
        }

        private void AppDataView_BackRequested(object sender, BackRequestedEventArgs e)
        {
            if (AppDetails.Visibility == Visibility.Visible)
            {
                AppDetails.Visibility = Visibility.Collapsed;
                listView.SelectedItem = null;
                commandBar.Visibility = Visibility.Visible;
                e.Handled = true;
            }
        }

        private async void listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listView.SelectedItem == null)
                return;

            if (listView.SelectionMode == ListViewSelectionMode.Single)
            {
                AdvancedDetails.Visibility = Visibility.Collapsed;
                ShowAdvancedDetails.Visibility = Visibility.Visible;

                AppDetails.DataContext = listView.SelectedItem;
                AppDetails.Visibility = Visibility.Visible;

                commandBar.Visibility = Visibility.Collapsed;

                AppData data = (AppData)listView.SelectedItem;

                currentApp = data;

                await data.CalculateSize();
            }
        }

        private async void CopyToClipboardTextBlock_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var pkg = new DataPackage();
            pkg.SetText(((TextBlock)sender).Text);
            Clipboard.SetContent(pkg);

            MessageDialog md = new MessageDialog("Copied to clipboard");
            await md.ShowAsync();
        }

        public void TestStorage()
        {

        }

        public async void TestRegistry()
        {
            var x = new RegistryHelper.CRegistryHelper();

            foreach (var i in x.GetRegistryItems(RegistryHelper.RegHives.HKEY_LOCAL_MACHINE, "SYSTEM"))
            {
                MessageDialog md = new MessageDialog(i.Name);
                await md.ShowAsync();
            }
        }

        private void HideAdvancedDetails_Tapped(object sender, TappedRoutedEventArgs e)
        {
            AdvancedDetails.Visibility = Visibility.Collapsed;
            ShowAdvancedDetails.Visibility = Visibility.Visible;
        }

        private void ShowAdvancedDetails_Tapped(object sender, TappedRoutedEventArgs e)
        {
            AdvancedDetails.Visibility = Visibility.Visible;
            ShowAdvancedDetails.Visibility = Visibility.Collapsed;
        }

        private void SelectAppBarButton_Unchecked(object sender, RoutedEventArgs e)
        {
            listView.SelectedItem = null;
            listView.SelectionMode = ListViewSelectionMode.Single;
        }

        private void SelectAppBarButton_Checked(object sender, RoutedEventArgs e)
        {
            listView.SelectionMode = ListViewSelectionMode.Multiple;
        }

        private async void CreateBackupButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await StartCreatingBackup(currentApp);
        }

        private async void BackupAppBarButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (listView.SelectedItems.Count == 0)
            {
                MessageDialog md = new MessageDialog("Please select some apps.");
                await md.ShowAsync();
                return;
            }

            await StartCreatingBackup(listView.SelectedItems.Cast<AppData>().ToList());
        }

        private async Task StartCreatingBackup(List<AppData> app)
        {
            List<CompactAppData> l = new List<CompactAppData>();
            foreach (var item in app)
            {
                l.Add(new CompactAppData(item));
            }
            await StartCreatingBackup(l);
        }

        private async Task StartCreatingBackup(AppData app)
        {
            await StartCreatingBackup(new CompactAppData(app));
        }

        private async Task StartCreatingBackup(CompactAppData app)
        {
            List<CompactAppData> l = new List<CompactAppData>();
            l.Add(app);
            await StartCreatingBackup(l);
        }

        private async Task StartCreatingBackup(List<CompactAppData> app)
        {
            var dialog = new BackupNameDialog(BackupManager.GenerateBackupName());
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                Visibility cmdvis = commandBar.Visibility;
                commandBar.Visibility = Visibility.Collapsed;
                progress.Visibility = Visibility.Visible;
                this.IsEnabled = false;

                try
                {
                    await FileOperations.IsValidBackupName(dialog.Text);

                    Backup backup = new Backup(dialog.Text);

                    backup.Apps.AddRange(app);

                    Frame.Navigate(typeof(BackupProgress), Newtonsoft.Json.JsonConvert.SerializeObject(backup));
                }
                catch (Exception ex)
                {
                    MessageDialog md = new MessageDialog(ex.Message);
                    await md.ShowAsync();
                }
                finally
                {
                    progress.Visibility = Visibility.Collapsed;
                    commandBar.Visibility = cmdvis;
                    this.IsEnabled = true;
                }
            }
        }
    }
}
