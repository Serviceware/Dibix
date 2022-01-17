using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class DaoExecutorInputClassWriter : ArtifactWriterBase
    {
        #region Fields
        internal const string InputTypeSuffix = "Input";
        private readonly ICollection<SqlStatementDefinition> _schemas;
        #endregion

        #region Properties
        public override string LayerName => CodeGeneration.LayerName.Data;
        public override string RegionName => "Input types";
        #endregion

        #region Constructor
        public DaoExecutorInputClassWriter(CodeGenerationModel model, SchemaDefinitionSource schemaFilter)
        {
            this._schemas = model.Schemas
                                 .OfType<SqlStatementDefinition>()
                                 .Where(x => schemaFilter.HasFlag(x.Source) && RequiresInput(x))
                                 .ToArray();
        }
        #endregion

        #region Overrides
        public override bool HasContent(CodeGenerationModel model) => this._schemas.Any();

        public override void Write(CodeGenerationContext context)
        {
            var namespaceGroups = this._schemas
                                      .GroupBy(x => x.Namespace)
                                      .ToArray();

            for (int i = 0; i < namespaceGroups.Length; i++)
            {
                IGrouping<string, SqlStatementDefinition> namespaceGroup = namespaceGroups[i];
                CSharpStatementScope scope = /*namespaceGroup.Key != null ? */context.CreateOutputScope(namespaceGroup.Key)/* : context.Output*/;
                IList<SqlStatementDefinition> statements = namespaceGroup.ToArray();
                for (int j = 0; j < statements.Count; j++)
                {
                    SqlStatementDefinition statement = statements[j];
                    CSharpModifiers classVisibility = context.GeneratePublicArtifacts ? CSharpModifiers.Public : CSharpModifiers.Internal;
                    CSharpClass inputType = scope.AddClass(GetInputTypeName(statement), classVisibility | CSharpModifiers.Sealed);

                    foreach (SqlQueryParameter parameter in statement.Parameters)
                    {
                        ICollection<CSharpAnnotation> propertyAnnotations = new Collection<CSharpAnnotation>();
                        if (parameter.Obfuscate)
                            propertyAnnotations.Add(new CSharpAnnotation("Obfuscated"));

                        inputType.AddProperty(parameter.Name, ResolvePropertyTypeName(parameter, context), propertyAnnotations)
                                 .Getter(null)
                                 .Setter(null);
                    }

                    if (j + 1 < statements.Count)
                        scope.AddSeparator();
                }

                if (i + 1 < namespaceGroups.Length)
                    context.AddSeparator();
            }
        }
        #endregion

        #region Private Methods
        private static bool RequiresInput(SqlStatementDefinition statement) => statement.GenerateInputClass;

        private static string GetInputTypeName(SqlStatementDefinition statement) => String.Concat(statement.DefinitionName, InputTypeSuffix);

        private static string ResolvePropertyTypeName(SqlQueryParameter parameter, CodeGenerationContext context)
        {
            string typeName = context.ResolveTypeName(parameter.Type, context);
            if (!parameter.IsOutput)
                return typeName;

            return $"IOutParameter<{typeName}>";
        }
        #endregion
    }
}