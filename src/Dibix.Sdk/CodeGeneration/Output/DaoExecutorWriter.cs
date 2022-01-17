using System;
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
    internal sealed class DaoExecutorWriter : ArtifactWriterBase
    {
        #region Fields
        private const string ConstantSuffix = "CommandText";
        private const string MethodPrefix = "";//"Execute";
        private readonly bool _accessorOnly;
        private readonly ICollection<SqlStatementDefinition> _schemas;
        #endregion

        #region Properties
        public override string LayerName => CodeGeneration.LayerName.Data;
        public override string RegionName => "Accessor";
        #endregion

        #region Constructor
        public DaoExecutorWriter(CodeGenerationModel model, SchemaDefinitionSource schemaFilter, bool accessorOnly)
        {
            this._accessorOnly = accessorOnly;
            this._schemas = model.Schemas
                                 .OfType<SqlStatementDefinition>()
                                 .Where(x => schemaFilter.HasFlag(x.Source))
                                 .ToArray();
        }
        #endregion

        #region Overrides
        public override bool HasContent(CodeGenerationModel model) => this._schemas.Any();

        public override void Write(CodeGenerationContext context)
        {
            context.AddUsing("Dibix");

            var namespaceGroups = this._schemas
                                      .GroupBy(x => x.Namespace)
                                      .ToArray();

            for (int i = 0; i < namespaceGroups.Length; i++)
            {
                IGrouping<string, SqlStatementDefinition> namespaceGroup = namespaceGroups[i];
                CSharpStatementScope scope = /*namespaceGroup.Key != null ? */context.CreateOutputScope(namespaceGroup.Key) /* : context.Output*/;
                IList<SqlStatementDefinition> statementDescriptors = namespaceGroup.ToArray();

                // Class
                CSharpModifiers classVisibility = context.GeneratePublicArtifacts ? CSharpModifiers.Public : CSharpModifiers.Internal;
                ICollection<CSharpAnnotation> annotations = new Collection<CSharpAnnotation> { new CSharpAnnotation("DatabaseAccessor") };
                CSharpClass @class = scope.AddClass(context.Model.DefaultClassName, classVisibility | CSharpModifiers.Static, annotations);

                // Command text constants
                AddCommandTextConstants(@class, context, statementDescriptors);

                // Execution methods
                @class.AddSeparator();
                this.AddExecutionMethods(@class, context, statementDescriptors);

                if (i + 1 < namespaceGroups.Length)
                    context.AddSeparator();
            }
        }
        #endregion

        #region Private Methods
        private static void AddCommandTextConstants(CSharpClass @class, CodeGenerationContext context, IList<SqlStatementDefinition> definitions)
        {
            for (int i = 0; i < definitions.Count; i++)
            {
                SqlStatementDefinition definition = definitions[i];
                //@class.AddComment(String.Concat("file:///", definition.SourcePath.Replace(" ", "%20").Replace(@"\", "/")), false);
                @class.AddComment(definition.DefinitionName, false);
                CSharpModifiers fieldVisibility = context.GeneratePublicArtifacts ? CSharpModifiers.Private : CSharpModifiers.Public;
                @class.AddField(name: String.Concat(definition.DefinitionName, ConstantSuffix)
                              , type: "string"
                              , value: new CSharpStringValue(definition.Statement.Content, context.Model.CommandTextFormatting == CommandTextFormatting.MultiLine)
                              , modifiers: fieldVisibility | CSharpModifiers.Const);

                if (i + 1 < definitions.Count)
                    @class.AddSeparator();
            }
        }

        private void AddExecutionMethods(CSharpClass @class, CodeGenerationContext context, IList<SqlStatementDefinition> definitions)
        {
            for (int i = 0; i < definitions.Count; i++)
            {
                SqlStatementDefinition definition = definitions[i];

                string methodName = String.Concat(MethodPrefix, definition.DefinitionName);
                if (definition.Async)
                    methodName = $"{methodName}Async";

                string resultTypeName = ResolveTypeName(definition, context);
                string returnTypeName = DetermineReturnTypeName(definition, resultTypeName, context);

                IEnumerable<CSharpAnnotation> annotations = Enumerable.Empty<CSharpAnnotation>();

                if (!this._accessorOnly && definition.ErrorResponses.Any())
                {
                    annotations = definition.ErrorResponses
                                                     .Select(x => new CSharpAnnotation("ErrorResponse").AddParameter("statusCode", new CSharpValue(x.StatusCode.ToString()))
                                                                                                       .AddParameter("errorCode", new CSharpValue(x.ErrorCode.ToString()))
                                                                                                       .AddParameter("errorDescription", new CSharpStringValue(x.ErrorDescription)));
                    context.AddDibixHttpServerReference();
                }

                CSharpModifiers modifiers = CSharpModifiers.Public | CSharpModifiers.Static;
                if (definition.Async)
                    modifiers |= CSharpModifiers.Async;

                CSharpMethod method = @class.AddMethod(name: methodName
                                                     , returnType: returnTypeName
                                                     , body: GenerateMethodBody(definition, resultTypeName, context)
                                                     , annotations: annotations
                                                     , isExtension: true
                                                     , modifiers: modifiers);
                method.AddParameter("databaseAccessorFactory", "IDatabaseAccessorFactory");

                if (definition.GenerateInputClass)
                    method.AddParameter("input", $"{definition.DefinitionName}{DaoExecutorInputClassWriter.InputTypeSuffix}", new CSharpAnnotation("InputClass"));
                else
                {
                    foreach (SqlQueryParameter parameter in definition.Parameters.OrderBy(x => x.DefaultValue != null))
                    {
                        ParameterKind parameterKind = parameter.IsOutput ? ParameterKind.Out : ParameterKind.Value;
                        CSharpValue defaultValue = parameter.DefaultValue != null ? context.BuildDefaultValueLiteral(parameter.DefaultValue) : null;
                        method.AddParameter(parameter.Name, context.ResolveTypeName(parameter.Type, context), defaultValue, parameterKind);
                    }
                }

                if (definition.Async)
                {
                    context.AddUsing<CancellationToken>();
                    method.AddParameter("cancellationToken", nameof(CancellationToken), new CSharpValue("default"));
                }

                if (i + 1 < definitions.Count)
                    @class.AddSeparator();
            }
        }

        private static string DetermineReturnTypeName(SqlStatementDefinition query, string resultTypeName, CodeGenerationContext context)
        {
            if (!query.Async) 
                return resultTypeName;

            context.AddUsing<Task>();
            StringBuilder sb = new StringBuilder(nameof(Task));
            if (query.ResultType != null)
                sb.Append('<')
                  .Append(resultTypeName)
                  .Append('>');

            string returnTypeName = sb.ToString();
            return returnTypeName;
        }

        private static string ResolveTypeName(SqlStatementDefinition query, CodeGenerationContext context)
        {
            if (query.Results.Any(x => x.Name != null)) // GridResult
                return GetComplexTypeName(query, context);

            return context.ResolveTypeName(query.ResultType, context);
        }

        private static string GenerateMethodBody(SqlStatementDefinition definition, string resultTypeName, CodeGenerationContext context)
        {
            StringWriter writer = new StringWriter();

            if (context.WriteGuardChecks)
            {
                ICollection<SqlQueryParameter> guardParameters = definition.Parameters.Where(x => x.Check != ContractCheck.None).ToArray();
                foreach (SqlQueryParameter parameter in guardParameters)
                    WriteGuardCheck(writer, parameter.Check, parameter.Name);

                if (guardParameters.Any())
                    writer.WriteLine();
            }

            writer.WriteLine("using (IDatabaseAccessor accessor = databaseAccessorFactory.Create())")
                  .WriteLine("{")
                  .PushIndent();

            if (definition.Parameters.Any())
                WriteParameters(writer, definition, context);

            bool hasOutputParameters = definition.Parameters.Any(x => x.IsOutput);
            WriteExecutor(writer, definition, resultTypeName, hasOutputParameters, context);

            WriteOutputParameterAssignment(writer, definition);

            if (hasOutputParameters && definition.ResultType != null)
                writer.WriteLine("return result;");

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

        private static void WriteParameters(StringWriter writer, SqlStatementDefinition query, CodeGenerationContext context)
        {
            writer.WriteLine("ParametersVisitor @params = accessor.Parameters()")
                  .SetTemporaryIndent(36);

            bool hasImplicitParameters = query.GenerateInputClass || query.Parameters.Any(x => !x.IsOutput && !x.Obfuscate);

            if (hasImplicitParameters)
            {
                writer.Write(".SetFromTemplate(");

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
            }

            if (!query.GenerateInputClass)
            {
                foreach (SqlQueryParameter parameter in query.Parameters)
                {
                    if (parameter.IsOutput)
                    {
                        string methodName = GetSetParameterMethodName(parameter.Type);
                        string clrTypeName = context.ResolveTypeName(parameter.Type, context);
                        writer.WriteLine($".{methodName}(nameof({parameter.Name}), out IOutParameter<{clrTypeName}> {parameter.Name}Output)");
                    }

                    if (parameter.Obfuscate)
                        writer.WriteLine($".SetString(nameof({parameter.Name}), {parameter.Name}, true)");
                }
            }

            writer.WriteLine(".Build();")
                  .ResetTemporaryIndent();
        }

        private static void WriteExecutor(StringWriter writer, SqlStatementDefinition query, string resultTypeName, bool hasOutputParameters, CodeGenerationContext context)
        {
            if (query.Results.Count > 1) // GridReader
            {
                WriteComplexResult(writer, query, resultTypeName, hasOutputParameters, context);
            }
            else if (query.Results.Count <= 1) // Execute or Query<T>/QuerySingle<T>/etc.
            {
                WriteSimpleResult(writer, query, resultTypeName, hasOutputParameters, context);
            }
            else
            {
                throw new InvalidOperationException("Unable to determine executor");
            }
        }

        private static void WriteSimpleResult(StringWriter writer, SqlStatementDefinition query, string resultTypeName, bool hasOutputParameters, CodeGenerationContext context)
        {
            SqlQueryResult singleResult = query.Results.SingleOrDefault();
            bool isGridResult = singleResult?.Name != null;

            if (isGridResult)
            {
                WriteComplexResultInitializer(writer, query, resultTypeName, hasOutputParameters);
                WriteComplexResultAssignment(writer, query, singleResult, context, isFirstResult: true, WriteSimpleMethodCall);
            }

            writer.WriteIndent();

            if (singleResult != null) 
                WriteResultInitialization(writer, resultTypeName, hasOutputParameters);

            if (!isGridResult)
                WriteSimpleMethodCall(writer, query, singleResult, context);
            else if (!hasOutputParameters)
                writer.WriteRaw("result");

            writer.WriteLineRaw(";");
        }

        private static void WriteSimpleMethodCall(StringWriter writer, SqlStatementDefinition query, SqlQueryResult singleResult, CodeGenerationContext context)
        {
            string methodName = singleResult != null ? GetExecutorMethodName(singleResult.ResultMode) : "Execute";
            WriteMethodCall(writer, query, methodName, singleResult, context);
        }

        private static void WriteComplexResult(StringWriter writer, SqlStatementDefinition query, string resultTypeName, bool hasOutputParameters, CodeGenerationContext context)
        {
            if (hasOutputParameters)
            {
                writer.Write(resultTypeName)
                      .WriteLineRaw(" result;");
            }

            writer.Write("using (IMultipleResultReader reader = ");

            WriteMethodCall(writer, query, "QueryMultiple", null, context);

            writer.WriteLineRaw(")")
                  .WriteLine("{")
                  .PushIndent();

            WriteComplexResultBody(writer, query, resultTypeName, hasOutputParameters, context);

            writer.PopIndent()
                  .WriteLine("}");
        }

        private static void WriteComplexResultBody(StringWriter writer, SqlStatementDefinition query, string resultTypeName, bool hasOutputParameters, CodeGenerationContext context)
        {
            WriteComplexResultInitializer(writer, query, resultTypeName, hasOutputParameters);

            bool performNullCheck = false;
            for (int i = 0; i < query.Results.Count; i++)
            {
                SqlQueryResult result = query.Results[i];
                bool isFirstResult = i == 0;
                bool isLastResult = i + 1 == query.Results.Count;

                WriteComplexResultAssignment(writer, query, result, context, isFirstResult, WriteGridReaderMethodCall);

                // Make sure subsequent results are not merged, when the root result returned null
                if (isFirstResult) 
                    performNullCheck = query.MergeGridResult && result.ResultMode == SqlQueryResultMode.SingleOrDefault;

                if (!performNullCheck) 
                    continue;

                if (!hasOutputParameters)
                {
                    if (isFirstResult)
                    {
                        writer.WriteLine("if (result == null)")
                              .PushIndent()
                              .WriteLine("return null;")
                              .PopIndent()
                              .WriteLine();
                    }
                    continue;
                }

                if (isFirstResult)
                {
                    writer.WriteLine("if (result != null)")
                          .WriteLine("{")
                          .PushIndent();
                }

                if (isLastResult)
                {
                    writer.PopIndent()
                          .WriteLine("}");
                }
            }

            if (!hasOutputParameters)
                writer.WriteLine("return result;");
        }

        private static void WriteComplexResultInitializer(StringWriter writer, SqlStatementDefinition query, string resultTypeName, bool hasOutputParameter)
        {
            writer.WriteIndent();

            if (!hasOutputParameter)
            {
                writer.WriteRaw(resultTypeName)
                      .WriteRaw(" ");
            }

            writer.WriteRaw("result = ");

            if (!query.MergeGridResult)
            {
                writer.WriteRaw("new ")
                      .WriteRaw(resultTypeName)
                      .WriteLineRaw("();");
            }
        }

        private static void WriteComplexResultAssignment(StringWriter writer, SqlStatementDefinition query, SqlQueryResult result, CodeGenerationContext context, bool isFirstResult, Action<StringWriter, SqlStatementDefinition, SqlQueryResult, CodeGenerationContext> valueWriter)
        {
            bool isEnumerable = result.ResultMode == SqlQueryResultMode.Many;
            if (!isFirstResult || !query.MergeGridResult)
            {
                writer.Write("result")
                      .WriteRaw($".{result.Name}")
                      .WriteRaw(isEnumerable ? ".ReplaceWith(" : " = ");
            }

            valueWriter(writer, query, result, context);

            if (isEnumerable && (!isFirstResult || !query.MergeGridResult))
                writer.WriteRaw(')'); // ReplaceWith

            writer.WriteLineRaw(";");
        }

        private static void WriteGridReaderMethodCall(StringWriter writer, SqlStatementDefinition query, SqlQueryResult result, CodeGenerationContext context)
        {
            if (query.Async)
                writer.WriteRaw("await ");

            writer.WriteRaw("reader.")
                  .WriteRaw(GetMultipleResultReaderMethodName(result.ResultMode));

            if (query.Async)
                writer.WriteRaw("Async");

            WriteGenericTypeArguments(writer, result, context);

            ICollection<string> parameters = new Collection<string>();

            AppendMultiMapParameters(result, parameters);

            WriteMethodParameters(writer, parameters);

            if (query.Async)
                writer.WriteRaw(".ConfigureAwait(false)");
        }

        private static void WriteMethodCall(StringWriter writer, SqlStatementDefinition query, string methodName, SqlQueryResult singleResult, CodeGenerationContext context)
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

            context.AddUsing<CommandType>();

            parameters.Add($"{query.DefinitionName}{ConstantSuffix}");
            parameters.Add($"{nameof(CommandType)}.{query.Statement.CommandType}");
            parameters.Add(query.Parameters.Any() ? "@params" : "ParametersVisitor.Empty");

            if (singleResult != null)
                AppendMultiMapParameters(singleResult, parameters);

            if (query.Async)
                parameters.Add("cancellationToken");

            WriteMethodParameters(writer, parameters);

            if (query.Async)
                writer.WriteRaw(".ConfigureAwait(false)");
        }

        private static void WriteGenericTypeArguments(StringWriter writer, SqlQueryResult result, CodeGenerationContext context)
        {
            writer.WriteRaw('<');

            for (int i = 0; i < result.Types.Count; i++)
            {
                string returnType = context.ResolveTypeName(result.Types[i], context, includeEnumerable: false);
                writer.WriteRaw(returnType);
                if (i + 1 < result.Types.Count)
                    writer.WriteRaw(", ");
            }

            if (result.ProjectToType != null)
                writer.WriteRaw(", ")
                      .WriteRaw(context.ResolveTypeName(result.ProjectToType, context, includeEnumerable: false));

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

        private static void WriteResultInitialization(StringWriter writer, string resultTypeName, bool hasOutputParameters)
        {
            if (hasOutputParameters)
            {
                writer.WriteRaw(resultTypeName)
                      .WriteRaw(" result =");
            }
            else
            {
                writer.WriteRaw("return");
            }
            writer.WriteRaw(' ');
        }

        private static void WriteOutputParameterAssignment(StringWriter writer, SqlStatementDefinition definition)
        {
            if (!definition.Parameters.Any(x => x.IsOutput) || definition.GenerateInputClass)
                return;

            foreach (SqlQueryParameter parameter in definition.Parameters.Where(parameter => parameter.IsOutput))
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
        private static string GetSetParameterMethodName(PrimitiveType dataType)
        {
            switch (dataType)
            {
                case PrimitiveType.Boolean: return "SetBoolean";
                case PrimitiveType.Byte: return "SetByte";
                case PrimitiveType.Int16: return "SetInt16";
                case PrimitiveType.Int32: return "SetInt32";
                case PrimitiveType.Int64: return "SetInt64";
                case PrimitiveType.Float: return "SetSingle";
                case PrimitiveType.Double: return "SetDouble";
                case PrimitiveType.Decimal: return "SetDecimal";
                case PrimitiveType.Binary: return "SetBytes";
                case PrimitiveType.DateTime: return "SetDateTime";
                case PrimitiveType.String: return "SetString";
                case PrimitiveType.UUID: return "SetGuid";
                case PrimitiveType.Xml: return "SetXml";
                default: throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null);
            }
        }

        private static string GetComplexTypeName(SqlStatementDefinition definition, CodeGenerationContext context)
        {
            if (!(definition.ResultType is SchemaTypeReference schemaTypeReference))
                throw new InvalidOperationException($"Unexpected result type for grid result: {definition.ResultType}");

            if (definition.GenerateResultClass)
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