using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Testing
{
    public abstract class TestResultFileComposer
    {
        private readonly TestContext _testContext;
        
        public ICollection<string> ResultFiles { get; }
        public string ResultDirectory { get; }

        protected TestResultFileComposer(string directory, TestContext testContext)
        {
            ResultDirectory = directory;
            ResultFiles = new List<string>();
            _testContext = testContext;
        }

        public string AddResultFile(string fileName)
        {
            string path = Path.Combine(ResultDirectory, fileName);
            EnsureDirectory(path);
            RegisterResultFile(path);
            OnFileNameRegistered(fileName);
            return path;
        }

        public string AddResultFile(string fileName, string content)
        {
            string path = AddResultFile(fileName);
            WriteContentToFile(path, content);
            return path;
        }

        public string ImportResultFile(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            string targetPath = AddResultFile(fileName);
            File.Copy(filePath, targetPath);
            return targetPath;
        }

        public void RegisterResultFile(string path)
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
            _testContext.AddResultFile(path);
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