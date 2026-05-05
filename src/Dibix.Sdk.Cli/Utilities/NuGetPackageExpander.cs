using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace Dibix.Sdk.Cli
{
    internal static class NuGetPackageExpander
    {
        // OPC packaging entries that NuGet strips when populating the global cache.
        private static readonly string[] OpcPrefixes = ["_rels/", "package/"];
        private const string ContentTypesFile = "[Content_Types].xml";

        // Mirrors `nuget add <nupkg> -Source <cacheRoot> -Expand` for the v3 global packages folder layout (lowercase id + version directory, .nupkg copy, base64 SHA512 marker file, .nupkg.metadata file).
        public static void Expand(string packageName, string packageVersion, string nupkgPath, string cacheRoot)
        {
            using ZipArchive archive = ZipFile.OpenRead(nupkgPath);

            string idLower = packageName.ToLowerInvariant();
            string targetDirectory = Path.Combine(cacheRoot, idLower, packageVersion);
            Directory.CreateDirectory(targetDirectory);

            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                string fullName = entry.FullName;
                if (IsOpcEntry(fullName))
                    continue;

                string targetFileName = fullName;
                if (targetFileName == $"{packageName}.nuspec")
                    targetFileName = $"{idLower}.nuspec";

                string targetPath = Path.GetFullPath(Path.Combine(targetDirectory, targetFileName));
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                entry.ExtractToFile(targetPath, overwrite: true);
            }

            string nupkgFileName = $"{idLower}.{packageVersion}.nupkg";
            string nupkgFilePath = Path.Combine(targetDirectory, nupkgFileName);
            File.Copy(nupkgPath, nupkgFilePath, overwrite: true);

            // Hash marker — NuGet treats this file's existence as proof the package is installed.
            string contentHash = ComputeSha512Base64(nupkgPath);
            File.WriteAllText($"{nupkgFilePath}.sha512", contentHash);

            // .nupkg.metadata — NuGet 5.x+ writes this on every restore; absence triggers re-resolution.
            string nupkgMetadataPath = Path.Combine(targetDirectory, ".nupkg.metadata");
            string metadataJson = JsonConvert.SerializeObject(new
            {
                version = 2,
                contentHash,
                source = (string)null
            }, Formatting.Indented);
            File.WriteAllText(nupkgMetadataPath, metadataJson);
        }

        private static bool IsOpcEntry(string fullName)
        {
            if (String.Equals(fullName, ContentTypesFile, StringComparison.OrdinalIgnoreCase))
                return true;

            bool isOpcEntry = OpcPrefixes.Any(x => fullName.StartsWith(x, StringComparison.OrdinalIgnoreCase));
            return isOpcEntry;
        }

        private static string ComputeSha512Base64(string filePath)
        {
            using FileStream stream = File.OpenRead(filePath);
            using SHA512 sha = SHA512.Create();
            return Convert.ToBase64String(sha.ComputeHash(stream));
        }
    }
}