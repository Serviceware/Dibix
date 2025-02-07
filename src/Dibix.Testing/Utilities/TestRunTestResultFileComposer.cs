using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Dibix.Testing
{
    internal sealed class TestRunTestResultFileComposer : TestResultFileComposer
    {
        private static readonly ConcurrentDictionary<string, Lazy<TestRunTestResultFileComposer>> Cache = new ConcurrentDictionary<string, Lazy<TestRunTestResultFileComposer>>();
        private readonly TestContext _testContext;
        private readonly string _defaultRunDirectory;
        private readonly bool _useDedicatedTestResultsDirectory;
        private readonly ICollection<string> _deployedFiles;
        private readonly ICollection<string> _registeredFileNames;

        private TestRunTestResultFileComposer(string directory, TestContext testContext, string defaultRunDirectory, bool useDedicatedTestResultsDirectory) : base(directory, testContext)
        {
            _testContext = testContext;
            _defaultRunDirectory = defaultRunDirectory;
            _deployedFiles = new HashSet<string>();
            _useDedicatedTestResultsDirectory = useDedicatedTestResultsDirectory;
            _registeredFileNames = new HashSet<string>();
        }

        public static TestRunTestResultFileComposer Resolve(TestContext testContext, bool useDedicatedTestResultsDirectory)
        {
            // Unfortunately, MSTest does not provide a unique id to identity a test run, therefore we use the generated test run directory from MSTest
            string testRunIdentifier = testContext.TestRunDirectory;

            // Use Lazy<T> to ensure the test run attachments are only written to disk once when running tests in parallel
            TestRunTestResultFileComposer instance = Cache.GetOrAdd(testRunIdentifier, _ => new Lazy<TestRunTestResultFileComposer>(() => Create(testContext, useDedicatedTestResultsDirectory))).Value;

            // Make test run attachments available for each test method
            instance.ImportResultFilesIfNecessary(testContext);

            return instance;
        }

        public void CopyTestOutput(IEnumerable<string> resultFiles)
        {
            if (!_useDedicatedTestResultsDirectory)
                return;

            CopyFiles(resultFiles, _defaultRunDirectory);
        }

        public bool IsFileNameRegistered(string fileName) => _registeredFileNames.Contains(fileName);

        protected override void OnFileNameRegistered(string fileName) => _registeredFileNames.Add(fileName);

        private void Initialize()
        {
            EnsureTestContextDump();
            EnsureEnvironmentDump();
        }

        private void ImportResultFilesIfNecessary(TestContext currentTestContext)
        {
            if (currentTestContext == _testContext)
                return;

            foreach (string resultFile in ResultFiles)
                currentTestContext.AddResultFile(resultFile);
        }

        private void EnsureTestContextDump() => AddResultFile("TestContext.json", JsonConvert.SerializeObject(_testContext, new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ContractResolver = new TestContextContractResolver()
        }));

        private void EnsureEnvironmentDump()
        {
            IDictionary<string, object> environmentVariables = Environment.GetEnvironmentVariables()
                                                                          .Cast<DictionaryEntry>()
                                                                          .ToDictionary(x => (string)x.Key, x => x.Value);
            int maxKeyLength = environmentVariables.Keys.Max(x => x.Length);

            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, object> environmentVariable in environmentVariables.OrderBy(x => x.Key))
            {
                sb.Append(environmentVariable.Key.PadRight(maxKeyLength));
                sb.Append(" = ");
                sb.Append(environmentVariable.Value);
                sb.AppendLine();
            }

            string environment = sb.ToString();
            AddResultFile("Environment.txt", environment);
        }

        private void CopyFiles(IEnumerable<string> files, string targetDirectory)
        {
            foreach (string file in files)
            {
                string relativeFilePath = file.Substring(ResultDirectory.Length + 1);
                string targetFilePath = Path.Combine(targetDirectory, relativeFilePath);
                EnsureDirectory(targetFilePath);

                if (_deployedFiles.Contains(targetFilePath))
                    continue;

                File.Copy(file, targetFilePath);

                bool isRunFile = ResultFiles.Contains(file);
                if (isRunFile)
                    _deployedFiles.Add(targetFilePath);
            }
        }

        private static TestRunTestResultFileComposer Create(TestContext testContext, bool useDedicatedTestResultsDirectory)
        {
            string defaultRunDirectory = testContext.TestRunResultsDirectory;
            string runDirectory = useDedicatedTestResultsDirectory ? CreateTestRunDirectory(testContext) : defaultRunDirectory;
            TestRunTestResultFileComposer instance = new TestRunTestResultFileComposer(runDirectory, testContext, defaultRunDirectory, useDedicatedTestResultsDirectory);
            instance.Initialize();
            return instance;
        }

        private static string CreateTestRunDirectory(TestContext testContext)
        {
            Assembly assembly = TestImplementationResolver.ResolveTestAssembly(testContext);
            string assemblyName = assembly.GetName().Name!;
            string directoryName = $"Run_{DateTime.Now:yyyy-MM-dd HH_mm_ss}";
            string path = Path.Combine(Path.GetTempPath(), "TestResults", directoryName, assemblyName);
            return path;
        }

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