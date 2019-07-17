using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class DaoExecutorWriter : DaoWriterBase, IDaoWriter
    {
        #region Fields
        private const string ConstantSuffix = "CommandText";
        private const string MethodPrefix = "";//"Execute";
        private const string ComplexResultTypeSuffix = "Result";
        #endregion

        #region Properties
        public override string RegionName => "Accessor";
        #endregion

        #region Overrides
        public override bool HasContent(OutputConfiguration configuration, SourceArtifacts artifacts) => artifacts.Statements.Any();

        protected override void Write(DaoWriterContext context, HashSet<string> contracts)
        {
            context.Output.AddUsing(typeof(GeneratedCodeAttribute).Namespace);

            foreach (IGrouping<string, SqlStatementInfo> namespaceGroup in context.Artifacts.Statements.GroupBy(x => x.Namespace))
            {
                CSharpStatementScope scope = namespaceGroup.Key != null ? context.Output.BeginScope(namespaceGroup.Key) : context.Output;
                IList<SqlStatementInfo> statements = namespaceGroup.ToArray();

                // Class
                CSharpModifiers classVisibility = context.Configuration.GeneratePublicArtifacts ? CSharpModifiers.Public : CSharpModifiers.Internal;
                CSharpClass @class = scope.AddClass(context.Configuration.ClassName, classVisibility | CSharpModifiers.Static, context.GeneratedCodeAnnotation);

                // Command text constants
                AddCommandTextConstants(@class, context, statements);

                // Execution methods
                @class.AddSeparator();
                this.AddExecutionMethods(@class, context, statements, contracts);

                // Add method info accessor fields
                // This is useful for dynamic invocation like in WAX
                @class.AddSeparator();
                AddMethodInfoFields(@class, context, statements);
            }
        }
        #endregion

        #region Private Methods
        private static void AddCommandTextConstants(CSharpClass @class, DaoWriterContext context, IList<SqlStatementInfo> statements)
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

        private void AddExecutionMethods(CSharpClass @class, DaoWriterContext context, IList<SqlStatementInfo> statements, HashSet<string> contracts)
        {
            context.Output.AddUsing("Dibix");

            IDictionary<SqlStatementInfo, string> methodReturnTypeMap = statements.ToDictionary(x => x, x => this.DetermineResultTypeName(context, x, contracts));

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
                                                     , body: this.GenerateMethodBody(statement, context, contracts)
                                                     , isExtension: true
                                                     , modifiers: CSharpModifiers.Public | CSharpModifiers.Static);
                method.AddParameter("databaseAccessorFactory", "IDatabaseAccessorFactory");
                foreach (SqlQueryParameter parameter in statement.Parameters)
                    method.AddParameter(parameter.Name, parameter.ClrTypeName);

                if (i + 1 < statements.Count)
                    @class.AddSeparator();
            }
        }

        private string DetermineResultTypeName(DaoWriterContext context, SqlStatementInfo query, HashSet<string> contracts)
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
                string resultContractName = base.PrefixWithRootNamespace(context, resultContract.Name, contracts);

                if (query.Results[0].ResultMode == SqlQueryResultMode.Many)
                    resultContractName = MakeEnumerableType(resultContractName);

                return resultContractName;
            }

            // GridReader
            return this.GetComplexTypeName(context, query, contracts);
        }

        private static void AddMethodInfoFields(CSharpClass @class, DaoWriterContext context, IEnumerable<SqlStatementInfo> statements)
        {
            Type methodInfoType = typeof(MethodInfo);
            context.Output.AddUsing(methodInfoType.Namespace);
            foreach (SqlStatementInfo statement in statements)
            {
                @class.AddField(name: String.Concat(statement.Name, methodInfoType.Name)
                              , type: methodInfoType.Name
                              , value: new CSharpValue($"typeof({context.Configuration.ClassName}).GetMethod(\"{statement.Name}\")")
                              , modifiers: CSharpModifiers.Public | CSharpModifiers.Static | CSharpModifiers.ReadOnly);
            }
        }

        private string GenerateMethodBody(SqlStatementInfo statement, DaoWriterContext context, HashSet<string> contracts)
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

            this.WriteExecutor(context, writer, statement, contracts);

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
                  .WriteLine(".SetFromTemplate(new")
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
                  .Write("})");

            writer.WriteLine()
                  .WriteLine(".Build();")
                  .ResetTemporaryIndent();
        }

        private void WriteExecutor(DaoWriterContext context, StringWriter writer, SqlStatementInfo query, HashSet<string> contracts)
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
                this.WriteComplexResult(context, writer, query, contracts);
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

        private void WriteComplexResult(DaoWriterContext context, StringWriter writer, SqlStatementInfo query, HashSet<string> contracts)
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

            this.WriteComplexResultBody(context, writer, query, contracts);

            writer.PopIndent()
                  .WriteLine("}");
        }

        private void WriteComplexResultBody(DaoWriterContext context, StringWriter writer, SqlStatementInfo query, HashSet<string> contracts)
        {
            string resultTypeName = this.GetComplexTypeName(context, query, contracts);

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

        private string GetComplexTypeName(DaoWriterContext context, SqlStatementInfo statement, HashSet<string> contracts)
        {
            if (statement.MergeGridResult)
                return statement.Results[0].Contracts[0].Name.ToString();

            StringBuilder sb = new StringBuilder();

            // Explicit existing type specified
            if (statement.ResultType != null)
            {
                sb.Append(base.PrefixWithRootNamespace(context, statement.ResultType, contracts));
            }
            else
            {
                // Use absolute namespaces to make it more stable
                sb.Append(context.Configuration.Namespace)
                  .Append('.');

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
                      .Append(ComplexResultTypeSuffix);
                }
            }

            return sb.ToString();
        }
        #endregion
    }
}