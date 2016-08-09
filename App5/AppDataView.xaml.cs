using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
                e.Handled = true;
            }
        }

        private async void listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listView.SelectedItem == null)
                return;


            AdvancedDetails.Visibility = Visibility.Collapsed;
            ShowAdvancedDetails.Visibility = Visibility.Visible;

            AppDetails.DataContext = listView.SelectedItem;
            AppDetails.Visibility = Visibility.Visible;

            AppData data = (AppData)listView.SelectedItem;
            await data.CalculateSize();
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
    }
}
