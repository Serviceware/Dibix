using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Testing
{
    internal sealed class TestRunTestResultFileComposer : TestResultFileComposer
    {
        private static readonly ConcurrentDictionary<string, Lazy<TestRunTestResultFileComposer>> Cache = new ConcurrentDictionary<string, Lazy<TestRunTestResultFileComposer>>();
        private static readonly ConcurrentDictionary<string, object> FileCopyLocks = new ConcurrentDictionary<string, object>();
        private readonly string _defaultRunDirectory;
        private readonly bool _useDedicatedTestResultsDirectory;
        private readonly ICollection<string> _registeredFileNames;

        private TestRunTestResultFileComposer(string directory, string defaultRunDirectory, bool useDedicatedTestResultsDirectory) : base(directory)
        {
            _defaultRunDirectory = defaultRunDirectory;
            _useDedicatedTestResultsDirectory = useDedicatedTestResultsDirectory;
            _registeredFileNames = new HashSet<string>();
        }

        public static TestRunTestResultFileComposer Resolve(TestContext testContext, bool useDedicatedTestResultsDirectory)
        {
            // Unfortunately, MSTest does not provide a unique id to identity a test run, therefore we use the generated test run directory from MSTest
            string testRunIdentifier = testContext.TestRunDirectory;

            // Use Lazy<T> to ensure the test run attachments are only written to disk once when running tests in parallel
            Lazy<TestRunTestResultFileComposer> value = Cache.GetOrAdd(testRunIdentifier, _ => new Lazy<TestRunTestResultFileComposer>(() => Create(testContext, useDedicatedTestResultsDirectory)));
            bool isFirstTest = !value.IsValueCreated;
            TestRunTestResultFileComposer instance = value.Value;

            // Make test run attachments available for each test method
            if (!isFirstTest)
                instance.ImportResultFiles(testContext);

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

        private void Initialize(TestContext testContext)
        {
            EnsureEnvironmentDump(testContext);
        }

        private void ImportResultFiles(TestContext testContext)
        {
            foreach (string resultFile in ResultFiles)
                testContext.AddResultFile(resultFile);
        }

        private void EnsureEnvironmentDump(TestContext testContext)
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
            AddResultFile("Environment.txt", environment, testContext);
        }

        private void CopyFiles(IEnumerable<string> files, string targetDirectory)
        {
            foreach (string file in files)
            {
                string relativeFilePath = file.Substring(ResultDirectory.Length + 1);
                string targetFilePath = Path.Combine(targetDirectory, relativeFilePath);
                EnsureDirectory(targetFilePath);

                bool isRunFile = ResultFiles.Contains(file);

                // There are two ways MSTest executes tests in multiple assemblies during one run:
                // 1. Each assembly is started in a dedicated process leading to a dedicated run directory
                // 2. Each assembly is started in the same process, but a different app domain, reusing the same run directory
                // Using the first strategy we could make use of our cached test run file composer instance and make note of already deployed test run files.
                // With the second strategy however, our test run file composer cache is empty again when the first test in the second assembly is run,
                // but since the test run directory is reused, files might already be present in the run directory.
                // Therefore, we accept this behavior and skip existing run files either way
                if (isRunFile && File.Exists(targetFilePath))
                    continue;

                // Use file-specific lock to prevent concurrent copies of the same file when running tests in parallel
                object lockObject = FileCopyLocks.GetOrAdd(targetFilePath, _ => new object());
                lock (lockObject)
                {
                    // Double-check after acquiring lock - another thread may have copied it
                    if (File.Exists(targetFilePath))
                        continue;

                    File.Copy(file, targetFilePath);
                }
            }
        }

        private static TestRunTestResultFileComposer Create(TestContext testContext, bool useDedicatedTestResultsDirectory)
        {
            string defaultRunDirectory = testContext.TestRunResultsDirectory;
            string runDirectory = useDedicatedTestResultsDirectory ? CreateTestRunDirectory(testContext) : defaultRunDirectory;
            TestRunTestResultFileComposer instance = new TestRunTestResultFileComposer(runDirectory, defaultRunDirectory, useDedicatedTestResultsDirectory);
            instance.Initialize(testContext);
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
    }
}