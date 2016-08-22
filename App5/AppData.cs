using System.ComponentModel;
using Windows.Storage;
using Windows.UI.Xaml.Media;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace AppDataManageTool
{
    public class AppData : INotifyPropertyChanged
    {
        public string PackageId { get; set; }
        public string PackageRootFolder { get; set; }
        public string PackageDataFolder { get; set; }
        public string DisplayName { get; set; }
        public ImageSource Logo { get; set; }
        public string AppDataSize { get; set; } = "Calculating...";
        public string FamilyName { get; set; }
        public bool SizeIsCalculated { get; set; } = false;
        public string Publisher { get; set; } = "";
        public bool IsLegacyApp { get; set; } = false;

        public event PropertyChangedEventHandler PropertyChanged;

        public static AppData FindAppData(string family)
        {
            foreach (var item in App.appsData)
                if (item.FamilyName == family)
                    return item;
            return null;
        }

        public async Task CalculateSize()
        {
            try {
                if (SizeIsCalculated)
                    return;

                string path = PackageDataFolder;
                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(path);//PackageRootFolder.ToLower().Replace("c:\\data\\","u:\\"));

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

        internal void NotifyChange()
        {
            PropertyChanged(this, new PropertyChangedEventArgs(""));
        }

        internal void ResetSizeData()
        {
            AppDataSize = "Calculating...";
            SizeIsCalculated = false;
        }
    }
}