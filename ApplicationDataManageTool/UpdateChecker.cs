using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.UI.Popups;

namespace AppDataManageTool
{
    class UpdateChecker
    {
        public static async void CheckForUpdates()
        {
            try
            {
                PackageVersion currentVersion = GetAppVersion();

                string text = await MakeWebRequest("http://www.ghiasi.net/AppDataManageTool/latestversion.txt?dtcache=" + DateTime.Now.ToString("yyyyMMddHHmmssfff"));
                string[] parts = text.Split('.');

                PackageVersion latestVersion = new PackageVersion();
                latestVersion.Major = ushort.Parse(parts[0]);
                latestVersion.Minor = ushort.Parse(parts[1]);
                latestVersion.Build = ushort.Parse(parts[2]);
                latestVersion.Revision = ushort.Parse(parts[3]);

                if (VersionToNumber(latestVersion) > VersionToNumber(currentVersion))
                {
                    MessageDialog md = new MessageDialog("App Data Manage Tool v" + text + " is available now.\r\n\r\n" +
                        "You can download it from:\r\nhttp://bit.ly/AppDataManageTool", "A newer version of App Data Manage Tool is available!");
                    await md.ShowAsync();
                }
            }
            catch
            {
                
            }
        }

        private static ulong VersionToNumber(PackageVersion latestVersion)
        {
            ulong major = latestVersion.Major;
            ulong minor = latestVersion.Minor;
            ulong build = latestVersion.Build;
            ulong revision = latestVersion.Revision;

            return (revision + build * 1000 + minor * 1000 * 1000 + major * 1000 * 1000 * 1000);
        }

        public static async Task<string> MakeWebRequest(string url)
        {
            HttpClient http = new System.Net.Http.HttpClient();
            HttpResponseMessage response = await http.GetAsync(url);
            return await response.Content.ReadAsStringAsync();
        }

        public static PackageVersion GetAppVersion()
        {
            Package package = Package.Current;
            PackageId packageId = package.Id;
            PackageVersion version = packageId.Version;

            return version;
        }

        public static string GetAppVersionString(bool showRevision)
        {
            PackageVersion version = GetAppVersion();
            if (showRevision)
                return string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
            else
                return string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Build);
        }
    }
}
