using Ailon.WP.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;
using Windows.Security.ExchangeActiveSyncProvisioning;

namespace AppDataManageTool
{
    public class Backup
    {
        public Backup(string _name, string _subtitle)
        {
            Name = _name;
            Apps = new List<CompactAppData>();
            Subtitle = _subtitle;
        }

        public void SetDeviceInfo()
        {
            var deviceInfo = new Windows.Security.ExchangeActiveSyncProvisioning.EasClientDeviceInformation();
            DeviceName = deviceInfo.FriendlyName;

            DeviceModel = PhoneNameResolver.Resolve(deviceInfo.SystemManufacturer, deviceInfo.SystemProductName).FullCanonicalName;

            CreationDate = DateTime.Now;
        }

        public string Name { get; set; }
        public List<CompactAppData> Apps { get; set; }
        public string Subtitle { get; set; }
        public string DeviceModel { get; set; }
        public string DeviceName { get; set; }
        public DateTime CreationDate { get; set; }

        internal static string GenerateAppSubtitle(List<CompactAppData> apps)
        {
            return apps.Count == 1 ? apps[0].DisplayName : (apps.Count.ToString() + " apps");
        }

        internal static string GenerateAppSubtitle(List<AppData> apps)
        {
            return apps.Count == 1 ? apps[0].DisplayName : (apps.Count.ToString() + " apps");
        }
    }

    public class CompactAppData
    {
        public CompactAppData() { }
        public CompactAppData(AppData currentApp)
        {
            PackageId = currentApp.PackageId;
            DisplayName = currentApp.DisplayName;
            FamilyName = currentApp.FamilyName;
        }

        public string PackageId { get; set; }
        public string DisplayName { get; set; }
        public string FamilyName { get; set; }
    }
}
