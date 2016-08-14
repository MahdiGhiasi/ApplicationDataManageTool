using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Media.Imaging;

namespace App5
{

    public class LoadingEventArgs : EventArgs
    {
        private readonly int current = 0;
        private readonly int total = 1;

        // Constructor. 
        public LoadingEventArgs(int current, int total)
        {
            this.current = current;
            this.total = total;
        }

        public int Current
        {
            get { return current; }
        }

        public int Total
        {
            get { return total; }
        }
    }

    class LoadAppData
    {
        public delegate void LoadingEventHandler(object sender, LoadingEventArgs e);

        public event LoadingEventHandler LoadingProgress;
        public event EventHandler LoadCompleted;

        protected virtual void OnLoadingProgress(LoadingEventArgs e)
        {
            if (LoadingProgress != null)
                LoadingProgress(this, e);
        }

        protected virtual void OnLoadCompleted()
        {
            if (LoadCompleted != null)
                LoadCompleted(this, new EventArgs());
        }



        public async Task<Dictionary<string, AppData>> LoadAppsAsDictionary()
        {
            List<AppData> data = await LoadApps();
            Dictionary<string, AppData> dic = new Dictionary<string, AppData>();

            foreach (var item in data)
            {
                dic.Add(item.PackageId, item);
            }

            return dic;
        }

        public async Task<List<AppData>> LoadApps()
        {
            List<AppData> list = new List<AppData>();

            Windows.Management.Deployment.PackageManager packageManager = new Windows.Management.Deployment.PackageManager();

            IEnumerable<Windows.ApplicationModel.Package> packages = (IEnumerable<Windows.ApplicationModel.Package>)packageManager.FindPackagesForUser("");

            int count = packages.Count();
            int progress = 0;

            foreach (var item in packages)
            {
                AppData data = new AppData();
                try
                {
                    var x = await item.GetAppListEntriesAsync();
                    data.DisplayName = (x.First().DisplayInfo.DisplayName);

                    BitmapImage bmp = new BitmapImage();
                    bmp.SetSource(await x.First().DisplayInfo.GetLogo(new Size(50, 50)).OpenReadAsync());
                    data.Logo = bmp;

                    data.PackageId = item.Id.FullName;
                    data.PackageRootFolder = item.InstalledLocation.Path;
                    data.FamilyName = item.Id.FamilyName;
                    data.PackageDataFolder = GetDataFolder(data);

                    list.Add(data);
                }
                catch { }
                finally
                {
                    progress++;
                    OnLoadingProgress(new LoadingEventArgs(progress, count));
                }
            }

            list = list.OrderBy(x => x.DisplayName).ToList();

            OnLoadCompleted();
            return list;
        }

        internal static string GetDataFolder(AppData data)
        {
            return "C:\\Data\\Users\\DefApps\\APPDATA\\Local\\Packages\\" + data.FamilyName;
        }

        internal static string GetDataFolder(CompactAppData data)
        {
            return GetDataFolder(GetAppDataFromCompactAppData(data));
        }

        internal static AppData GetAppDataFromCompactAppData(CompactAppData data)
        {
            return App.appsData.FirstOrDefault(x => x.PackageId == data.PackageId);
        }

        public List<AppData> LoadAppNamesRegistry()
        {
            List<AppData> list = new List<AppData>();

            var reg = new RegistryHelper.CRegistryHelper();

            foreach (var i in reg.GetRegistryItems(RegistryHelper.RegHives.HKEY_CURRENT_USER, @"SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages"))
            {
                var children = reg.GetRegistryItems(RegistryHelper.RegHives.HKEY_CURRENT_USER, @"SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages\" + i.Name);

                AppData data = new AppData();

                foreach (var item in children)
                {
                    if (item.Name == "PackageRootFolder")
                        data.PackageRootFolder = item.Value;
                    else if (item.Name == "PackageID")
                        data.PackageId = item.Value;
                    else if (item.Name == "DisplayName")
                        data.DisplayName = item.Value;
                }

                list.Add(data);
            }

            return list;
        }

    }
}
