using System.ComponentModel;
using Windows.Storage;
using Windows.UI.Xaml.Media;
using System;
using System.Threading.Tasks;

namespace App5
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

        public event PropertyChangedEventHandler PropertyChanged;


        public async Task CalculateSize()
        {
            try {
                if (SizeIsCalculated)
                    return;

                string path = PackageDataFolder;
                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(path);//PackageRootFolder.ToLower().Replace("c:\\data\\","u:\\"));
                AppDataSize = GetFileSizeString(await GetSize(folder));
                NotifyChange();
                SizeIsCalculated = true;
            }
            catch
            {
                AppDataSize = "Unknown";
            }
        }

        private async Task<double> GetSize(StorageFolder folder)
        {
            double size = 0;
            foreach (var item in await folder.GetFilesAsync())
            {
                size += (await item.GetBasicPropertiesAsync()).Size;
            }

            foreach (var item in await folder.GetFoldersAsync())
            {
                size += await GetSize(item);
            }

            return size;
        }

        private string GetFileSizeString(double byteCount)
        {
            string size = "0 Bytes";
            if (byteCount >= 1073741824.0)
                size = String.Format("{0:##.##}", byteCount / 1073741824.0) + " GB";
            else if (byteCount >= 1048576.0)
                size = String.Format("{0:##.##}", byteCount / 1048576.0) + " MB";
            else if (byteCount >= 1024.0)
                size = String.Format("{0:##.##}", byteCount / 1024.0) + " KB";
            else if (byteCount > 0 && byteCount < 1024.0)
                size = byteCount.ToString() + " Bytes";

            return size;
        }


        internal void NotifyChange()
        {
            PropertyChanged(this, new PropertyChangedEventArgs(""));
        }
    }
}