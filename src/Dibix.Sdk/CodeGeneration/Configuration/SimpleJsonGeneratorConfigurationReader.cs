using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class SimpleJsonGeneratorConfigurationReader : JsonGeneratorConfigurationReader
    {
        #region Properties
        protected override string SchemaName => "dibix.schema.simple";
        #endregion

        #region Constructor
        public SimpleJsonGeneratorConfigurationReader(string json, IFileSystemProvider fileSystemProvider, IErrorReporter errorReporter) : base(json, fileSystemProvider, errorReporter) { }
        #endregion

        #region Overrides
        protected override void Read(GeneratorConfiguration configuration, JObject root)
        {
            this.ReadInput(configuration, root);
            base.Read(configuration, root);
        }

        protected override void ReadDacPacProject(DacPacSourceConfiguration configuration, JObject json)
        {
            json.GetPropertyValues("include").Each(x => configuration.AddStoredProcedure(x, NormalizeModelElementName(x)));
        }
        #endregion

        #region Private Methods
        private void ReadInput(GeneratorConfiguration configuration, JObject root)
        {
            var groups = new[]
            {
                new
                {
                    Key = "dml",
                    Formatter = typeof(TakeSourceSqlStatementFormatter)
                },
                new
                {
                    Key = "ddl",
                    Formatter = typeof(ExecStoredProcedureSqlStatementFormatter)
                }
            };
            foreach (var group in groups)
            {
                JToken token = root[group.Key];
                if (token == null)
                    continue;

                this.ReadInputSources(configuration, token, x => x.Formatter = group.Formatter);
            }
        }

        private static string NormalizeModelElementName(string modelElementName)
        {
            string name = modelElementName.Split('.').Last();
            string normalizedName = Regex.Replace(CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name), @"[\[\]_]", String.Empty);
            return normalizedName;
        }
        #endregion
    }
}