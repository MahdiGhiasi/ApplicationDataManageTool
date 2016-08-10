using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App5
{
    public class Backup
    {
        public Backup(string _name)
        {
            Name = _name;
            Apps = new List<CompactAppData>();
        }

        public string Name { get; set; }
        public List<CompactAppData> Apps { get; set; }
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
