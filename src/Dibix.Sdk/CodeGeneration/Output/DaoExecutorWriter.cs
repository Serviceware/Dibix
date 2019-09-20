using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class DaoExecutorWriter : DaoChildWriterBase, IDaoChildWriter
    {
        #region Fields
        private const string ConstantSuffix = "CommandText";
        private const string MethodPrefix = "";//"Execute";
        #endregion

        #region Properties
        public override string RegionName => "Accessor";
        #endregion

        #region Overrides
        public override bool HasContent(SourceArtifacts artifacts) => artifacts.Statements.Any();

        public override void Write(WriterContext context)
        {
            context.Output.AddUsing(typeof(GeneratedCodeAttribute).Namespace);

            foreach (IGrouping<string, SqlStatementInfo> namespaceGroup in context.Artifacts.Statements.GroupBy(x => x.Namespace))
            {
                CSharpStatementScope scope = namespaceGroup.Key != null ? context.Output.BeginScope(NamespaceUtility.BuildRelativeNamespace(context.Configuration.RootNamespace, namespaceGroup.Key)) : context.Output;
                IList<SqlStatementInfo> statements = namespaceGroup.ToArray();

                // Class
                CSharpModifiers classVisibility = context.Configuration.GeneratePublicArtifacts ? CSharpModifiers.Public : CSharpModifiers.Internal;
                IEnumerable<string> annotations = new[] { context.GeneratedCodeAnnotation }.Where(x => x != null);
                CSharpClass @class = scope.AddClass(context.Configuration.DefaultClassName, classVisibility | CSharpModifiers.Static, annotations);

                // Command text constants
                AddCommandTextConstants(@class, context, statements);

                // Execution methods
                @class.AddSeparator();
                AddExecutionMethods(@class, context, statements);
            }
        }
        #endregion

        #region Private Methods
        private static void AddCommandTextConstants(CSharpClass @class, WriterContext context, IList<SqlStatementInfo> statements)
        {
            for (int i = 0; i < statements.Count; i++)
            {
                SqlStatementInfo statement = statements[i];
                //@class.AddComment(String.Concat("file:///", statement.SourcePath.Replace(" ", "%20").Replace(@"\", "/")), false);
                @class.AddComment(statement.Name, false);
                CSharpModifiers fieldVisibility = context.Configuration.GeneratePublicArtifacts ? CSharpModifiers.Private : CSharpModifiers.Public;
                @class.AddField(name: String.Concat(statement.Name, ConstantSuffix)
                              , type: typeof(string).ToCSharpTypeName()
                              , value: new CSharpStringValue(context.FormatCommandText(statement.Content, context.Configuration.Formatting), context.Configuration.Formatting.HasFlag(CommandTextFormatting.Verbatim))
                              , modifiers: fieldVisibility | CSharpModifiers.Const);

                if (i + 1 < statements.Count)
                    @class.AddSeparator();
            }
        }

        private static void AddExecutionMethods(CSharpClass @class, WriterContext context, IList<SqlStatementInfo> statements)
        {
            context.Output.AddUsing("Dibix");

            IDictionary<SqlStatementInfo, string> methodReturnTypeMap = statements.ToDictionary(x => x, DetermineResultTypeName);

            for (int i = 0; i < statements.Count; i++)
            {
                SqlStatementInfo statement = statements[i];
                bool isSingleResult = statement.Results.Count == 1;

                if (isSingleResult && statement.Results[0].ResultMode == SqlQueryResultMode.Many)
                    context.Output.AddUsing(typeof(IEnumerable<>).Namespace);

                if (statement.IsFileApi)
                    context.Output.AddUsing("Dibix.Http");

                string resultTypeName = methodReturnTypeMap[statement];
                CSharpMethod method = @class.AddMethod(name: String.Concat(MethodPrefix, statement.Name)
                                                     , type: resultTypeName
                                                     , body: GenerateMethodBody(statement, context)
                                                     , isExtension: true
                                                     , modifiers: CSharpModifiers.Public | CSharpModifiers.Static);
                method.AddParameter("databaseAccessorFactory", "IDatabaseAccessorFactory");

                if (statement.GenerateInputClass)
                    method.AddParameter("input", $"{statement.Name}{DaoExecutorInputClassWriter.InputTypeSuffix}", "InputClass");
                else
                {
                    foreach (SqlQueryParameter parameter in statement.Parameters)
                        method.AddParameter(parameter.Name, parameter.ClrTypeName);
                }

                if (i + 1 < statements.Count)
                    @class.AddSeparator();
            }
        }

        private static string DetermineResultTypeName(SqlStatementInfo query)
        {
            if (query.IsFileApi)
            {
                return "HttpFileResponse";
            }

            if (query.Results.Count == 0) // Execute/ExecutePrimitive.
            {
                return typeof(int).ToCSharpTypeName();
            }

            if (query.Results.Count == 1) // Query<T>/QuerySingle/etc.
            {
                ContractInfo resultContract = query.Results[0].Contracts.First();
                string resultContractName = resultContract.Name.ToString();

                if (query.Results[0].ResultMode == SqlQueryResultMode.Many)
                    resultContractName = MakeEnumerableType(resultContractName);

                return resultContractName;
            }

            // GridReader
            return GetComplexTypeName(query);
        }

        private static string GenerateMethodBody(SqlStatementInfo statement, WriterContext context)
        {
            StringWriter writer = new StringWriter();

            if (context.WriteGuardChecks)
            {
                ICollection<SqlQueryParameter> guardParameters = statement.Parameters.Where(x => x.Check != ContractCheck.None).ToArray();
                foreach (SqlQueryParameter parameter in guardParameters)
                    WriteGuardCheck(writer, parameter.Check, parameter.Name);

                if (guardParameters.Any())
                    writer.WriteLine();
            }

            writer.WriteLine("using (IDatabaseAccessor accessor = databaseAccessorFactory.Create())")
                  .WriteLine("{")
                  .PushIndent();

            if (statement.Parameters.Any())
                WriteParameters(writer, statement);

            WriteExecutor(writer, statement);

            writer.PopIndent()
                  .Write("}");

            return writer.ToString();
        }

        private static void WriteGuardCheck(StringWriter writer, ContractCheck mode, string parameterName)
        {
            writer.Write("Guard.Is");
            switch (mode)
            {
                case ContractCheck.None:
                    break;
                case ContractCheck.NotNull:
                case ContractCheck.NotNullOrEmpty:
                    writer.WriteRaw(mode);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
            writer.WriteRaw('(')
                  .WriteRaw(parameterName)
                  .WriteRaw(", \"")
                  .WriteRaw(parameterName)
                  .WriteRaw("\");")
                  .WriteLine();
        }

        private static void WriteParameters(StringWriter writer, SqlStatementInfo query)
        {
            writer.Write("IParametersVisitor @params = accessor.Parameters()")
                  .SetTemporaryIndent(37);

            writer.WriteLine()
                  .Write(".SetFromTemplate(");

            if (query.GenerateInputClass)
                writer.WriteRaw("input");
            else
            {

                writer.WriteLineRaw("new")
                      .WriteLine("{")
                      .PushIndent();

                for (int i = 0; i < query.Parameters.Count; i++)
                {
                    SqlQueryParameter parameter = query.Parameters[i];
                    writer.Write(parameter.Name);

                    if (i + 1 < query.Parameters.Count)
                        writer.WriteRaw(",");

                    writer.WriteLine();
                }

                writer.PopIndent()
                      .Write("}");
            }

            writer.WriteLineRaw(")")
                  .WriteLine(".Build();")
                  .ResetTemporaryIndent();
        }

        private static void WriteExecutor(StringWriter writer, SqlStatementInfo query)
        {
            if (query.IsFileApi)
            {
                WriteFileApiResult(writer, query);
            }
            else if (query.Results.Count == 0) // Execute/ExecutePrimitive.
            {
                WriteNoResult(writer, query);
            }
            else if (query.Results.Count == 1) // Query<T>/QuerySingle/etc.
            {
                WriteSingleResult(writer, query);
            }
            else if (query.Results.Count > 1) // GridReader
            {
                WriteComplexResult(writer, query);
            }
        }

        private static void WriteNoResult(StringWriter writer, SqlStatementInfo query)
        {
            writer.Write("return accessor.Execute(")
                  .WriteRaw(query.Name)
                  .WriteRaw(ConstantSuffix);

            if (query.CommandType.HasValue)
                writer.WriteRaw(", System.Data.CommandType.")
                      .WriteRaw(query.CommandType.Value);

            if (query.Parameters.Any())
                writer.WriteRaw(", @params");

            writer.WriteLineRaw(");");
        }

        private static void WriteSingleResult(StringWriter writer, SqlStatementInfo query)
        {
            SqlQueryResult singleResult = query.Results.Single();

            writer.Write("return accessor.")
                  .WriteRaw(GetExecutorMethodName(query.Results[0].ResultMode))
                  .WriteRaw('<');

            for (int i = 0; i < singleResult.Contracts.Count; i++)
            {
                ContractName returnType = singleResult.Contracts[i].Name;
                writer.WriteRaw(returnType);
                if (i + 1 < singleResult.Contracts.Count)
                    writer.WriteRaw(", ");
            }

            if (singleResult.ResultType != null)
                writer.WriteRaw(", ")
                      .WriteRaw(singleResult.ResultType);

            writer.WriteRaw(">(")
                  .WriteRaw(query.Name)
                  .WriteRaw(ConstantSuffix);

            if (query.CommandType.HasValue)
                writer.WriteRaw($", {typeof(CommandType).FullName}.")
                      .WriteRaw(query.CommandType.Value);

            if (query.Parameters.Any())
                writer.WriteRaw(", @params");

            if (query.Results[0].Contracts.Count > 1)
            {
                if (!String.IsNullOrEmpty(query.Results[0].Converter))
                    writer.WriteRaw(", ")
                          .WriteRaw(query.Results[0].Converter);

                writer.WriteRaw(", \"")
                      .WriteRaw(query.Results[0].SplitOn)
                      .WriteRaw('"');
            }

            writer.WriteLineRaw(");");
        }

        private static void WriteComplexResult(StringWriter writer, SqlStatementInfo query)
        {
            writer.Write("using (IMultipleResultReader reader = accessor.QueryMultiple(")
                  .WriteRaw(query.Name)
                  .WriteRaw(ConstantSuffix);

            if (query.CommandType.HasValue)
                writer.WriteRaw($", {typeof(CommandType).FullName}.")
                      .WriteRaw(query.CommandType.Value);

            if (query.Parameters.Any())
                writer.WriteRaw(", @params");

            writer.WriteLineRaw("))")
                  .WriteLine("{")
                  .PushIndent();

            WriteComplexResultBody(writer, query);

            writer.PopIndent()
                  .WriteLine("}");
        }

        private static void WriteComplexResultBody(StringWriter writer, SqlStatementInfo query)
        {
            string resultTypeName = GetComplexTypeName(query);

            writer.Write(resultTypeName)
                  .WriteRaw(" result = ");

            if (!query.MergeGridResult)
            {
                writer.WriteRaw("new ")
                      .WriteRaw(resultTypeName)
                      .WriteLineRaw("();");
            }

            for (int i = 0; i < query.Results.Count; i++)
            {
                SqlQueryResult result = query.Results[i];

                bool isEnumerable = result.ResultMode == SqlQueryResultMode.Many;
                if (i > 0 || !query.MergeGridResult)
                {
                    writer.Write("result")
                          .WriteRaw($".{result.Name}");

                    if (isEnumerable)
                        writer.WriteRaw(".ReplaceWith(");
                    else
                        writer.WriteRaw(" = ");
                }

                writer.WriteRaw("reader.")
                      .WriteRaw(GetMultipleResultReaderMethodName(result.ResultMode))
                      .WriteRaw('<');

                for (int j = 0; j < result.Contracts.Count; j++)
                {
                    ContractName returnType = result.Contracts[j].Name;
                    writer.WriteRaw(returnType);
                    if (j + 1 < result.Contracts.Count)
                        writer.WriteRaw(", ");
                }

                if (result.ResultType != null)
                    writer.WriteRaw(", ")
                          .WriteRaw(result.ResultType);

                writer.WriteRaw(">(");

                if (result.Contracts.Count > 1)
                {
                    if (!String.IsNullOrEmpty(result.Converter))
                        writer.WriteRaw(result.Converter)
                              .WriteRaw(", ");

                    writer.WriteRaw('"')
                          .WriteRaw(result.SplitOn)
                          .WriteRaw('"');
                }

                writer.WriteRaw(')');

                if (isEnumerable && (i > 0 || !query.MergeGridResult))
                    writer.WriteRaw(')');

                writer.WriteLineRaw(";");
            }

            writer.WriteLine("return result;");
        }

        private static void WriteFileApiResult(StringWriter writer, SqlStatementInfo query)
        {
            writer.Write("return accessor.QueryFile(")
                  .WriteRaw(query.Name)
                  .WriteRaw(ConstantSuffix)
                  .WriteRaw(", @params);")
                  .WriteLine();
        }

        private static string GetExecutorMethodName(SqlQueryResultMode mode)
        {
            switch (mode)
            {
                case SqlQueryResultMode.Many: return "QueryMany";
                case SqlQueryResultMode.Single: return "QuerySingle";
                case SqlQueryResultMode.SingleOrDefault: return "QuerySingleOrDefault";
                default: throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }

        private static string GetMultipleResultReaderMethodName(SqlQueryResultMode mode)
        {
            switch (mode)
            {
                case SqlQueryResultMode.Many: return "ReadMany";
                case SqlQueryResultMode.Single: return "ReadSingle";
                case SqlQueryResultMode.SingleOrDefault: return "ReadSingleOrDefault";
                default: throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }

        private static string MakeEnumerableType(string typeName)
        {
            return String.Concat("IEnumerable<", typeName, '>');
        }

        private static string GetComplexTypeName(SqlStatementInfo statement)
        {
            if (statement.MergeGridResult)
                return statement.Results[0].Contracts[0].Name.ToString();

            StringBuilder sb = new StringBuilder();

            // Explicit existing type specified
            if (statement.ResultType != null)
            {
                sb.Append(statement.ResultType);
            }
            else
            {
                // Control generated type name (includes namespace)
                if (statement.GeneratedResultTypeName != null)
                {
                    sb.Append(statement.GeneratedResultTypeName);
                }
                else
                {
                    // Use data accessor namespace if available
                    if (statement.Namespace != null)
                        sb.Append(statement.Namespace)
                          .Append('.');

                    // Generate type name based on statement name
                    sb.Append(statement.Name)
                      .Append(DaoGridResultClassWriter.ComplexResultTypeSuffix);
                }
            }

            return sb.ToString();
        }
        #endregion
    }
}