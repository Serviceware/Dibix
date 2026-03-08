using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Dibix.Sdk.Tests
{
    internal static class DatabaseTestUtility
    {
        private static readonly XDocument DatabaseProject;
        private static readonly XmlNamespaceManager DatabaseProjectNamespaceManager;

        public static string ProjectName => "Dibix.Sdk.Tests.Database";
        public static string ProjectDirectory { get; } = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "..", "..", ".."));
        public static string DatabaseProjectDirectory { get; } = Path.GetFullPath(Path.Combine(ProjectDirectory, "..", ProjectName));
        public static string DatabaseProjectPath { get; } = Path.Combine(DatabaseProjectDirectory, $"{ProjectName}.sqlproj");
        public static string DatabaseSchemaProviderName { get; }
        public static string ModelCollation { get; }

        static DatabaseTestUtility()
        {
            DatabaseProject = XDocument.Load(DatabaseProjectPath);
            DatabaseProjectNamespaceManager = new XmlNamespaceManager(new NameTable());
            DatabaseProjectNamespaceManager.AddNamespace("x", "http://schemas.microsoft.com/developer/msbuild/2003");

            DatabaseSchemaProviderName = (string)DatabaseProject.XPathEvaluate("string(x:Project/x:PropertyGroup/x:DSP)", DatabaseProjectNamespaceManager);
            ModelCollation = (string)DatabaseProject.XPathEvaluate("string(x:Project/x:PropertyGroup/x:ModelCollation)", DatabaseProjectNamespaceManager);
        }

        public static IEnumerable<XElement> QueryProject(string expression) => DatabaseProject.XPathSelectElements(expression, DatabaseProjectNamespaceManager);
    }
}