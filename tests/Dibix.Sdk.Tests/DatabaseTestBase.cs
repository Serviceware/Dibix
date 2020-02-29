using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Dibix.Sdk.Tests
{
    public abstract class DatabaseTestBase
    {
        private readonly XDocument _databaseProject;
        private readonly XmlNamespaceManager _databaseProjectNamespaceManager;

        protected static Assembly Assembly { get; } = typeof(DatabaseTestBase).Assembly;
        protected static string ProjectDirectory { get; } = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "..", "..", ".."));
        protected static string DatabaseProjectDirectory { get; } = Path.GetFullPath(Path.Combine(ProjectDirectory, "..", "Dibix.Sdk.Tests.Database"));
        protected string DatabaseSchemaProviderName { get; }
        protected string ModelCollation { get; }

        protected DatabaseTestBase()
        {
            string databaseProjectPath = Path.Combine(DatabaseProjectDirectory, "Dibix.Sdk.Tests.Database.sqlproj");

            this._databaseProject = XDocument.Load(databaseProjectPath);
            this._databaseProjectNamespaceManager = new XmlNamespaceManager(new NameTable());
            this._databaseProjectNamespaceManager.AddNamespace("x", "http://schemas.microsoft.com/developer/msbuild/2003");

            this.DatabaseSchemaProviderName = (string)this._databaseProject.XPathEvaluate("string(x:Project/x:PropertyGroup/x:DSP)", this._databaseProjectNamespaceManager);
            this.ModelCollation = (string)this._databaseProject.XPathEvaluate("string(x:Project/x:PropertyGroup/x:ModelCollation)", this._databaseProjectNamespaceManager);

            IDictionary<string, Assembly> dependentAssemblies = LoadDependentAssemblies().ToDictionary(x => x.Key, x => x.Value);
            AppDomain.CurrentDomain.AssemblyResolve += (sender, e) => dependentAssemblies.TryGetValue(new AssemblyName(e.Name).Name, out Assembly assembly) ? assembly : null;

        }

        protected object QueryProject(string expression) => this._databaseProject.XPathEvaluate(expression, this._databaseProjectNamespaceManager);

        private static IEnumerable<KeyValuePair<string, Assembly>> LoadDependentAssemblies()
        {
            string pattern = $"^{Regex.Escape($"{Assembly.GetName().Name}.")}[A-Za-z.]+{Regex.Escape(".dll")}$";
            foreach (string resourceName in Assembly.GetManifestResourceNames().Where(x => Regex.IsMatch(x, pattern)))
            {
                using (Stream sourceStream = Assembly.GetManifestResourceStream(resourceName))
                {
                    using (MemoryStream targetStream = new MemoryStream())
                    {
                        sourceStream.CopyTo(targetStream);
                        Assembly assembly = Assembly.Load(targetStream.ToArray());
                        yield return new KeyValuePair<string, Assembly>(assembly.GetName().Name, assembly);
                    }
                }
            }
        }
    }
}