using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml.Media.Imaging;
using WinRTXamlToolkit.Imaging;

namespace MahdiGhiasi.AppListManager
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

    public class LoadAppData
    {
        public static ObservableCollection<AppData> appsData { get; set; } = new ObservableCollection<AppData>();
        public static Dictionary<string, AppData> familyNameAppData { get; set; } = new Dictionary<string, AppData>();

        public delegate void LoadingEventHandler(object sender, LoadingEventArgs e);

        public event LoadingEventHandler LoadingProgress;
        public event EventHandler LoadCompleted;

        LegacyBridge.LegacyAppTools legacyTools;

        public bool LoadLegacyAppsToo { get; set; }

        public LoadAppData(bool loadLegacyAppsToo = true)
        {
            LoadLegacyAppsToo = loadLegacyAppsToo;
            LoadLegacyTools();
        }

        private void LoadLegacyTools()
        {
            if (LoadLegacyAppsToo)
            {
                try
                {
                    legacyTools = new LegacyBridge.LegacyAppTools();
                }
                catch (Exception ex)
                {
                    throw new Exception("Can't load legacy tools.", ex);
                }
            }
        }

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

        public async Task LoadApps(bool reloadLegacyTools = false)
        {
            if (reloadLegacyTools)
            {
                LoadLegacyTools();
            }

            //Modern apps
            Windows.Management.Deployment.PackageManager packageManager = new Windows.Management.Deployment.PackageManager();
            IEnumerable<Windows.ApplicationModel.Package> packages = (IEnumerable<Windows.ApplicationModel.Package>)packageManager.FindPackagesForUser("");

            //Legacy apps
            StorageFolder programsFolder;
            IEnumerable<StorageFolder> programs = null;

            if (LoadLegacyAppsToo)
            {
                try
                {
                    programsFolder = await StorageFolder.GetFolderFromPathAsync(@"C:\Data\Programs");
                    programs = await programsFolder.GetFoldersAsync();
                }
                catch (Exception ex)
                {
                    MessageDialog md = new MessageDialog("Can't access legacy WP8 apps folder (" + ex.Message + ")");
                    await md.ShowAsync();
                    LoadLegacyAppsToo = false;
                }
            }

            int count = packages.Count() + (LoadLegacyAppsToo ? programs.Count() : 0);
            int progress = 0;


            StorageFolder localCacheFolder = ApplicationData.Current.LocalCacheFolder;

            if ((await localCacheFolder.TryGetItemAsync("Logos")) == null)
                await localCacheFolder.CreateFolderAsync("Logos");

            StorageFolder logosFolder = await localCacheFolder.GetFolderAsync("Logos");

            HashSet<string> existingAppFamilyNames = new HashSet<string>();
            foreach (var item in packages)
            {
                System.Diagnostics.Debug.WriteLine(progress);

                AppData appD = await LoadModernAppData(item, logosFolder);
                if ((appD != null) && (appD.PackageId != ""))
                {
                    appsData.AddSorted(appD, new AppDataNameComparer());
                    familyNameAppData.Add(appD.FamilyName, appD);
                    existingAppFamilyNames.Add(appD.FamilyName);
                }
                else if (appD != null)
                {
                    existingAppFamilyNames.Add(appD.FamilyName);
                }

                progress++;
                OnLoadingProgress(new LoadingEventArgs(progress, count));
            }

            if (LoadLegacyAppsToo)
            {
                System.Diagnostics.Debug.WriteLine("Now loading legacy apps...");

                foreach (StorageFolder item in programs)
                {
                    AppData appD = await LoadLegacyAppData(item);
                    if ((appD != null) && (appD.PackageId != ""))
                    {
                        appsData.AddSorted(appD, new AppDataNameComparer());
                        familyNameAppData.Add(appD.FamilyName, appD);
                        existingAppFamilyNames.Add(appD.FamilyName);
                    }
                    else if (appD != null)
                    {
                        existingAppFamilyNames.Add(appD.FamilyName);
                    }

                    progress++;
                    OnLoadingProgress(new LoadingEventArgs(progress, count));
                }
            }

            //Remove apps that are no longer installed on device from cache.
            List<AppData> removedApps = new List<AppData>();
            foreach (var item in appsData)
            {
                if (!existingAppFamilyNames.Contains(item.FamilyName))
                    removedApps.Add(item);
            }

            foreach (var item in removedApps)
            {
                familyNameAppData.Remove(item.FamilyName);
                appsData.Remove(item);
            }


            SaveAppList();
            OnLoadCompleted();
        }

        private async Task<AppData> LoadLegacyAppData(StorageFolder item)
        {
            if (familyNameAppData.ContainsKey(item.Name))
                return new AppData()
                {
                    FamilyName = item.Name,
                    PackageId = ""
                };

            try
            {
                IStorageItem s = await item.TryGetItemAsync("Install");
                if ((s != null) && (s is StorageFolder))
                {
                    StorageFolder installFolder = (StorageFolder)s;

                    IStorageItem m = await installFolder.TryGetItemAsync("WMAppManifest.xml");
                    if ((m != null) && (m is StorageFile))
                    {
                        string appName, publisherName;

                        StorageFile manifest = (StorageFile)m;

                        string text = await FileIO.ReadTextAsync(manifest);

                        var appData = legacyTools.GetAppData(item.Name);

                        if (appData != null)
                        {
                            appName = appData.Name;
                            publisherName = appData.Publisher;
                        }
                        else
                        {
                            string appTag = text.Substring(text.IndexOf("<App "));
                            appTag = appTag.Substring(0, appTag.IndexOf(">"));

                            appName = appTag.Substring(appTag.IndexOf(@"Title=""") + @"Title=""".Length);
                            appName = appName.Substring(0, appName.IndexOf("\""));
                            appName = GetNameStringFromManifestFormat(appName);

                            publisherName = appTag.Substring(appTag.IndexOf(@"Publisher=""") + @"Publisher=""".Length);
                            publisherName = publisherName.Substring(0, publisherName.IndexOf("\""));
                            publisherName = GetNameStringFromManifestFormat(publisherName);
                        }

                        AppData app = new AppData()
                        {
                            DisplayName = appName,
                            FamilyName = item.Name,
                            PackageId = item.Name,
                            PackageRootFolder = item.Path,
                            Publisher = publisherName,
                            IsLegacyApp = true
                        };

                        app.PackageDataFolder = await GetDataFolder(app);

                        string iconPathTag;
                        try
                        {
                            iconPathTag = text.Substring(text.IndexOf("<IconPath "));
                            iconPathTag = iconPathTag.Substring(iconPathTag.IndexOf(">") + 1);
                            iconPathTag = iconPathTag.Substring(0, iconPathTag.IndexOf("</IconPath>"));
                            iconPathTag = System.IO.Path.Combine(installFolder.Path, iconPathTag);

                            app.LogoPath = iconPathTag;
                        }
                        catch
                        {
                            iconPathTag = "";
                        }

                        return app;
                    }
                }
            }
            catch { }

            return null;
        }

        private async Task<AppData> LoadModernAppData(Windows.ApplicationModel.Package item, StorageFolder saveLogoLocation)
        {
            AppData data = new AppData();
            try
            {
                data.FamilyName = item.Id.FamilyName;

                if (familyNameAppData.ContainsKey(data.FamilyName))
                {
                    familyNameAppData[data.FamilyName].PackageId = item.Id.FullName; //Refresh package id.

                    data.PackageId = "";
                    return data;
                }

                IReadOnlyList<Windows.ApplicationModel.Core.AppListEntry> x = await item.GetAppListEntriesAsync();

                if ((x == null) || (x.Count == 0))
                    return null;

                data.DisplayName = (x.First().DisplayInfo.DisplayName);

                data.PackageId = item.Id.FullName;
                data.PackageRootFolder = item.InstalledLocation.Path;
                data.PackageDataFolder = await GetDataFolder(data);
                data.IsLegacyApp = false;

                if ((await saveLogoLocation.TryGetItemAsync(data.FamilyName + ".png")) == null)
                {
                    WriteableBitmap bmp = null;
                    try
                    {
                        var stream = await x.First().DisplayInfo.GetLogo(new Size(50, 50)).OpenReadAsync();
                        BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
                        bmp = new WriteableBitmap((int)decoder.PixelWidth, (int)decoder.PixelHeight);
                        bmp.SetSource(stream);

                        await bmp.SaveAsync(saveLogoLocation, data.FamilyName + ".png");
                    }
                    catch { }
                }

                data.LogoPath = System.IO.Path.Combine(saveLogoLocation.Path, data.FamilyName + ".png");

                try
                {
                    var appxManifest = await item.InstalledLocation.TryGetItemAsync("AppxManifest.xml");
                    if ((appxManifest != null) && (appxManifest is StorageFile))
                    {
                        string appxManifestData = await FileIO.ReadTextAsync((StorageFile)appxManifest);

                        string publisher = appxManifestData.Substring(appxManifestData.IndexOf("<PublisherDisplayName>") + "<PublisherDisplayName>".Length);
                        publisher = publisher.Substring(0, publisher.IndexOf("</PublisherDisplayName>"));

                        if ((publisher.Length > "ms-resource:".Length) && (publisher.Substring(0, "ms-resource:".Length) == "ms-resource:"))
                            publisher = "";

                        data.Publisher = publisher;
                    }
                }
                catch { }

                return data;
            }
            catch { }

            return null;
        }

        public static async Task<string> GetDataFolder(AppData data)
        {
            if (data.IsLegacyApp)
            {
                string Cpath = "C:\\Data\\Users\\DefApps\\AppData\\" + data.FamilyName;
                string Dpath = "D:\\WPSystem\\AppData\\" + data.FamilyName;

                try
                {
                    var x = await StorageFolder.GetFolderFromPathAsync(Cpath);
                    return Cpath;
                }
                catch
                {
                    try
                    {
                        var y = await StorageFolder.GetFolderFromPathAsync(Dpath);
                        return Dpath;
                    }
                    catch
                    {
                        return Cpath;
                    }
                }
            }
            else
                return "C:\\Data\\Users\\DefApps\\APPDATA\\Local\\Packages\\" + data.FamilyName;
        }

        public static async Task DeleteAppListCache()
        {
            StorageFolder localCacheFolder = ApplicationData.Current.LocalCacheFolder;
            var cacheFile = await localCacheFolder.TryGetItemAsync("applistcache.txt");
            if ((cacheFile != null) && (cacheFile is StorageFile))
            {
                await (cacheFile as StorageFile).DeleteAsync();
            }
        }

        public static async Task SaveAppList()
        {
            string serializedData = Newtonsoft.Json.JsonConvert.SerializeObject(appsData, Newtonsoft.Json.Formatting.Indented);

            StorageFolder localCacheFolder = ApplicationData.Current.LocalCacheFolder;
            StorageFile cacheFile = await localCacheFolder.CreateFileAsync("applistcache.txt", CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(cacheFile, serializedData);
        }

        public static async Task<bool> LoadCachedAppList()
        {
            appsData = new ObservableCollection<AppData>(await GetCachedAppList());
            familyNameAppData.Clear();
            foreach (var item in appsData)
            {
                familyNameAppData.Add(item.FamilyName, item);
            };

            return appsData.Count != 0;
        }

        private static async Task<List<AppData>> GetCachedAppList()
        {
            StorageFolder localCacheFolder = ApplicationData.Current.LocalCacheFolder;
            var file = await localCacheFolder.TryGetItemAsync("applistcache.txt");
            if ((file != null) && (file is StorageFile))
            {
                StorageFile cacheFile = file as StorageFile;
                string data = await FileIO.ReadTextAsync(cacheFile);

                return Newtonsoft.Json.JsonConvert.DeserializeObject<List<AppData>>(data);
            }

            return new List<AppData>();
        }

        static string GetNameStringFromManifestFormat(string inputS/*, StorageFolder curPath*/)
        {
            if (inputS.Length < 1)
                return inputS;
            else if (inputS[0] != '@')
                return inputS;
            else
                return "Unknown";
        }
    }
}
