﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Testing
{
    internal sealed class TestMethodTestResultFileComposer : TestResultFileComposer
    {
        private readonly string _resultTestsDirectory;
        private readonly string _normalizedTestName;

        private TestMethodTestResultFileComposer(string directory, string resultTestsDirectory, string normalizedTestName, TestContext testContext) : base(directory, testContext)
        {
            _resultTestsDirectory = resultTestsDirectory;
            _normalizedTestName = normalizedTestName;
        }

        public static TestMethodTestResultFileComposer Create(TestContext testContext, string resultTestsDirectory)
        {
            string normalizedTestName = String.Join("_", TestContextUtility.GetTestName(testContext).Split(Path.GetInvalidFileNameChars()));
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
    }
}