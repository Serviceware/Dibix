using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
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
                              , value: new CSharpStringValue(statement.Content, context.Model.CommandTextFormatting == CommandTextFormatting.MultiLine)
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

                IEnumerable<string> annotations = statement.ErrorResponses.Select(x => $"ErrorResponse(statusCode: {x.StatusCode}, errorCode: {x.ErrorCode}, errorDescription: \"{x.ErrorDescription}\", isClientError: {x.IsClientError.ToString().ToLowerInvariant()})");
                if (statement.ErrorResponses.Any())
                    context.AddUsing("Dibix.Http");

                CSharpMethod method = @class.AddMethod(name: methodName
                                                     , type: resultTypeName
                                                     , body: GenerateMethodBody(statement, context)
                                                     , annotations: annotations
                                                     , isExtension: true
                                                     , modifiers: CSharpModifiers.Public | CSharpModifiers.Static);
                method.AddParameter("databaseAccessorFactory", "IDatabaseAccessorFactory");

                if (statement.GenerateInputClass)
                    method.AddParameter("input", $"{statement.Name}{DaoExecutorInputClassWriter.InputTypeSuffix}", "InputClass");
                else
                {
                    foreach (SqlQueryParameter parameter in statement.Parameters)
                    {
                        ParameterKind parameterKind = parameter.IsOutput ? ParameterKind.Out : ParameterKind.Value;
                        CSharpValue defaultValue = parameter.HasDefaultValue ? ParseDefaultValue(parameter.DefaultValue) : null;
                        method.AddParameter(parameter.Name, context.ResolveTypeName(parameter.Type), parameterKind, defaultValue);
                    }
                }

                if (statement.Async)
                {
                    context.AddUsing(typeof(CancellationToken).Namespace);
                    method.AddParameter("cancellationToken", "CancellationToken", default, new CSharpValue("default"));
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
                WriteParameters(writer, statement, context);

            WriteExecutor(writer, statement, context);

            WriteOutputParameterAssignment(writer, statement);

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

        private static void WriteParameters(StringWriter writer, SqlStatementInfo query, DaoCodeGenerationContext context)
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
                    if (parameter.Obfuscate || parameter.IsOutput)
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

            foreach (SqlQueryParameter parameter in query.Parameters)
            {
                if (parameter.IsOutput)
                {
                    string methodName = GetSetParameterMethodName(parameter.Type);
                    string clrTypeName = context.ResolveTypeName(parameter.Type);
                    writer.WriteLine($".{methodName}(nameof({parameter.Name}), out IOutParameter<{clrTypeName}> {parameter.Name}Output)");
                }

                if (parameter.Obfuscate)
                    writer.WriteLine($".SetString(nameof({parameter.Name}), {parameter.Name}, true)");
            }

            writer.WriteLine(".Build();")
                  .ResetTemporaryIndent();
        }

        private static void WriteExecutor(StringWriter writer, SqlStatementInfo query, DaoCodeGenerationContext context)
        {
            if (query.IsFileApi)
            {
                WriteFileApiResult(writer, query, context);
            }
            else if (query.Results.Count > 1) // GridReader
            {
                WriteComplexResult(writer, query, context);
            }
            else if (query.Results.Count <= 1) // Execute or Query<T>/QuerySingle<T>/etc.
            {
                WriteSimpleResult(writer, query, context);
            }
            else
            {
                throw new InvalidOperationException("Unable to determine executor");
            }
        }

        private static void WriteSimpleResult(StringWriter writer, SqlStatementInfo query, DaoCodeGenerationContext context)
        {
            SqlQueryResult singleResult = query.Results.SingleOrDefault();
            bool isGridResult = singleResult?.Name != null;

            if (isGridResult)
            {
                WriteComplexResultInitializer(writer, query, context);
                WriteComplexResultAssignment(writer, query, singleResult, context, isFirstResult: true, WriteSimpleMethodCall);
            }

            writer.WriteIndent();

            if (singleResult != null)
                writer.WriteRaw("return ");

            if (!isGridResult)
                WriteSimpleMethodCall(writer, query, singleResult, context);
            else
                writer.WriteRaw("result");

            writer.WriteLineRaw(";");
        }

        private static void WriteSimpleMethodCall(StringWriter writer, SqlStatementInfo query, SqlQueryResult singleResult, DaoCodeGenerationContext context)
        {
            string methodName = singleResult != null ? GetExecutorMethodName(singleResult.ResultMode) : "Execute";
            WriteMethodCall(writer, query, methodName, singleResult, context);
        }

        private static void WriteComplexResult(StringWriter writer, SqlStatementInfo query, DaoCodeGenerationContext context)
        {
            writer.Write("using (IMultipleResultReader reader = ");

            WriteMethodCall(writer, query, "QueryMultiple", null, context);

            writer.WriteLineRaw(")")
                  .WriteLine("{")
                  .PushIndent();

            WriteComplexResultBody(writer, query, context);

            writer.PopIndent()
                  .WriteLine("}");
        }

        private static void WriteComplexResultBody(StringWriter writer, SqlStatementInfo query, DaoCodeGenerationContext context)
        {
            WriteComplexResultInitializer(writer, query, context);

            for (int i = 0; i < query.Results.Count; i++)
            {
                SqlQueryResult result = query.Results[i];
                bool isFirstResult = i == 0;

                WriteComplexResultAssignment(writer, query, result, context, isFirstResult, WriteGridReaderMethodCall);

                // Make sure subsequent results are not merged, when the root result returned null
                if (query.MergeGridResult && isFirstResult && result.ResultMode == SqlQueryResultMode.SingleOrDefault)
                {
                    writer.WriteLine("if (result == null)")
                          .PushIndent()
                          .WriteLine("return null;")
                          .PopIndent()
                          .WriteLine();
                }
            }

            writer.WriteLine("return result;");
        }

        private static void WriteComplexResultInitializer(StringWriter writer, SqlStatementInfo query, DaoCodeGenerationContext context)
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
        }

        private static void WriteComplexResultAssignment(StringWriter writer, SqlStatementInfo query, SqlQueryResult result, DaoCodeGenerationContext context, bool isFirstResult, Action<StringWriter, SqlStatementInfo, SqlQueryResult, DaoCodeGenerationContext> valueWriter)
        {
            bool isEnumerable = result.ResultMode == SqlQueryResultMode.Many;
            if (!isFirstResult || !query.MergeGridResult)
            {
                writer.Write("result")
                      .WriteRaw($".{result.Name}");

                if (isEnumerable)
                    writer.WriteRaw(".ReplaceWith(");
                else
                    writer.WriteRaw(" = ");
            }

            valueWriter(writer, query, result, context);

            if (isEnumerable && (!isFirstResult || !query.MergeGridResult))
                writer.WriteRaw(')'); // ReplaceWith

            writer.WriteLineRaw(";");
        }

        private static void WriteGridReaderMethodCall(StringWriter writer, SqlStatementInfo query, SqlQueryResult result, DaoCodeGenerationContext context)
        {
            writer.WriteRaw("reader.")
                  .WriteRaw(GetMultipleResultReaderMethodName(result.ResultMode));

            WriteGenericTypeArguments(writer, result, context);

            ICollection<string> parameters = new Collection<string>();

            AppendMultiMapParameters(result, parameters);

            WriteMethodParameters(writer, parameters);
        }

        private static void WriteFileApiResult(StringWriter writer, SqlStatementInfo query, DaoCodeGenerationContext context)
        {
            writer.Write("return ");

            WriteMethodCall(writer, query, "QueryFile", null, context);

            writer.WriteLineRaw(";");
        }

        private static void WriteMethodCall(StringWriter writer, SqlStatementInfo query, string methodName, SqlQueryResult singleResult, DaoCodeGenerationContext context)
        {
            if (query.Async)
                writer.WriteRaw("await ");

            writer.WriteRaw("accessor.")
                  .WriteRaw(methodName);

            if (query.Async)
                writer.WriteRaw("Async");

            if (singleResult != null)
                WriteGenericTypeArguments(writer, singleResult, context);

            ICollection<string> parameters = new Collection<string>();

            parameters.Add($"{query.Name}{ConstantSuffix}");

            if (query.CommandType.HasValue)
                parameters.Add($"{typeof(CommandType).FullName}.{query.CommandType.Value}");

            if (query.Parameters.Any())
                parameters.Add("@params");

            if (singleResult != null)
                AppendMultiMapParameters(singleResult, parameters);

            if (query.Async)
                parameters.Add("cancellationToken");

            WriteMethodParameters(writer, parameters);

            if (query.Async)
                writer.WriteRaw(".ConfigureAwait(false)");
        }

        private static void WriteGenericTypeArguments(StringWriter writer, SqlQueryResult result, DaoCodeGenerationContext context)
        {
            writer.WriteRaw('<');

            for (int i = 0; i < result.Types.Count; i++)
            {
                string returnType = context.ResolveTypeName(result.Types[i]);
                writer.WriteRaw(returnType);
                if (i + 1 < result.Types.Count)
                    writer.WriteRaw(", ");
            }

            if (result.ProjectToType != null)
                writer.WriteRaw(", ")
                      .WriteRaw(context.ResolveTypeName(result.ProjectToType));

            writer.WriteRaw('>');
        }

        private static void AppendMultiMapParameters(SqlQueryResult result, ICollection<string> parameters)
        {
            if (result.Types.Count > 1)
            {
                if (!String.IsNullOrEmpty(result.Converter))
                    parameters.Add(result.Converter);

                parameters.Add($"\"{result.SplitOn}\"");
            }
        }

        private static void WriteMethodParameters(StringWriter writer, IEnumerable<string> parameters)
        {
            writer.WriteRaw('(')
                  .WriteRaw(String.Join(", ", parameters))
                  .WriteRaw(')');
        }

        private static void WriteOutputParameterAssignment(StringWriter writer, SqlStatementInfo statement)
        {
            if (!statement.Parameters.Any(x => x.IsOutput))
                return;

            writer.WriteLine();

            foreach (SqlQueryParameter parameter in statement.Parameters.Where(parameter => parameter.IsOutput))
            {
                writer.WriteLine($"{parameter.Name} = {parameter.Name}Output.Result;");
            }
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

        private static string GetSetParameterMethodName(TypeReference typeReference)
        {
            switch (typeReference)
            {
                case PrimitiveTypeReference primitiveTypeReference: return GetSetParameterMethodName(primitiveTypeReference.Type);
                default: throw new ArgumentOutOfRangeException(nameof(typeReference), typeReference, null);
            }
        }
        private static string GetSetParameterMethodName(PrimitiveDataType dataType)
        {
            switch (dataType)
            {
                case PrimitiveDataType.Int32: return "SetInt32";
                case PrimitiveDataType.UUID: return "SetGuid";
                default: throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null);
            }
        }

        private static string MakeEnumerableType(string typeName) => $"IEnumerable<{typeName}>";

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