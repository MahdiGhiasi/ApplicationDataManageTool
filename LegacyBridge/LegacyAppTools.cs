using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.Phone.Management.Deployment;
using Windows.ApplicationModel;

namespace LegacyBridge
{
    public class LegacyAppTools
    {
        Dictionary<string, Package> apps;
        public LegacyAppTools()
        {
            apps = new Dictionary<string, Package>();
            foreach (Package item in InstallationManager.FindPackages())
            {
                try
                {
                    apps.Add(item.Id.ProductId, item);
                }
                catch { } //Ignore this.
            }
        }

        public LegacyAppData GetAppData(string packageId)
        {
            if (!apps.ContainsKey(packageId))
                return null;

            Package package = apps[packageId];
            
            return new LegacyAppData
            {
                Name = package.Id.Name,
                Publisher = package.Id.Publisher,
                Author = package.Id.Author,
                ProductId = package.Id.ProductId,
                Version = package.Id.Version,
                InstallDate = package.InstallDate
            };
        }
    }

    public class LegacyAppData
    {
        public string Name { get; set; }
        public string Publisher { get; set; }
        public string ProductId { get; set; }
        public string Author { get; set; }
        public DateTimeOffset InstallDate { get; set; }
        public PackageVersion Version { get; set; }
    }
}
