using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class DaoExecutorWriter : DaoWriter
    {
        #region Fields
        private const string ConstantSuffix = "CommandText";
        private const string MethodPrefix = "";//"Execute";
        #endregion

        #region Properties
        public override string LayerName => CodeGeneration.LayerName.Data;
        public override string RegionName => "Accessor";
        #endregion

        #region Overrides
        public override bool HasContent(CodeGenerationModel model) => model.Statements.Any();

        public override void Write(DaoCodeGenerationContext context)
        {
            context.AddUsing(typeof(GeneratedCodeAttribute).Namespace)
                   .AddUsing("Dibix");

            foreach (IGrouping<string, SqlStatementInfo> namespaceGroup in context.Model.Statements.GroupBy(x => context.GetRelativeNamespace(this.LayerName, x.Namespace)))
            {
                CSharpStatementScope scope = namespaceGroup.Key != null ? context.Output.BeginScope(namespaceGroup.Key) : context.Output;
                IList<SqlStatementInfo> statements = namespaceGroup.ToArray();

                // Class
                CSharpModifiers classVisibility = context.GeneratePublicArtifacts ? CSharpModifiers.Public : CSharpModifiers.Internal;
                IEnumerable<string> annotations = new[] { context.GeneratedCodeAnnotation, "DatabaseAccessor" }.Where(x => x != null);
                CSharpClass @class = scope.AddClass(context.Model.DefaultClassName, classVisibility | CSharpModifiers.Static, annotations);

                // Command text constants
                AddCommandTextConstants(@class, context, statements);

                // Execution methods
                @class.AddSeparator();
                AddExecutionMethods(@class, context, statements);
            }
        }
        #endregion

        #region Private Methods
        private static void AddCommandTextConstants(CSharpClass @class, DaoCodeGenerationContext context, IList<SqlStatementInfo> statements)
        {
            for (int i = 0; i < statements.Count; i++)
            {
                SqlStatementInfo statement = statements[i];
                //@class.AddComment(String.Concat("file:///", statement.SourcePath.Replace(" ", "%20").Replace(@"\", "/")), false);
                @class.AddComment(statement.Name, false);
                CSharpModifiers fieldVisibility = context.GeneratePublicArtifacts ? CSharpModifiers.Private : CSharpModifiers.Public;
                @class.AddField(name: String.Concat(statement.Name, ConstantSuffix)
                              , type: "string"
                              , value: new CSharpStringValue(CodeGenerationUtility.FormatCommandText(statement.Content, context.Model.CommandTextFormatting), context.Model.CommandTextFormatting.HasFlag(CommandTextFormatting.Verbatim))
                              , modifiers: fieldVisibility | CSharpModifiers.Const);

                if (i + 1 < statements.Count)
                    @class.AddSeparator();
            }
        }

        private static void AddExecutionMethods(CSharpClass @class, DaoCodeGenerationContext context, IList<SqlStatementInfo> statements)
        {
            IDictionary<SqlStatementInfo, string> methodReturnTypeMap = statements.ToDictionary(x => x, x => ResolveTypeName(x, context));

            for (int i = 0; i < statements.Count; i++)
            {
                SqlStatementInfo statement = statements[i];
                bool isSingleResult = statement.Results.Count == 1;

                if (isSingleResult && statement.Results[0].ResultMode == SqlQueryResultMode.Many)
                    context.AddUsing(typeof(IEnumerable<>).Namespace);

                if (statement.IsFileApi)
                    context.AddUsing("Dibix.Http");

                string methodName = String.Concat(MethodPrefix, statement.Name);
                if (statement.Async)
                    methodName = $"{methodName}Async";

                string resultTypeName = methodReturnTypeMap[statement];
                CSharpMethod method = @class.AddMethod(name: methodName
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
                    {
                        CSharpValue defaultValue = parameter.HasDefaultValue ? ParseDefaultValue(parameter.DefaultValue) : null;
                        method.AddParameter(parameter.Name, context.ResolveTypeName(parameter.Type), defaultValue);
                    }
                }

                if (i + 1 < statements.Count)
                    @class.AddSeparator();
            }
        }

        private static CSharpValue ParseDefaultValue(object defaultValue)
        {
            if (defaultValue == null)
                return new CSharpValue("null");

            string defailtValueStr = defaultValue.ToString();

            if (defaultValue is bool)
                return new CSharpValue(defailtValueStr.ToLowerInvariant());

            if (defaultValue is string)
                return new CSharpStringValue(defailtValueStr, false);

            return new CSharpValue(defailtValueStr);
        }

        private static string ResolveTypeName(SqlStatementInfo query, DaoCodeGenerationContext context)
        {
            string resultTypeName = ResolveTypeNameCore(query, context);
            if (query.Async)
            {
                context.AddUsing(typeof(Task).Namespace);
                StringBuilder sb = new StringBuilder("async Task");
                if (query.ResultType != null)
                    sb.Append('<')
                      .Append(resultTypeName)
                      .Append('>');

                resultTypeName = sb.ToString();
            }
            return resultTypeName;
        }
        private static string ResolveTypeNameCore(SqlStatementInfo query, DaoCodeGenerationContext context)
        {
            if (query.IsFileApi)
                return "HttpFileResponse";

            if (query.ResultType == null) // Execute
                return "void";
            
            if (query.Results.Any(x => x.Name != null)) // GridResult
                return GetComplexTypeName(query, context);

            string typeName = context.ResolveTypeName(query.ResultType);
            return query.Results[0].ResultMode == SqlQueryResultMode.Many ? MakeEnumerableType(typeName) : typeName;
        }

        private static string GenerateMethodBody(SqlStatementInfo statement, DaoCodeGenerationContext context)
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

            WriteExecutor(writer, statement, context);

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
                    if (parameter.Obfuscate)
                        continue;

                    writer.Write(parameter.Name);

                    if (i + 1 < query.Parameters.Count)
                        writer.WriteRaw(",");

                    writer.WriteLine();
                }

                writer.PopIndent()
                      .Write("}");
            }

            writer.WriteLineRaw(")");

            foreach (SqlQueryParameter parameter in query.Parameters.Where(x => x.Obfuscate))
            {
                writer.WriteLine($".SetString(nameof({parameter.Name}), {parameter.Name}, true)");
            }

            writer.WriteLine(".Build();")
                  .ResetTemporaryIndent();
        }

        private static void WriteExecutor(StringWriter writer, SqlStatementInfo query, DaoCodeGenerationContext context)
        {
            if (query.IsFileApi)
            {
                WriteFileApiResult(writer, query);
            }
            else if (query.ResultType == null) // Execute
            {
                WriteNoResult(writer, query);
            }
            else if (query.Results.Any(x => x.Name != null)) // GridReader
            {
                WriteComplexResult(writer, query, context);
            }
            else if (query.Results.Count == 1) // Query<T>/QuerySingle/etc.
            {
                WriteSingleResult(writer, query, context);
            }
        }

        private static void WriteNoResult(StringWriter writer, SqlStatementInfo query)
        {
            writer.WriteIndent();
            
            if (query.ResultType != null)
                writer.WriteRaw("return ");

            if (query.Async)
                writer.WriteRaw("await ");

            writer.WriteRaw("accessor.Execute");

            if (query.Async)
                writer.WriteRaw("Async");

            writer.WriteRaw('(')
                  .WriteRaw(query.Name)
                  .WriteRaw(ConstantSuffix);

            if (query.CommandType.HasValue)
                writer.WriteRaw(", System.Data.CommandType.")
                      .WriteRaw(query.CommandType.Value);

            if (query.Parameters.Any())
                writer.WriteRaw(", @params");

            writer.WriteRaw(')');

            if (query.Async)
                writer.WriteRaw(".ConfigureAwait(false)");

            writer.WriteLineRaw(";");
        }

        private static void WriteSingleResult(StringWriter writer, SqlStatementInfo query, DaoCodeGenerationContext context)
        {
            SqlQueryResult singleResult = query.Results.Single();
            writer.Write("return ");

            if (query.Async)
                writer.WriteRaw("await ");

            writer.WriteRaw("accessor.")
                  .WriteRaw(GetExecutorMethodName(query.Results[0].ResultMode));

            if (query.Async)
                writer.WriteRaw("Async");

            writer.WriteRaw('<');

            for (int i = 0; i < singleResult.Types.Count; i++)
            {
                string returnType = context.ResolveTypeName(singleResult.Types[i]);
                writer.WriteRaw(returnType);
                if (i + 1 < singleResult.Types.Count)
                    writer.WriteRaw(", ");
            }

            if (singleResult.ProjectToType != null)
                writer.WriteRaw(", ")
                      .WriteRaw(context.ResolveTypeName(singleResult.ProjectToType));

            writer.WriteRaw(">(")
                  .WriteRaw(query.Name)
                  .WriteRaw(ConstantSuffix);

            if (query.CommandType.HasValue)
                writer.WriteRaw($", {typeof(CommandType).FullName}.")
                      .WriteRaw(query.CommandType.Value);

            if (query.Parameters.Any())
                writer.WriteRaw(", @params");

            if (query.Results[0].Types.Count > 1)
            {
                if (!String.IsNullOrEmpty(query.Results[0].Converter))
                    writer.WriteRaw(", ")
                          .WriteRaw(query.Results[0].Converter);

                writer.WriteRaw(", \"")
                      .WriteRaw(query.Results[0].SplitOn)
                      .WriteRaw('"');
            }

            writer.WriteRaw(')');

            if (query.Async)
                writer.WriteRaw(".ConfigureAwait(false)");

            writer.WriteLineRaw(";");
        }

        private static void WriteComplexResult(StringWriter writer, SqlStatementInfo query, DaoCodeGenerationContext context)
        {
            writer.Write("using (IMultipleResultReader reader = ");

            if (query.Async)
                writer.WriteRaw("await ");

            writer.WriteRaw("accessor.QueryMultiple");

            if (query.Async)
                writer.WriteRaw("Async");

            writer.WriteRaw('(')
                  .WriteRaw(query.Name)
                  .WriteRaw(ConstantSuffix);

            if (query.CommandType.HasValue)
                writer.WriteRaw($", {typeof(CommandType).FullName}.")
                      .WriteRaw(query.CommandType.Value);

            if (query.Parameters.Any())
                writer.WriteRaw(", @params");

            writer.WriteRaw(")");

            if (query.Async)
                writer.WriteRaw(".ConfigureAwait(false)");

            writer.WriteLineRaw(")")
                  .WriteLine("{")
                  .PushIndent();

            WriteComplexResultBody(writer, query, context);

            writer.PopIndent()
                  .WriteLine("}");
        }

        private static void WriteComplexResultBody(StringWriter writer, SqlStatementInfo query, DaoCodeGenerationContext context)
        {
            string resultTypeName = GetComplexTypeName(query, context);

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

                for (int j = 0; j < result.Types.Count; j++)
                {
                    string returnType = context.ResolveTypeName(result.Types[j]);
                    writer.WriteRaw(returnType);
                    if (j + 1 < result.Types.Count)
                        writer.WriteRaw(", ");
                }

                if (result.ProjectToType != null)
                    writer.WriteRaw(", ")
                          .WriteRaw(context.ResolveTypeName(result.ProjectToType));

                writer.WriteRaw(">(");

                if (result.Types.Count > 1)
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

        private static string GetComplexTypeName(SqlStatementInfo statement, DaoCodeGenerationContext context)
        {
            if (!(statement.ResultType is SchemaTypeReference schemaTypeReference))
                throw new InvalidOperationException($"Unexpected result type for grid result: {statement.ResultType}");

            if (statement.GenerateResultClass)
            {
                ObjectSchema schema = (ObjectSchema)context.GetSchema(schemaTypeReference);
                if (schema == null)
                    return null;

                StringBuilder sb = new StringBuilder();
                if (context.WriteNamespaces)
                    sb.Append(schema.Namespace)
                      .Append('.');

                sb.Append(schema.DefinitionName);
                return sb.ToString();
            }

            return schemaTypeReference.Key;
        }
        #endregion
    }
}