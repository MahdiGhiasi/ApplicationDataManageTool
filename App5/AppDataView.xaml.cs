using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
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

namespace AppDataManageTool
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AppDataView : Page
    {
        public static AppData PageStatus_CurrentApp = null;
        public static bool PageStatus_IsShowingDetails = false;

        AppData currentApp = null;

        public AppDataView()
        {
            this.InitializeComponent();

            AppDetails.Visibility = Visibility.Collapsed;
            AdvancedDetails.Visibility = Visibility.Collapsed;
            ShowAdvancedDetails.Visibility = App.hiddenMode ? Visibility.Visible : Visibility.Collapsed;

            hiddenButtons.Visibility = App.hiddenMode ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ((App)App.Current).BackRequested += AppDataView_BackRequested;

            if ((PageStatus_CurrentApp != null) && (PageStatus_IsShowingDetails))
            {
                AppDetails.Visibility = Visibility.Visible;
                commandBar.Visibility = Visibility.Collapsed;
            }

            List<AppData> appsPlus = new List<AppData>(App.appsData);
            for (char i = 'A'; i <= 'Z'; i++)
            {
                appsPlus.Add(new AppData()
                {
                    DisplayName = i.ToString(),
                    FamilyName = "",
                });
            }
            appsPlus.Add(new AppData() { DisplayName = "0", FamilyName = "" });
            appsPlus.Add(new AppData() { DisplayName = "زبان دیگر", FamilyName = "" });

            appsPlus = appsPlus.OrderBy(x => x.DisplayName).ToList();

            ObservableCollection<DataGroup> groupedData = new ObservableCollection<DataGroup>(appsPlus.GroupBy((d) => ItemsGroupName(d)
            , (key, items) => new DataGroup()
            {
                Name = key,
                Items = new ObservableCollection<AppData>(items)
            }).ToList());

            foreach (var item in groupedData)
            {
                item.Items = new ObservableCollection<AppData>((from AppData x in item.Items
                                                                where x.FamilyName.Length > 0
                                                                select x).ToList());
            }

            App.appsData.CollectionChanged += async (object s, System.Collections.Specialized.NotifyCollectionChangedEventArgs ee) =>
            {
                if (ee.NewItems != null)
                {
                    foreach (AppData item in ee.NewItems)
                    {
                        if ((ee.OldItems != null) && (ee.OldItems.Contains(item)))
                            continue;
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            try
                            {
                                string groupName = ItemsGroupName(item);
                                var group = groupedData.First(x => x.Name == groupName);
                                group.Items.AddSorted(item, new AppDataNameComparer());
                                group.NotifyChange();
                            }
                            catch
                            {
                                //Avoid thread crash if this page instance no longer exists.
                            }
                        });
                    }
                }

                if (ee.OldItems != null)
                {
                    foreach (AppData item in ee.OldItems)
                    {
                        if ((ee.NewItems != null) && (ee.NewItems.Contains(item)))
                            continue;
                        try
                        {
                            string groupName = ItemsGroupName(item);
                            var group = groupedData.First(x => x.Name == groupName);
                            group.Items.Remove(item);
                            group.NotifyChange();
                        }
                        catch
                        {
                            //Avoid thread crash if this page instance no longer exists.
                        }
                    }
                }
            };

            collection.Source = groupedData;

            listView.SelectedIndex = -1;
            listView.SelectionChanged += listView_SelectionChanged;

            await Task.Delay(50);

            if (PageStatus_CurrentApp != null)
            {
                listView.ScrollIntoView(PageStatus_CurrentApp);
                if (PageStatus_IsShowingDetails)
                {
                    listView.SelectedItem = PageStatus_CurrentApp;
                }
            }
        }

        private static string ItemsGroupName(AppData d)
        {
            if (d.DisplayName.Length == 0)
                return "#";
            else if ("0123456789".Contains(d.DisplayName[0]))
                return "#";
            else if ("abcdefghijklmnopqrstuvwxyz".Contains(d.DisplayName[0].ToString().ToLower()))
                return d.DisplayName[0].ToString().ToUpper();
            else
                return "...";
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            ((App)App.Current).BackRequested -= AppDataView_BackRequested;
            listView.SelectionChanged -= listView_SelectionChanged;

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
            else if (listView.SelectionMode == ListViewSelectionMode.Multiple)
            {
                SelectAppBarButton.IsChecked = false;
                e.Handled = true;
            }
        }

        private async void listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listView.SelectedItem == null)
                return;

            if (listView.SelectionMode == ListViewSelectionMode.Single)
            {
                AppDetails.DataContext = null;

                AdvancedDetails.Visibility = Visibility.Collapsed;
                ShowAdvancedDetails.Visibility = App.hiddenMode ? Visibility.Visible : Visibility.Collapsed;

                AppDetails.DataContext = listView.SelectedItem;
                AppDetails.Visibility = Visibility.Visible;

                commandBar.Visibility = Visibility.Collapsed;

                AppData data = (AppData)listView.SelectedItem;

                currentApp = data;

                List<Backup> backupsContainingThisApp = (from Backup b in BackupManager.currentBackups
                                                         where b.Apps.Any(x => x.FamilyName == currentApp.FamilyName)
                                                         select b).ToList();

                noBackupsAvailable.Visibility = backupsContainingThisApp.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                backupsList.ItemsSource = backupsContainingThisApp;

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
        /**
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
        /**/
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

        private async Task StartCreatingBackup(List<CompactAppData> apps)
        {
            PageStatus_CurrentApp = App.appsData.First(x => x.PackageId == apps.OrderBy(y => y.DisplayName).Last().PackageId);
            PageStatus_IsShowingDetails = AppDetails.Visibility == Visibility.Visible;

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

                    Backup b = new Backup(dialog.Text, Backup.GenerateAppSubtitle(apps));

                    b.Apps.AddRange(apps);

                    Frame.Navigate(typeof(BackupProgress), Newtonsoft.Json.JsonConvert.SerializeObject(new BackupProgressMessage() { backup = b, IsRestore = false }));
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

        private async void ResetAppButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            MessageDialog md = new MessageDialog("The data will be permanently lost. Consider creating a backup before this.\r\n\r\nNote: Doing this on 'system apps' is not safe, and might cause your phone to stop working.", "Are you sure you want to reset the state of this app?");
            md.Commands.Add(new UICommand("Yes") { Id = 1 });
            md.Commands.Add(new UICommand("No") { Id = 0 });
            md.DefaultCommandIndex = 1;
            md.CancelCommandIndex = 0;

            var result = await md.ShowAsync();

            if (((int)result.Id) == 1)
            {
                progress.Visibility = Visibility.Visible;
                BackupManager bm = new BackupManager();
                await bm.ResetAppData(currentApp);
                FileOperations.RemoveFromGetContentsCache(currentApp.FamilyName);

                await currentApp.CalculateSize();
                progress.Visibility = Visibility.Collapsed;
            }
        }

        private void backupsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PageStatus_CurrentApp = currentApp;
            PageStatus_IsShowingDetails = true;

            Frame.Navigate(typeof(Backups), backupsList.SelectedItem);
        }

        private async void ZipFromInstallFilesAppButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            HiddenThings ht = new HiddenThings();

            progress.Visibility = Visibility.Visible;

            string fileName = currentApp.FamilyName + ".zip";

            FileSavePicker fsp = new FileSavePicker();
            fsp.FileTypeChoices.Add("Zip archive", new[] { ".zip" });
            fsp.SuggestedFileName = fileName;
            
            StorageFile file = await fsp.PickSaveFileAsync();

            if (file != null)
            {
                await file.DeleteAsync();
                ht.Progress += Ht_Progress;
                await ht.BackupPath(currentApp.PackageRootFolder, file.Path);
                ht.Progress -= Ht_Progress;

                MessageDialog md = new MessageDialog("Saved to " + file.Path);
                await md.ShowAsync();
            }

            progressText.Text = "";
            progress.Visibility = Visibility.Collapsed;
        }

        private void Ht_Progress(object sender, string message)
        {
            progressText.Text = message;
        }
    }

    internal class DataGroup : INotifyPropertyChanged
    {
        public string Name
        {
            get;
            set;
        }

        private ObservableCollection<AppData> items;

        public ObservableCollection<AppData> Items
        {
            get { return items; }
            set { items = value; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public override string ToString()
        {
            return Name;
        }

        public void NotifyChange()
        {
            PropertyChanged(this, new PropertyChangedEventArgs(""));
        }
    }
}
