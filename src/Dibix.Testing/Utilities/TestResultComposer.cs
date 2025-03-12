using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Testing
{
    public abstract class TestResultFileComposer
    {
        public ICollection<string> ResultFiles { get; }
        public string ResultDirectory { get; }

        protected TestResultFileComposer(string directory)
        {
            ResultDirectory = directory;
            ResultFiles = new List<string>();
        }

        public string AddResultFile(string fileName, TestContext testContext)
        {
            string path = Path.Combine(ResultDirectory, fileName);
            EnsureDirectory(path);
            RegisterResultFile(path, testContext);
            OnFileNameRegistered(fileName);
            return path;
        }

        public string AddResultFile(string fileName, string content, TestContext testContext)
        {
            string path = AddResultFile(fileName, testContext);
            WriteContentToFile(path, content);
            return path;
        }

        public string ImportResultFile(string filePath, TestContext testContext)
        {
            string fileName = Path.GetFileName(filePath);
            string targetPath = AddResultFile(fileName, testContext);
            File.Copy(filePath, targetPath);
            return targetPath;
        }

        public void RegisterResultFile(string path, TestContext testContext)
        {
            if (path.Length > 255)
            {
                throw new ArgumentException($"""
                                             Test result file path too long: {path}
                                             Allowed path length 255: {path.Substring(0, 255)}
                                             """, nameof(path));
            }

            if (ResultFiles.Contains(path))
                throw new InvalidOperationException($"Test result file already registered: {path}");

            ResultFiles.Add(path);
            testContext.AddResultFile(path);
        }

        public static void WriteContentToFile(string path, string content)
        {
            if (File.Exists(path))
                throw new InvalidOperationException($"Path exists already: {path}");

            EnsureDirectory(path);
            File.WriteAllText(path, content);
        }

        protected virtual void OnFileNameRegistered(string fileName) { }

        protected static void EnsureDirectory(string path)
        {
            string directory = Path.GetDirectoryName(path);
            Directory.CreateDirectory(directory);
        }
    }
}