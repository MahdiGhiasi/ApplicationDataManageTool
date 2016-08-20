﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

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
                    data.IsLegacyApp = false;

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
            if (data.IsLegacyApp)
                return "C:\\Data\\Users\\DefApps\\AppData\\" + data.FamilyName;
            else
                return "C:\\Data\\Users\\DefApps\\APPDATA\\Local\\Packages\\" + data.FamilyName;
        }

        internal static string GetDataFolder(CompactAppData data)
        {
            return GetDataFolder(GetAppDataFromCompactAppData(data));
        }

        internal static AppData GetAppDataFromCompactAppData(CompactAppData data)
        {
            return App.appsData.FirstOrDefault(x => x.FamilyName == data.FamilyName);
        }

        internal async Task<List<AppData>> LoadLegacyApps()
        {
            List<AppData> output = new List<AppData>();

            StorageFolder programsFolder = await StorageFolder.GetFolderFromPathAsync(@"C:\Data\Programs");
            foreach (StorageFolder item in await programsFolder.GetFoldersAsync())
            {
                IStorageItem s = await item.TryGetItemAsync("Install");
                if ((s != null) && (s is StorageFolder))
                {
                    StorageFolder installFolder = (StorageFolder)s;

                    IStorageItem m = await installFolder.TryGetItemAsync("WMAppManifest.xml");
                    if ((m != null) && (m is StorageFile))
                    {
                        StorageFile manifest = (StorageFile)m;

                        string text = await FileIO.ReadTextAsync(manifest);

                        string appTag = text.Substring(text.IndexOf("<App "));
                        appTag = appTag.Substring(0, appTag.IndexOf(">"));

                        string appName = appTag.Substring(appTag.IndexOf(@"Title=""") + @"Title=""".Length);
                        appName = appName.Substring(0, appName.IndexOf("\""));
                        appName = await GetString(appName);

                        string publisherName = appTag.Substring(appTag.IndexOf(@"Publisher=""") + @"Publisher=""".Length);
                        publisherName = publisherName.Substring(0, publisherName.IndexOf("\""));
                        publisherName = await GetString(publisherName);




                        AppData app = new AppData()
                        {
                            DisplayName = appName,
                            FamilyName = item.Name,
                            PackageId = item.Name,
                            PackageRootFolder = item.Path,
                            Publisher = publisherName,
                            IsLegacyApp = true
                        };

                        app.PackageDataFolder = GetDataFolder(app);

                        string iconPathTag;
                        try {
                            iconPathTag = text.Substring(text.IndexOf("<IconPath "));
                            iconPathTag = iconPathTag.Substring(iconPathTag.IndexOf(">") + 1);
                            iconPathTag = iconPathTag.Substring(0, iconPathTag.IndexOf("</IconPath>"));
                            iconPathTag = System.IO.Path.Combine(installFolder.Path, iconPathTag);

                            app.Logo = new BitmapImage(new Uri(iconPathTag));
                        }
                        catch
                        {
                            iconPathTag = "";
                        }

                        output.Add(app);
                    }
                }
            }

            return output;
        }

        static async Task<string> GetString(string inputS/*, StorageFolder curPath*/)
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
