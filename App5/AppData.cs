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
                AppDataSize = FileOperations.GetFileSizeString(await FileOperations.GetSize(folder));
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
    }
}