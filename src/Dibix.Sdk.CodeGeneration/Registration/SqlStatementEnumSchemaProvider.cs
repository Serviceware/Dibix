using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.Abstractions;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class SqlStatementEnumSchemaProvider : ISchemaProvider
    {
        private readonly string _productName;
        private readonly string _areaName;
        private readonly ICollection<TaskItem> _files;
        private readonly IDictionary<string, EnumSchema> _definitions;
        private readonly ILogger _logger;

        public SqlStatementEnumSchemaProvider(string productName, string areaName, ICollection<TaskItem> files, ILogger logger)
        {
            _productName = productName;
            _areaName = areaName;
            _files = files;
            _logger = logger;
            _definitions = new Dictionary<string, EnumSchema>();
        }

        public IEnumerable<SchemaDefinition> Collect()
        {
            Collect(_files.Select(x => x.GetFullPath()));
            return _definitions.Values;
        }

        private void Collect(IEnumerable<string> files)
        {
            foreach (string file in files)
            {
                try
                {
                    bool result = Read(file, out EnumSchema definition);

                    if (!result)
                        continue;

                    if (_definitions.ContainsKey(definition.FullName))
                    {
                        _logger.LogError($"Ambiguous enum definition name: {definition.FullName}", definition.Location.Source, definition.Location.Line, definition.Location.Column);
                        continue;
                    }

                    _definitions.Add(definition.FullName, definition);
                }
                catch (Exception exception)
                {
                    throw new Exception($@"{exception.Message}
   at {file}
", exception);
                }
            }
        }

        private bool Read(string file, out EnumSchema definition)
        {
            TSqlFragment fragment = ScriptDomFacade.Load(file);

            EnumContractVisitor visitor = new EnumContractVisitor(file, _productName, _areaName, _logger);
            fragment.Accept(visitor);

            if (visitor.Definition != null)
            {
                definition = visitor.Definition;
                return true;
            }

            definition = null;
            return false;
        }
    }
}