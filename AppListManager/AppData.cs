using System.ComponentModel;
using Windows.Storage;
using Windows.UI.Xaml.Media;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;

namespace MahdiGhiasi.AppListManager
{
    public class AppData : INotifyPropertyChanged
    {
        public string PackageId { get; set; }
        public string PackageRootFolder { get; set; }
        public string PackageDataFolder { get; set; }
        public string DisplayName { get; set; }

        [JsonIgnore]
        public BitmapImage Logo {
            get
            {
                if ((LogoPath == null) || (LogoPath.Length == 0))
                    return null;
                return new BitmapImage(new Uri(LogoPath));
            }
        }

        public string LogoPath { get; set; }
        public string FamilyName { get; set; }
        public string Publisher { get; set; } = "";
        public bool IsLegacyApp { get; set; } = false;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyChange()
        {
            PropertyChanged(this, new PropertyChangedEventArgs(""));
        }
    }

    public class AppDataNameComparer : IComparer<AppData>
    {
        public int Compare(AppData a, AppData b)
        {
            return String.Compare(a.DisplayName, b.DisplayName);
        }
    }
}