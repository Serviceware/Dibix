using System;
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
        public static string DatabaseSchemaProviderName { get; }
        public static string ModelCollation { get; }

        static DatabaseTestUtility()
        {
            string databaseProjectPath = Path.Combine(DatabaseProjectDirectory, "Dibix.Sdk.Tests.Database.sqlproj");

            DatabaseProject = XDocument.Load(databaseProjectPath);
            DatabaseProjectNamespaceManager = new XmlNamespaceManager(new NameTable());
            DatabaseProjectNamespaceManager.AddNamespace("x", "http://schemas.microsoft.com/developer/msbuild/2003");

            DatabaseSchemaProviderName = (string)DatabaseProject.XPathEvaluate("string(x:Project/x:PropertyGroup/x:DSP)", DatabaseProjectNamespaceManager);
            ModelCollation = (string)DatabaseProject.XPathEvaluate("string(x:Project/x:PropertyGroup/x:ModelCollation)", DatabaseProjectNamespaceManager);
        }

        public static object QueryProject(string expression) => DatabaseProject.XPathEvaluate(expression, DatabaseProjectNamespaceManager);
    }
}