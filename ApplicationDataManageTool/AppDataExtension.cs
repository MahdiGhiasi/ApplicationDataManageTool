using System.ComponentModel;
using Windows.Storage;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using MahdiGhiasi.AppListManager;

namespace AppDataManageTool
{
    public class AppDataExtension : INotifyPropertyChanged
    {
        public string AppDataSize { get; set; } = "Calculating...";
        public bool SizeIsCalculated { get; set; } = false;
        public string familyName { get; set; }
        public AppData TheApp { get; set; }

        public static AppData FindAppData(string family)
        {
            foreach (var item in LoadAppData.appsData)
                if (item.FamilyName == family)
                    return item;
            return null;
        }

        public async Task CalculateSize()
        {
            try
            {
                if (SizeIsCalculated)
                    return;

                string path = TheApp.PackageDataFolder;
                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(path);

                List<IStorageItem> contents = await FileOperations.GetContents(folder);

                List<StorageFile> files = (from IStorageItem s in contents
                                           where s is StorageFile
                                           select (StorageFile)s).ToList();

                double sizeBytes = await FileOperations.GetSizeOfFiles(files);
                AppDataSize = FileOperations.GetFileSizeString(sizeBytes);
                SizeIsCalculated = true;
                NotifyChange();
            }
            catch
            {
                AppDataSize = "Unknown";
            }
        }

        internal void ResetSizeData()
        {
            AppDataSize = "Calculating...";
            SizeIsCalculated = false;
        }

        internal static async Task<string> GetDataFolder(CompactAppData data)
        {
            return await LoadAppData.GetDataFolder(GetAppDataFromCompactAppData(data));
        }

        internal static AppData GetAppDataFromCompactAppData(CompactAppData data)
        {
            return LoadAppData.appsData.FirstOrDefault(x => x.FamilyName == data.FamilyName);
        }

        internal static void ResetAppSizes()
        {
            foreach (AppData item in LoadAppData.appsData)
            {
                AppDataExtension itemEx = App.GetAppDataEx(item);
                itemEx.ResetSizeData();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyChange()
        {
            PropertyChanged(this, new PropertyChangedEventArgs(""));
        }
    }
}