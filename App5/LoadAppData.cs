using System;
using System.Collections.Generic;
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

namespace AppDataManageTool
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

        LegacyBridge.LegacyAppTools legacyTools;

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

        public async Task LoadApps()
        {
            bool loadLegacyApps = true;
            try
            {
                legacyTools = new LegacyBridge.LegacyAppTools();
            }
            catch (Exception ex)
            {
                MessageDialog md = new MessageDialog("Can't load legacy WP8 apps (" + ex.Message + ")");
                await md.ShowAsync();
                loadLegacyApps = false;
            }

            //Modern apps
            Windows.Management.Deployment.PackageManager packageManager = new Windows.Management.Deployment.PackageManager();
            IEnumerable<Windows.ApplicationModel.Package> packages = (IEnumerable<Windows.ApplicationModel.Package>)packageManager.FindPackagesForUser("");

            //Legacy apps
            StorageFolder programsFolder;
            IEnumerable<StorageFolder> programs = null;

            if (loadLegacyApps)
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
                    loadLegacyApps = false;
                }
            }

            int count = packages.Count() + (loadLegacyApps ? programs.Count() : 0);
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
                    App.appsData.AddSorted(appD, new AppDataNameComparer());
                    App.familyNameAppData.Add(appD.FamilyName, appD);
                    existingAppFamilyNames.Add(appD.FamilyName);
                }
                else if (appD != null)
                {
                    existingAppFamilyNames.Add(appD.FamilyName);
                }

                progress++;
                OnLoadingProgress(new LoadingEventArgs(progress, count));
            }

            if (loadLegacyApps)
            {
                System.Diagnostics.Debug.WriteLine("Now loading legacy apps...");

                foreach (StorageFolder item in programs)
                {
                    AppData appD = await LoadLegacyAppData(item);
                    if ((appD != null) && (appD.PackageId != ""))
                    {
                        App.appsData.AddSorted(appD, new AppDataNameComparer());
                        App.familyNameAppData.Add(appD.FamilyName, appD);
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
            foreach (var item in App.appsData)
            {
                if (!existingAppFamilyNames.Contains(item.FamilyName))
                    removedApps.Add(item);
            }

            foreach (var item in removedApps)
            {
                App.familyNameAppData.Remove(item.FamilyName);
                App.appsData.Remove(item);
            }


            OnLoadCompleted();
        }

        private async Task<AppData> LoadLegacyAppData(StorageFolder item)
        {
            if (App.familyNameAppData.ContainsKey(item.Name))
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

                if (App.familyNameAppData.ContainsKey(data.FamilyName))
                {
                    App.familyNameAppData[data.FamilyName].PackageId = item.Id.FullName; //Refresh package id.

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

        internal static async Task<string> GetDataFolder(AppData data)
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

        internal static async Task<string> GetDataFolder(CompactAppData data)
        {
            return await GetDataFolder(GetAppDataFromCompactAppData(data));
        }

        internal static AppData GetAppDataFromCompactAppData(CompactAppData data)
        {
            return App.appsData.FirstOrDefault(x => x.FamilyName == data.FamilyName);
        }

        internal static void ResetAppSizes()
        {
            foreach (AppData item in App.appsData)
            {
                item.ResetSizeData();
            }
        }

        internal static async Task DeleteAppListCache()
        {
            StorageFolder localCacheFolder = ApplicationData.Current.LocalCacheFolder;
            var cacheFile = await localCacheFolder.TryGetItemAsync("applistcache.txt");
            if ((cacheFile != null) && (cacheFile is StorageFile))
            {
                await (cacheFile as StorageFile).DeleteAsync();
            }
        }

        internal static async Task SaveAppList()
        {
            string serializedData = Newtonsoft.Json.JsonConvert.SerializeObject(App.appsData, Newtonsoft.Json.Formatting.Indented);

            System.Diagnostics.Debug.WriteLine(serializedData.Length);

            StorageFolder localCacheFolder = ApplicationData.Current.LocalCacheFolder;
            StorageFile cacheFile = await localCacheFolder.CreateFileAsync("applistcache.txt", CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(cacheFile, serializedData);
        }

        internal static async Task<List<AppData>> LoadCachedAppList()
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
            /* try
             {
                 string s = inputS.Substring(1);

                 string[] parts = s.Split(',');

                 StorageFile file = await curPath.GetFileAsync(parts[0]);
                 await file.CopyAsync(await StorageFolder.GetFolderFromPathAsync("C:\\Data\\Users\\Public"), parts[0], NameCollisionOption.ReplaceExisting);

                 return GetStringResource(System.IO.Path.Combine("C:\\Data\\Users\\Public", parts[0]), (uint)Math.Abs(int.Parse(parts[1])));
             }
             catch (Exception ex)
             {
                 return inputS;
             }
             */
        }
        /**
        static string GetStringResource(string dllPath, uint resourceId)
        {
            IntPtr pDll = NativeMethods.LoadLibrary(dllPath);
            
            return GetStringResource(pDll, resourceId);
        }

        /// <summary>Returns a string resource from a DLL.</summary>
        /// <param name="DLLHandle">The handle of the DLL (from LoadLibrary()).</param>
        /// <param name="ResID">The resource ID.</param>
        /// <returns>The name from the DLL.</returns>
        static string GetStringResource(IntPtr handle, uint resourceId)
        {
            StringBuilder buffer = new StringBuilder(8192);     //Buffer for output from LoadString()

            int length = NativeMethods.LoadString(handle, resourceId, buffer, buffer.Capacity);

            return buffer.ToString(0, length);      //Return the part of the buffer that was used.
        }


        static class NativeMethods
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern IntPtr LoadLibrary(string lpLibFileName);

            [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern int LoadString(IntPtr hInstance, uint wID, StringBuilder lpBuffer, int nBufferMax);

            [DllImport("kernel32.dll")]
            public static extern int FreeLibrary(IntPtr hLibModule);
        }
        /**/

        /**
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
/**/
    }
}
