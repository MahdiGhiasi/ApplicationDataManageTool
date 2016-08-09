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
        public string DisplayName { get; set; }
        public ImageSource Logo { get; set; }
        public string AppSize { get; set; } = "Calculating...";
        public string AppDataSize { get; set; } = "Calculating...";
        public string FamilyName { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;


        public async void CalculateSize()
        {
            string path = "C:\\Data\\Users\\DefApps\\APPDATA\\Local\\Packages\\" + FamilyName + "\\LocalState";
            PackageRootFolder = path;
            StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(path);//PackageRootFolder.ToLower().Replace("c:\\data\\","u:\\"));
            AppSize = (await GetSize(folder)).ToString() + " Bytes";
            AppDataSize = (await folder.GetFilesAsync()).Count.ToString();
            //NotifyChange();
        }

        private async Task<ulong> GetSize(StorageFolder folder)
        {
            ulong size = 0;
            foreach (var item in await folder.GetFilesAsync())
            {
                size += (await item.GetBasicPropertiesAsync()).Size;
            }
            return size;
        }

        internal void NotifyChange()
        {
            PropertyChanged(this, new PropertyChangedEventArgs(""));
        }
    }
}