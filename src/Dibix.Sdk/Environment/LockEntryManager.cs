using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk
{
    public sealed class LockEntryManager : IDisposable
    {
        private static readonly Assembly ThisAssembly = typeof(LockEntryManager).Assembly;
        private static readonly string ResourcePath = $"{typeof(LockEntryManager).Namespace}.Environment.lockfile";
        private readonly IDictionary<LockRecordKey, LockRecord> _map;
        private readonly bool _resetSuppressions;
        private readonly string _resetSuppressionFilePath;

        private LockEntryManager(LockStore store, bool resetSuppressions, string resetSuppressionFilePath)
        {
            this._map = BuildMap(store);
            this._resetSuppressions = resetSuppressions;
            this._resetSuppressionFilePath = resetSuppressionFilePath;
        }

        public static LockEntryManager Create()
        {
            string resetSuppressionFilePath = CollectResetSuppressionFilePath();
            bool resetSuppressions = !String.IsNullOrEmpty(resetSuppressionFilePath);
            LockStore store = Read(resetSuppressions, resetSuppressionFilePath);
            return new LockEntryManager(store, resetSuppressions, resetSuppressionFilePath);
        }

        public bool HasEntry(string sectionName, string recordName) => HasEntry(sectionName, groupName: null, recordName, signature: null);
        public bool HasEntry(string sectionName, string groupName, string recordName, string signature)
        {
            LockRecordKey key = new LockRecordKey(sectionName, groupName, recordName);
            if (this._map.TryGetValue(key, out LockRecord record))
            {
                if (record.Signature == signature)
                    return true;

                if (this._resetSuppressions)
                {
                    record.Signature = signature;
                    return true;
                }
            }
            else if (this._resetSuppressions)
            {
                this._map.Add(new LockRecordKey(sectionName, groupName, recordName), new LockRecord(recordName, signature));
                return true;
            }

            return false;
        }

        public void Write(string path, bool encoded)
        {
            LockStore store = new LockStore();
            foreach (var sectionGroup in this._map.GroupBy(x => x.Key.SectionName))
            {
                LockSection section = new LockSection(sectionGroup.Key);

                foreach (var groupGroup in sectionGroup.GroupBy(x => x.Key.GroupName))
                {
                    LockGroup group = new LockGroup(groupGroup.Key);
                    group.Records.AddRange(groupGroup.Select(x => x.Value));

                    section.Groups.Add(group);
                }

                store.Sections.Add(section);
            }

            if (encoded)
                WriteEncoded(store, path);
            else
                WritePlain(store, path);
        }

        public void Dispose() => this.Write();

        private static LockStore Read() => Read(OpenFromAssembly());
        private static LockStore Read(string filePath) => Read(OpenFromPath(filePath));
        private static LockStore Read(Stream stream)
        {
            using (stream)
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                byte[] bytes = (byte[])binaryFormatter.Deserialize(stream);

                using (MemoryStream memoryStream = new MemoryStream(bytes))
                {
                    using (Stream zipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                    {
                        using (TextReader textReader = new StreamReader(zipStream))
                        {
                            using (JsonReader jsonReader = new JsonTextReader(textReader))
                            {
                                JsonSerializer serializer = new JsonSerializer();
                                LockStore store = serializer.Deserialize<LockStore>(jsonReader);
                                return store;
                            }
                        }
                    }
                }
            }
        }
        private static LockStore Read(bool resetSuppressions, string resetSuppressionFilePath)
        {
            if (!resetSuppressions)
                return Read();

            if (File.Exists(resetSuppressionFilePath))
                return Read(resetSuppressionFilePath);

            return new LockStore();
        }

        private void Write()
        {
            if (!this._resetSuppressions)
                return;
                //throw new InvalidOperationException("This action is only supported when resetting suppressions");

            this.Write(this._resetSuppressionFilePath, encoded: true);
        }

        private static void WritePlain(LockStore store, string path)
        {
            JObject json = new JObject();

            foreach (LockSection section in store.Sections)
            {
                bool supportsGroup = section.Groups.Any(x => x.Name != null);
                JToken sectionValue;

                if (supportsGroup)
                {
                    JObject sectionJson = new JObject();
                    sectionValue = sectionJson;

                    foreach (LockGroup group in section.Groups)
                    {
                        JObject groupJson;
                        if (group.Name != null)
                        {
                            groupJson = new JObject();
                            sectionJson.Add(group.Name, groupJson);
                        }
                        else
                            groupJson = sectionJson;

                        foreach (LockRecord record in group.Records)
                        {
                            groupJson.Add(record.Name, record.Signature);
                        }
                    }
                }
                else
                {
                    JArray sectionJson = new JArray();
                    sectionValue = sectionJson;
                    
                    LockGroup group = section.Groups.SingleOrDefault();
                    if (group != null)
                    {
                        foreach (LockRecord record in group.Records)
                        {
                            sectionJson.Add(record.Name);
                        }
                    }
                }

                json.Add(section.Name, sectionValue);
            }

            using (Stream stream = File.Create(path))
            {
                using (TextWriter textWriter = new StreamWriter(stream))
                {
                    using (JsonWriter jsonWriter = new JsonTextWriter(textWriter) { Formatting = Formatting.Indented })
                    {
                        json.WriteTo(jsonWriter);
                    }
                }
            }
        }
        
        private static void WriteEncoded(LockStore store, string path)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (Stream zipStream = new GZipStream(memoryStream, CompressionMode.Compress))
                {
                    using (TextWriter textWriter = new StreamWriter(zipStream))
                    {
                        using (JsonWriter jsonWriter = new JsonTextWriter(textWriter))
                        {
                            JsonSerializer serializer = new JsonSerializer();
                            serializer.Serialize(jsonWriter, store);
                        }
                    }
                }

                using (Stream stream = File.Open(path, FileMode.Create))
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    binaryFormatter.Serialize(stream, memoryStream.ToArray());
                }
            }
        }

        private static Stream OpenFromAssembly() => ThisAssembly.GetManifestResourceStream(ResourcePath);
        
        private static Stream OpenFromPath(string path) => File.OpenRead(path);

        private static IDictionary<LockRecordKey, LockRecord> BuildMap(LockStore store)
        {
            var query = from section in store.Sections
                        from @group in section.Groups
                        from record in @group.Records
                        select new
                        {
                            Key = new LockRecordKey(section.Name, @group.Name, record.Name),
                            Value = record
                        };
            IDictionary<LockRecordKey, LockRecord> map = query.ToDictionary(x => x.Key, x => x.Value);
            return map;
        }

        private static string CollectResetSuppressionFilePath()
        {
            bool foundFlag = false;
            foreach (string arg in Environment.GetCommandLineArgs().Skip(3))
            {
                if (arg == "-s")
                {
                    foundFlag = true;
                    continue;
                }

                if (foundFlag)
                {
                    return arg;
                }
            }
            return null;
        }

        private readonly struct LockRecordKey
        {
            public string SectionName { get; }
            public string GroupName { get; }
            public string RecordName { get; }

            public LockRecordKey(string sectionName, string groupName, string recordName)
            {
                this.SectionName = sectionName;
                this.GroupName = groupName;
                this.RecordName = recordName;
            }
        }
    }

    internal sealed class LockStore
    {
        public ICollection<LockSection> Sections { get; }

        public LockStore()
        {
            this.Sections = new SortedSet<LockSection>(Comparer<LockSection>.Create((x, y) => String.CompareOrdinal(x.Name, y.Name)));
        }
    }

    internal sealed class LockSection
    {
        public string Name { get; }
        public ICollection<LockGroup> Groups { get; }

        public LockSection(string name)
        {
            this.Name = name;
            this.Groups = new SortedSet<LockGroup>(Comparer<LockGroup>.Create((x, y) => String.CompareOrdinal(x.Name, y.Name)));
        }
    }

    internal sealed class LockGroup
    {
        public string Name { get; }
        public ICollection<LockRecord> Records { get; }

        public LockGroup(string name)
        {
            this.Name = name;
            this.Records = new SortedSet<LockRecord>(Comparer<LockRecord>.Create((x, y) => String.CompareOrdinal(x.Name, y.Name)));
        }
    }

    internal sealed class LockRecord
    {
        public string Name { get; set; }
        public string Signature { get; set; }

        public LockRecord(string name, string signature)
        {
            this.Name = name;
            this.Signature = signature;
        }
    }
}