using System;
using System.Collections.Generic;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class DaoExecutorInputClassWriter : DaoWriterBase, IDaoWriter
    {
        #region Fields
        internal const string InputTypeSuffix = "Input";
        #endregion

        #region Properties
        public override string RegionName => "Input types";
        #endregion

        #region Overrides
        public override bool HasContent(OutputConfiguration configuration, SourceArtifacts artifacts) => artifacts.Statements.Any(RequiresInput);

        protected override void Write(DaoWriterContext context, HashSet<string> contracts)
        {
            var namespaceGroups = context.Artifacts
                                         .Statements
                                         .Where(RequiresInput)
                                         .GroupBy(x => x.Namespace)
                                         .ToArray();

            for (int i = 0; i < namespaceGroups.Length; i++)
            {
                IGrouping<string, SqlStatementInfo> namespaceGroup = namespaceGroups[i];
                CSharpStatementScope scope = namespaceGroup.Key != null ? context.Output.BeginScope(namespaceGroup.Key) : context.Output;
                IList<SqlStatementInfo> statements = namespaceGroup.ToArray();
                for (int j = 0; j < statements.Count; j++)
                {
                    SqlStatementInfo statement = statements[j];
                    CSharpModifiers classVisibility = context.Configuration.GeneratePublicArtifacts ? CSharpModifiers.Public : CSharpModifiers.Internal;
                    CSharpClass inputType = scope.AddClass(GetComplexTypeName(statement), classVisibility | CSharpModifiers.Sealed);

                    foreach (SqlQueryParameter parameter in statement.Parameters)
                    {
                        inputType.AddProperty(parameter.Name, parameter.ClrTypeName)
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

        private static string GetComplexTypeName(SqlStatementInfo statement) => String.Concat(statement.Name, InputTypeSuffix);
        #endregion
    }
}