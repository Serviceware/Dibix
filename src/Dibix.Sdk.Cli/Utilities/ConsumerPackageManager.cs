using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using LibGit2Sharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.Cli
{
    internal sealed class ConsumerPackageManager
    {
        private readonly string _rootDirectory;
        private readonly string _cpmFile;
        private IReadOnlyDictionary<string, string> _packageVersions;

        public ConsumerPackageManager(string rootDirectory, string cpmFile)
        {
            _rootDirectory = rootDirectory;
            _cpmFile = cpmFile;
        }

        public static ConsumerPackageManager Load(string rootDirectory)
        {
            const string cpmFileName = "Directory.Packages.props";
            string cpmFile = Path.Combine(rootDirectory, cpmFileName);
            if (!File.Exists(cpmFile))
                throw new CommandLineValidationException($"Consumer repository root '{rootDirectory}' does define a '{cpmFileName}' file");

            return new ConsumerPackageManager(rootDirectory, cpmFile);
        }

        public async Task<string> GetPackageVersion(string packageName)
        {
            _packageVersions ??= await CollectPackageVersionVariablesFromMSBuild().ConfigureAwait(false);
            if (!_packageVersions.TryGetValue(packageName, out string packageVersion))
                throw new InvalidOperationException($"No package version registered in consumer for package '{packageName}'");

            return packageVersion;
        }

        public async Task SetPackageVersionMSBuild(string packageName, string version, CancellationToken cancellationToken)
        {
            PackageVersionPropertyXmlTarget result = await CollectPackageVersionFromMSBuildProperty(packageName, cancellationToken).ConfigureAwait(false);
            result.UpdateValue(version);
            await SavePreservingXmlDeclaration(result.Document, result.FilePath, cancellationToken).ConfigureAwait(false);
            _packageVersions = null;
        }

        public async Task SetPackageVersionGlobalJson(string packageName, string version)
        {
            string filePath = EnsureGlobalJsonPath(packageName);

            JObject root;
            const string propertyName = "msbuild-sdks";
            using (TextReader textReader = File.OpenText(filePath))
            {
#if NET
                await using (JsonReader jsonReader = new JsonTextReader(textReader))
#else
                using (JsonReader jsonReader = new JsonTextReader(textReader))
#endif
                {
                    root = await JObject.LoadAsync(jsonReader).ConfigureAwait(false);
                }
            }

            JProperty msBuildSdksProperty = root.Property(propertyName);
            if (msBuildSdksProperty == null)
                throw new InvalidOperationException($"Property '{propertyName}' not found at '{root.Path}' in '{filePath}'");

            JObject msBuildSdks = (JObject)msBuildSdksProperty.Value;
            JProperty packageProperty = msBuildSdks.Property(packageName);
            if (packageProperty == null)
                throw new InvalidOperationException($"Property '{packageName}' not found at '{msBuildSdks.Path}' in '{filePath}'");

            msBuildSdks[packageName] = version;

#if NET
            await using (TextWriter textWriter = File.CreateText(filePath))
#else
            using (TextWriter textWriter = File.CreateText(filePath))
#endif
            {
#if NET
                await using (JsonWriter jsonWriter = new JsonTextWriter(textWriter))
#else
                using (JsonWriter jsonWriter = new JsonTextWriter(textWriter))
#endif
                {
                    jsonWriter.Formatting = Newtonsoft.Json.Formatting.Indented;
                    await root.WriteToAsync(jsonWriter).ConfigureAwait(false);
                }
            }
        }

        public async Task<string> GetPackageVersionTargetFileMSBuild(string packageName, CancellationToken cancellationToken)
        {
            PackageVersionPropertyXmlTarget result = await CollectPackageVersionFromMSBuildProperty(packageName, cancellationToken).ConfigureAwait(false);
            return result.FilePath;
        }

        public async Task RevertPackageVersionChanges(string packageName, bool isSdk, CancellationToken cancellationToken)
        {
            PackageVersionPropertyXmlTarget result = await CollectPackageVersionFromMSBuildProperty(packageName, cancellationToken).ConfigureAwait(false);
            Repository repository = new Repository(_rootDirectory);
            repository.RevertFile(result.FilePath);

            if (isSdk)
                repository.RevertFile(EnsureGlobalJsonPath(packageName));
        }

        private async Task<IReadOnlyDictionary<string, string>> CollectPackageVersionVariablesFromMSBuild() => await CollectPackageVersionVariablesFromMSBuildCore().ToDictionaryAsync().ConfigureAwait(false);
        private async IAsyncEnumerable<(string PackageName, string PackageVersion)> CollectPackageVersionVariablesFromMSBuildCore()
        {
            string standardOutput = await ProcessUtility.Capture(fileName: "dotnet", arguments: $"msbuild -getItem:PackageVersion \"{_cpmFile}\"").ConfigureAwait(false);
            JObject json = JObject.Parse(standardOutput);
            foreach (JObject item in json.SelectTokens("$.Items.PackageVersion[?(@.Identity =~ /^Dibix/)]").Cast<JObject>())
            {
                string packageName = GetRequiredJsonPropertyValue(item, "Identity");
                string packageVersion = GetRequiredJsonPropertyValue(item, "Version");
                yield return (packageName, packageVersion);
            }
        }

        private static string GetRequiredJsonPropertyValue(JObject container, string propertyName)
        {
            JProperty property = container.Property(propertyName);
            if (property == null)
                throw new InvalidOperationException($"Missing property '{propertyName}' at '{container.Path}'");

            return (string)property.Value;
        }

        private static async Task<PackageVersionPropertyXmlTarget> TryFindPropertyInImports(string filePath, string propertyName, HashSet<string> visited, CancellationToken cancellationToken)
        {
            string fullPath = Path.GetFullPath(filePath);
            if (!visited.Add(fullPath))
                return null;

            XDocument document = await LoadPreservingWhitespace(filePath, cancellationToken).ConfigureAwait(false);

            XElement target = document.XPathSelectElement($"/Project/PropertyGroup/{propertyName}");
            if (target != null)
                return new PackageVersionPropertyXmlTarget<XElement>(target, filePath, (x, y) => x.Value = y);

            string thisDir = $"{Path.GetDirectoryName(fullPath)}{Path.DirectorySeparatorChar}";
            foreach (XElement import in document.XPathSelectElements("/Project/Import"))
            {
                XAttribute projectAttribute = import.Attribute("Project");
                if (projectAttribute == null)
                    throw new InvalidOperationException($"Import tag missing 'Project' attribute at '{filePath}' line {((IXmlLineInfo)import).LineNumber}");

                string importPath = Path.GetFullPath(projectAttribute.Value.Replace("$(MSBuildThisFileDirectory)", thisDir));

                foreach (string imported in ExpandGlob(importPath))
                {
                    PackageVersionPropertyXmlTarget result = await TryFindPropertyInImports(imported, propertyName, visited, cancellationToken).ConfigureAwait(false);
                    if (result != null)
                        return result;
                }
            }

            return null;
        }

        private async Task<PackageVersionPropertyXmlTarget> CollectPackageVersionFromMSBuildProperty(string packageName, CancellationToken cancellationToken)
        {
            XDocument document = await LoadPreservingWhitespace(_cpmFile, cancellationToken).ConfigureAwait(false);

            XAttribute versionAttribute = ((IEnumerable<object>)document.XPathEvaluate($"/Project/ItemGroup/PackageVersion[@Include='{packageName}']/@Version")).Cast<XAttribute>().FirstOrDefault();
            if (versionAttribute == null)
                throw new InvalidOperationException($"<PackageVersion Include=\"{packageName}\"> not found in '{_cpmFile}'");

            PackageVersionPropertyXmlTarget result;
            if (TryGetSinglePropertyReference(versionAttribute.Value, out string propertyName))
            {
                // Indirect form: <PackageVersion Version="$(X)" /> — locate the defining .props file.
                HashSet<string> visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                result = await TryFindPropertyInImports(_cpmFile, propertyName, visited, cancellationToken).ConfigureAwait(false);
                if (result == null)
                    throw new InvalidOperationException($"Property '{propertyName}' (referenced by '{packageName}') is not defined in any .props file imported from '{_cpmFile}'");
            }
            else
            {
                // Literal form: <PackageVersion Version="1.7.53" /> — edit in place.
                result = new PackageVersionPropertyXmlTarget<XAttribute>(versionAttribute, _cpmFile, (x, y) => x.Value = y);
            }

            return result;
        }

        private string EnsureGlobalJsonPath(string packageName)
        {
            const string fileName = "global.json";
            string path = Path.Combine(_rootDirectory, fileName);
            if (!File.Exists(path))
                throw new InvalidOperationException($"Expected to find a '{fileName}' file at '{_rootDirectory}' to update SDK version of '{packageName}', but it does not exist");

            return path;
        }

        private static IEnumerable<string> ExpandGlob(string pathOrPattern)
        {
            string fileName = Path.GetFileName(pathOrPattern);
            if (fileName.IndexOfAny(['*', '?']) >= 0)
            {
                string dir = Path.GetDirectoryName(pathOrPattern)!;

                foreach (string match in Directory.EnumerateFiles(dir, fileName))
                    yield return match;
            }
            else if (File.Exists(pathOrPattern))
            {
                yield return pathOrPattern;
            }
        }

#if NET
        private static async Task<XDocument> LoadPreservingWhitespace(string path, CancellationToken cancellationToken)
#else
        private static Task<XDocument> LoadPreservingWhitespace(string path, CancellationToken cancellationToken)
#endif
        {
            XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();
#if NET
            xmlReaderSettings.Async = true;
#endif
            using TextReader textReader = File.OpenText(path);
            using XmlReader xmlReader = XmlReader.Create(textReader, xmlReaderSettings);
#if NET
            XDocument document = await XDocument.LoadAsync(xmlReader, LoadOptions.PreserveWhitespace, cancellationToken).ConfigureAwait(false);
            return document;
#else
            XDocument document = XDocument.Load(xmlReader, LoadOptions.PreserveWhitespace);
            return Task.FromResult(document);
#endif
        }

#if NET
        private static async Task SavePreservingXmlDeclaration(XDocument document, string filePath, CancellationToken cancellationToken)
#else
        private static Task SavePreservingXmlDeclaration(XDocument document, string filePath, CancellationToken cancellationToken)
#endif
        {
            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings{ OmitXmlDeclaration = document.Declaration == null };
#if NET
            await using TextWriter textWriter = File.CreateText(filePath);
#else
            using TextWriter textWriter = File.CreateText(filePath);
#endif
#if NET
            xmlWriterSettings.Async = true;
            await using XmlWriter xmlWriter = XmlWriter.Create(textWriter, xmlWriterSettings);
#else
            using XmlWriter xmlWriter = XmlWriter.Create(textWriter, xmlWriterSettings);
#endif
#if NET
            await document.SaveAsync(xmlWriter, cancellationToken).ConfigureAwait(false);
#else
            document.Save(xmlWriter);
            return Task.CompletedTask;
#endif
        }

        private static bool TryGetSinglePropertyReference(string raw, out string propertyName)
        {
            Match match = Regex.Match(raw, @"^\$\((?<PropertyName>[^)]+)\)$");
            if (match.Success)
            {
                propertyName = match.Groups["PropertyName"].Value;
                return true;
            }

            propertyName = null;
            return false;
        }

        private sealed class PackageVersionPropertyXmlTarget<TNode> : PackageVersionPropertyXmlTarget where TNode : XObject
        {
            private readonly TNode _node;
            private readonly Action<TNode, string> _valueSetter;

            public PackageVersionPropertyXmlTarget(TNode node, string filePath, Action<TNode, string> valueSetter) : base(node, filePath)
            {
                _node = node;
                _valueSetter = valueSetter;
            }

            public override void UpdateValue(string value)
            {
                _valueSetter(_node, value);
            }
        }

        private abstract class PackageVersionPropertyXmlTarget
        {
            private readonly XObject _node;

            public XDocument Document => _node.Document;
            public string FilePath { get; }

            public PackageVersionPropertyXmlTarget(XObject node, string filePath)
            {
                _node = node;
                FilePath = filePath;
            }

            public abstract void UpdateValue(string value);
        }
    }
}