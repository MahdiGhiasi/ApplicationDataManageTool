using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
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

        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            listView.ItemsSource = appsData;
            foreach (var item in App.appsData)
            {
                appsData.Add(item);
            }
        }

        private void listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                foreach (var item in e.RemovedItems)
                {
                    // Set the DataTemplate of the deselected ListViewItems
                    ((ListViewItem)(sender as ListView).ContainerFromItem(item)).ContentTemplate = appsListSmall;
                }
            }
            catch { }

            try
            {
                AppData cur = (AppData)e.AddedItems[0];
                ((ListViewItem)(sender as ListView).ContainerFromItem(cur)).ContentTemplate = appsListLarge;
                cur.CalculateSize();
            }
            catch { }

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

    }
}
