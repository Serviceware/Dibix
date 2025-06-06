﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Dibix.Sdk.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk
{
    public sealed class LockEntryManager : ILockEntryManager, IDisposable
    {
        private readonly IDictionary<LockRecordKey, LockRecord> _recordMap;
        private readonly IDictionary<string, string> _filePathSignatureMap;
        private readonly bool _reset;
        private readonly string _filePath;

        private LockEntryManager(LockStore store, bool reset, string filePath)
        {
            _recordMap = BuildMap(store);
            _filePathSignatureMap = new Dictionary<string, string>();
            _reset = reset;
            _filePath = filePath;
        }

        public static LockEntryManager Create(bool reset, string filePath)
        {
            LockStore store = CreateStore(filePath);
            return new LockEntryManager(store, reset, filePath);
        }

        public bool HasEntry(string sectionName, string recordName) => HasEntry(sectionName, groupName: null, recordName, filePath: null);
        public bool HasEntry(string sectionName, string recordName, string filePath) => HasEntry(sectionName, groupName: null, recordName, filePath);
        public bool HasEntry(string sectionName, string groupName, string recordName, string filePath)
        {
            string signature = GetSignature(filePath);
            LockRecordKey key = new LockRecordKey(sectionName, groupName, recordName);
            if (_recordMap.TryGetValue(key, out LockRecord record))
            {
                if (record.Signature == signature)
                    return true;

                if (_reset)
                {
                    record.Signature = signature;
                    return true;
                }
            }
            else if (_reset)
            {
                _recordMap.Add(new LockRecordKey(sectionName, groupName, recordName), new LockRecord(recordName, signature));
                return true;
            }

            return false;
        }

        public void Write(string path, bool encoded)
        {
            LockStore store = new LockStore();
            foreach (var sectionGroup in _recordMap.GroupBy(x => x.Key.SectionName))
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

        public void Dispose() => Write();

        private string GetSignature(string filePath)
        {
            if (filePath == null)
                return null;

            if (!_filePathSignatureMap.TryGetValue(filePath, out string signature))
            {
                signature = CalculateHash(filePath);
                _filePathSignatureMap.Add(filePath, signature);
            }

            return signature;
        }

        private static LockStore CreateStore(string suppressionFilePath)
        {
            if (File.Exists(suppressionFilePath))
                return Read(suppressionFilePath);

            return new LockStore();
        }

        private static LockStore Read(string filePath) => Read(OpenFromPath(filePath));
        private static LockStore Read(Stream stream)
        {
            using (stream)
            {
                using (BinaryReader binaryReader = new BinaryReader(stream))
                {
                    int length = binaryReader.ReadInt32();
                    byte[] bytes = binaryReader.ReadBytes(length);

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
        }

        private void Write()
        {
            if (!_reset)
                return; //throw new InvalidOperationException("This action is only supported when resetting suppressions");

            Write(_filePath, encoded: true);
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

                byte[] bytes = memoryStream.ToArray();
                using (Stream stream = File.Open(path, FileMode.Create))
                {
                    using (BinaryWriter binaryWriter = new BinaryWriter(stream))
                    {
                        binaryWriter.Write(bytes.Length);
                        binaryWriter.Write(bytes);
                    }
                }
            }
        }

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

        private static string CalculateHash(string filename)
        {
            using (MD5 md5 = MD5.Create())
            {
                string input = String.Join("\n", File.ReadAllLines(filename));
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        private readonly struct LockRecordKey
        {
            public string SectionName { get; }
            public string GroupName { get; }
            public string RecordName { get; }

            public LockRecordKey(string sectionName, string groupName, string recordName)
            {
                SectionName = sectionName;
                GroupName = groupName;
                RecordName = recordName;
            }
        }
    }

    internal sealed class LockStore
    {
        public ICollection<LockSection> Sections { get; }

        public LockStore()
        {
            Sections = new SortedSet<LockSection>(Comparer<LockSection>.Create((x, y) => String.CompareOrdinal(x.Name, y.Name)));
        }
    }

    internal sealed class LockSection
    {
        public string Name { get; }
        public ICollection<LockGroup> Groups { get; }

        public LockSection(string name)
        {
            Name = name;
            Groups = new SortedSet<LockGroup>(Comparer<LockGroup>.Create((x, y) => String.CompareOrdinal(x.Name, y.Name)));
        }
    }

    internal sealed class LockGroup
    {
        public string Name { get; }
        public ICollection<LockRecord> Records { get; }

        public LockGroup(string name)
        {
            Name = name;
            Records = new SortedSet<LockRecord>(Comparer<LockRecord>.Create((x, y) => String.CompareOrdinal(x.Name, y.Name)));
        }
    }

    internal sealed class LockRecord
    {
        public string Name { get; set; }
        public string Signature { get; set; }

        public LockRecord(string name, string signature)
        {
            Name = name;
            Signature = signature;
        }
    }
}