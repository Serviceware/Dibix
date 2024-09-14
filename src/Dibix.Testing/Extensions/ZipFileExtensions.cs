using System;
using System.IO;
using System.IO.Compression;

namespace Dibix.Testing
{
    /// <remarks>
    /// Rewritten from .NET implementation to open with FileShare.ReadWrite instead of FileShare.Read.
    /// Without it, it's not possible to zip a file that is currently being written to.
    /// </remarks>
    internal static partial class ZipFileExtensions
    {
        public static ZipArchiveEntry CreateEntryFromFile(this ZipArchive destination, string sourceFileName, string entryName) => DoCreateEntryFromFile(destination, sourceFileName, entryName, null);

        private static ZipArchiveEntry DoCreateEntryFromFile(this ZipArchive destination, string sourceFileName, string entryName, CompressionLevel? compressionLevel)
        {
            // Checking of compressionLevel is passed down to DeflateStream and the IDeflater implementation
            // as it is a pluggable component that completely encapsulates the meaning of compressionLevel.

            // Argument checking gets passed down to FileStream's ctor and CreateEntry

            using FileStream fs = new FileStream(sourceFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 0x1000, useAsync: false);
            ZipArchiveEntry entry = compressionLevel.HasValue ? destination.CreateEntry(entryName, compressionLevel.Value): destination.CreateEntry(entryName);

            DateTime lastWrite = File.GetLastWriteTime(sourceFileName);

            // If file to be archived has an invalid last modified time, use the first datetime representable in the Zip timestamp format
            // (midnight on January 1, 1980):
            if (lastWrite.Year is < 1980 or > 2107)
                lastWrite = new DateTime(1980, 1, 1, 0, 0, 0);

            entry.LastWriteTime = lastWrite;

            SetExternalAttributes(fs, entry);

            using Stream es = entry.Open();
            fs.CopyTo(es);

            return entry;
        }

        static partial void SetExternalAttributes(FileStream fs, ZipArchiveEntry entry);
    }
}