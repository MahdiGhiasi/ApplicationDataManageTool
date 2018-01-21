using LightBuzz.Archiver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace AppDataManageTool
{
    class HiddenThings
    {
        public delegate void ProgressEventHandler(object sender, string message);
        public event ProgressEventHandler Progress;

        protected virtual void OnProgress(string message)
        {
            if (Progress != null)
                Progress(this, message);
        }

        public async Task BackupPath(string path, string output)
        {
            ArchiverPlus archiver = new ArchiverPlus();
            archiver.CompressingProgress += Archiver_CompressingProgress;
            
            await archiver.Compress(new StorageFolder[] { await StorageFolder.GetFolderFromPathAsync(path) }.ToList(),
                                    await (await StorageFolder.GetFolderFromPathAsync(System.IO.Path.GetDirectoryName(output))).CreateFileAsync(System.IO.Path.GetFileName(output)), System.IO.Compression.CompressionLevel.NoCompression);

            archiver.CompressingProgress -= Archiver_CompressingProgress;
        }

        private void Archiver_CompressingProgress(object sender, CompressingEventArgs e)
        {
            OnProgress("Copying...\r\n" + e.ProcessedFilesCount.ToString() + " files copied.");
        }
    }
}
