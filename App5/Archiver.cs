﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Compression;
using Windows.Storage.Search;
using Windows.Storage.Streams;

namespace LightBuzz.Archiver
{
    /// <summary>
    /// Compresses and decompresses single files and folders.
    /// </summary>
    public class Archiver
    {
        /// <summary>
        /// Compresses a folder, including all of its files and sub-folders.
        /// </summary>
        /// <param name="source">The folder containing the files to compress.</param>
        /// <param name="destination">The compressed zip file.</param>
        public async void Compress(StorageFolder source, StorageFile destination)
        {
            using (Stream stream = await destination.OpenStreamForWriteAsync())
            {
                using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create))
                {
                    await AddFolderToArchive(source, archive, "");
                }
            }
        }

        /// <summary>
        /// Compresses a single file.
        /// </summary>
        /// <param name="source">The file to compress.</param>
        /// <param name="destination">The compressed zip file.</param>
        public async void Compress(StorageFile source, StorageFile destination)
        {
            using (Stream stream = await destination.OpenStreamForWriteAsync())
            {
                using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create))
                {
                    ZipArchiveEntry entry = archive.CreateEntry(source.Name);

                    using (Stream data = entry.Open())
                    {
                        byte[] buffer = await ConvertToBinary(source);
                        data.Write(buffer, 0, buffer.Length);
                    }
                }
            }
        }

        /// <summary>
        /// Decompresses the specified file to the specified folder.
        /// </summary>
        /// <param name="source">The compressed zip file.</param>
        /// <param name="destination">The folder where the file will be decompressed.</param>
        public async void Decompress(StorageFile source, StorageFolder destination)
        {
            using (Stream stream = await source.OpenStreamForReadAsync())
            {
                using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (!string.IsNullOrEmpty(entry.FullName))
                        {
                            if (!entry.FullName.EndsWith("/"))
                            {
                                string fileName = entry.FullName.Replace("/", "\\");

                                using (Stream entryStream = entry.Open())
                                {
                                    byte[] buffer = new byte[entry.Length];
                                    entryStream.Read(buffer, 0, buffer.Length);

                                    try
                                    {
                                        StorageFile file = await destination.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

                                        using (IRandomAccessStream uncompressedFileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                                        {
                                            using (Stream data = uncompressedFileStream.AsStreamForWrite())
                                            {
                                                data.Write(buffer, 0, buffer.Length);
                                                data.Flush();
                                            }
                                        }
                                    }
                                    catch
                                    {
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds the specified folder, along with its files and sub-folders, to the specified archive.
        /// Creadits to Jin Yanyun
        /// http://www.rapidsnail.com/Tutorial/t/2012/116/40/23786/windows-and-development-winrt-to-zip-files-unzip-and-folder-zip-compression.aspx
        /// </summary>
        /// <param name="folder">The folder to add.</param>
        /// <param name="archive">The zip archive.</param>
        /// <param name="separator">The directory separator character.</param>
        private async Task AddFolderToArchive(StorageFolder folder, ZipArchive archive, string separator)
        {
            foreach (StorageFile file in await folder.GetFilesAsync())
            {
                ZipArchiveEntry entry = archive.CreateEntry(separator + file.Name);

                using (Stream stream = entry.Open())
                {
                    byte[] buffer = await ConvertToBinary(file);
                    stream.Write(buffer, 0, buffer.Length);
                }
            }

            foreach (var storageFolder in await folder.GetFoldersAsync())
            {
                await AddFolderToArchive(storageFolder, archive, separator + storageFolder.Name + "/");
            }
        }

        /// <summary>
        /// Converts the specified file to a byte array.
        /// </summary>
        /// <param name="storageFile">The file to convert.</param>
        /// <returns>A byte array representation of the file.</returns>
        private async Task<byte[]> ConvertToBinary(StorageFile storageFile)
        {
            IRandomAccessStreamWithContentType stream = await storageFile.OpenReadAsync();

            using (DataReader reader = new DataReader(stream))
            {
                byte[] buffer = new byte[stream.Size];

                await reader.LoadAsync((uint)stream.Size);
                reader.ReadBytes(buffer);

                return buffer;
            }
        }
    }
}
