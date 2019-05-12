using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.CodeGeneration.Ast;
using Newtonsoft.Json.Schema;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class DaoContractClassWriter : IDaoWriter
    {
        #region Fields
        private readonly IJsonSchemaProvider _jsonSchemaProvider;
        #endregion

        #region Properties
        public string RegionName => "Contracts";
        #endregion

        #region Constructor
        public DaoContractClassWriter(IJsonSchemaProvider jsonSchemaProvider)
        {
            this._jsonSchemaProvider = jsonSchemaProvider;
        }
        #endregion

        #region IDaoWriter Members
        public bool HasContent(IEnumerable<SqlStatementInfo> statements) => statements.Any(x => x.Results.Any(y => y.Contracts.Any(z => z.Schema != null)));

        public void Write(DaoWriterContext context)
        {
            foreach (IGrouping<string, JsonContract> group in context.Artifacts.Contracts.GroupBy(x => x.Namespace))
            {
                string @namespace = group.Key;
                CSharpStatementScope scope = context.Output.BeginScope(@namespace);
                foreach (JsonContract contract in group)
                {
                    CSharpClass @class = scope.AddClass(contract.DefinitionName, CSharpModifiers.Public);
                    foreach (KeyValuePair<string, JSchema> property in contract.Schema.Properties)
                    {
                        @class.AddProperty(property.Key, property.Value.Type.ToString());
                    }
                }
            }
        }
        #endregion
    }
}