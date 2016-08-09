using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace App5
{
    static class FileOperations
    {
        public static async Task<double> GetSize(StorageFolder folder)
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

        public static string GetFileSizeString(double byteCount)
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
    }
}
