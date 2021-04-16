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
        #endregion

        #region Properties
        public override string LayerName => CodeGeneration.LayerName.Data;
        public override string RegionName => "Input types";
        #endregion

        #region Overrides
        public override bool HasContent(CodeGenerationModel model) => model.Statements.Any(RequiresInput);

        public override void Write(CodeGenerationContext context)
        {
            var namespaceGroups = context.Model
                                         .Statements
                                         .Where(RequiresInput)
                                         .GroupBy(x => context.GetRelativeNamespace(this.LayerName, x.Namespace))
                                         .ToArray();

            for (int i = 0; i < namespaceGroups.Length; i++)
            {
                IGrouping<string, SqlStatementInfo> namespaceGroup = namespaceGroups[i];
                CSharpStatementScope scope = namespaceGroup.Key != null ? context.Output.BeginScope(namespaceGroup.Key) : context.Output;
                IList<SqlStatementInfo> statements = namespaceGroup.ToArray();
                for (int j = 0; j < statements.Count; j++)
                {
                    SqlStatementInfo statement = statements[j];
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
                    context.Output.AddSeparator();
            }
        }
        #endregion

        #region Private Methods
        private static bool RequiresInput(SqlStatementInfo statement) => statement.GenerateInputClass;

        private static string GetInputTypeName(SqlStatementInfo statement) => String.Concat(statement.Name, InputTypeSuffix);

        private static string ResolvePropertyTypeName(SqlQueryParameter parameter, CodeGenerationContext context)
        {
            string typeName = context.ResolveTypeName(parameter.Type);
            if (!parameter.IsOutput)
                return typeName;

            return $"IOutParameter<{typeName}>";
        }
        #endregion
    }
}