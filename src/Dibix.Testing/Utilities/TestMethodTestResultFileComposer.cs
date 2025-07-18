using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Dibix.Testing
{
    internal sealed class TestMethodTestResultFileComposer : TestResultFileComposer
    {
        private readonly string _resultTestsDirectory;
        private readonly string _normalizedTestName;
        private readonly TestContext _testContext;

        private TestMethodTestResultFileComposer(string directory, string resultTestsDirectory, string normalizedTestName, TestContext testContext) : base(directory)
        {
            _resultTestsDirectory = resultTestsDirectory;
            _normalizedTestName = normalizedTestName;
            _testContext = testContext;
        }

        public static TestMethodTestResultFileComposer Create(TestContext testContext, string resultTestsDirectory)
        {
            string testName = testContext.TestDisplayName ?? testContext.TestName ?? throw new InvalidOperationException("TestDisplayName and TestName are null");
            string normalizedTestName = String.Join("_", testName.Split(Path.GetInvalidFileNameChars()));
            string testDirectory = Path.Combine(resultTestsDirectory, normalizedTestName);
            TestMethodTestResultFileComposer instance = new TestMethodTestResultFileComposer(testDirectory, resultTestsDirectory, normalizedTestName, testContext);
            return instance;
        }

        public void ZipTestOutput(string resultRunDirectory, IEnumerable<string> resultFiles)
        {
            string path = Path.Combine(_resultTestsDirectory, $"{_normalizedTestName}.zip");
            EnsureDirectory(path);
            using (ZipArchive archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                foreach (string file in resultFiles)
                {
                    int relativePathIndex = resultRunDirectory.Length + 1;
                    string relativePath = file.Substring(relativePathIndex, file.Length - relativePathIndex);
                    archive.CreateEntryFromFile(file, relativePath);
                }
            }
            RegisterResultFile(path);
        }

        public void RegisterTextContextInfo(TestContext testContext) => AddResultFile("TestContext.json", JsonConvert.SerializeObject(testContext, new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ContractResolver = new TestContextContractResolver()
        }), testContext);

        public string AddResultFile(string fileName) => AddResultFile(fileName, _testContext);

        public string AddResultFile(string fileName, string content) => AddResultFile(fileName, content, _testContext);

        public string ImportResultFile(string filePath) => ImportResultFile(filePath, _testContext);

        public void RegisterResultFile(string path) => RegisterResultFile(path, _testContext);

        private sealed class TestContextContractResolver : DefaultContractResolver, IContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                JsonProperty property = base.CreateProperty(member, memberSerialization);

                JsonProperty result = member.DeclaringType?.FullName switch
                {
                    // Self referencing loop detected for property 'Context' with type 'Microsoft.VisualStudio.TestPlatform.MSTestAdapter.PlatformServices.TestContextImplementation'. Path ''.
                    "Microsoft.VisualStudio.TestPlatform.MSTestAdapter.PlatformServices.TestContextImplementation" when property.PropertyName == "Context" => null,

                    // We don't need this property for debugging purposes
                    "Microsoft.VisualStudio.TestTools.UnitTesting.TestContext" when property.PropertyName == "CancellationTokenSource" => null,

                    _ => property
                };

                return result;
            }
        }
    }
}