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
        private readonly ICollection<SqlStatementDefinition> _schemas;
        #endregion

        #region Properties
        public override string LayerName => CodeGeneration.LayerName.Data;
        public override string RegionName => "Accessor";
        #endregion

        #region Constructor
        public DaoExecutorWriter(CodeGenerationModel model, CodeGenerationOutputFilter outputFilter)
        {
            _schemas = model.GetSchemas(outputFilter)
                            .OfType<SqlStatementDefinition>()
                            .ToArray();
        }
        #endregion

        #region Overrides
        public override bool HasContent(CodeGenerationModel model) => _schemas.Any();

        public override void Write(CodeGenerationContext context)
        {
            context.AddUsing("Dibix");

            var namespaceGroups = _schemas.GroupBy(x => x.Namespace).OrderBy(x => x.Key).ToArray();

            for (int i = 0; i < namespaceGroups.Length; i++)
            {
                IGrouping<string, SqlStatementDefinition> namespaceGroup = namespaceGroups[i];
                CSharpStatementScope scope = /*namespaceGroup.Key != null ? */context.CreateOutputScope(namespaceGroup.Key) /* : context.Output*/;
                IList<SqlStatementDefinition> statementDescriptors = namespaceGroup.OrderBy(x => x.DefinitionName).ToArray();

                // Class
                ICollection<CSharpAnnotation> annotations = new Collection<CSharpAnnotation> { new CSharpAnnotation("DatabaseAccessor") };
                CSharpClass @class = scope.AddClass(context.Model.DefaultClassName, CSharpModifiers.Public | CSharpModifiers.Static, annotations);

                // Command text constants
                AddCommandTextConstants(@class, context, statementDescriptors);

                // Execution methods
                @class.AddSeparator();
                AddExecutionMethods(@class, context, statementDescriptors);

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
                @class.AddField(name: String.Concat(definition.DefinitionName, ConstantSuffix)
                              , type: "string"
                              , value: new CSharpStringValue(definition.Statement.Content, context.Model.CommandTextFormatting == CommandTextFormatting.MultiLine)
                              , modifiers: CSharpModifiers.Private | CSharpModifiers.Const);

                if (i + 1 < definitions.Count)
                    @class.AddSeparator();
            }
        }

        private static void AddExecutionMethods(CSharpClass @class, CodeGenerationContext context, IList<SqlStatementDefinition> definitions)
        {
            for (int i = 0; i < definitions.Count; i++)
            {
                SqlStatementDefinition definition = definitions[i];

                string methodName = String.Concat(MethodPrefix, definition.DefinitionName);
                if (definition.Async)
                    methodName = $"{methodName}Async";

                string resultTypeName = ResolveTypeName(definition, context);
                string returnTypeName = DetermineReturnTypeName(definition, resultTypeName, context);

                CSharpModifiers modifiers = CSharpModifiers.Public | CSharpModifiers.Static;
                if (definition.Async)
                    modifiers |= CSharpModifiers.Async;

                CSharpMethod method = @class.AddMethod(name: methodName
                                                     , returnType: returnTypeName
                                                     , body: GenerateMethodBody(definition, resultTypeName, context)
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
                        method.AddParameter(parameter.Name, context.ResolveTypeName(parameter.Type), defaultValue, parameterKind);
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

        private static string DetermineReturnTypeName(SqlStatementDefinition definition, string resultTypeName, CodeGenerationContext context)
        {
            if (!definition.Async) 
                return resultTypeName;

            context.AddUsing<Task>();
            StringBuilder sb = new StringBuilder(nameof(Task));
            if (definition.ResultType != null)
                sb.Append('<')
                  .Append(resultTypeName)
                  .Append('>');

            string returnTypeName = sb.ToString();
            return returnTypeName;
        }

        private static string ResolveTypeName(SqlStatementDefinition definition, CodeGenerationContext context)
        {
            if (definition.IsGridResult())
                return GetComplexTypeName(definition, context);

            return context.ResolveTypeName(definition.ResultType);
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

        private static void WriteParameters(StringWriter writer, SqlStatementDefinition definition, CodeGenerationContext context)
        {
            writer.WriteLine("ParametersVisitor @params = accessor.Parameters()")
                  .SetTemporaryIndent(36);

            bool hasImplicitParameters = definition.GenerateInputClass || definition.Parameters.Any(x => !x.HasParameterOptions());

            if (hasImplicitParameters)
            {
                writer.Write(".SetFromTemplate(");

                if (definition.GenerateInputClass)
                    writer.WriteRaw("input");
                else
                {
                    writer.WriteLineRaw("new")
                          .WriteLine("{")
                          .PushIndent();

                    for (int i = 0; i < definition.Parameters.Count; i++)
                    {
                        SqlQueryParameter parameter = definition.Parameters[i];
                        if (parameter.HasParameterOptions())
                            continue;

                        writer.Write(parameter.Name);

                        if (i + 1 < definition.Parameters.Count)
                            writer.WriteRaw(",");

                        writer.WriteLine();
                    }

                    writer.PopIndent()
                          .Write("}");
                }

                writer.WriteLineRaw(")");
            }

            if (!definition.GenerateInputClass)
            {
                foreach (SqlQueryParameter parameter in definition.Parameters)
                {
                    if (!parameter.HasParameterOptions())
                        continue;

                    string methodName = GetSetParameterMethodName(parameter.Type);
                    writer.Write($".{methodName}(nameof({parameter.Name}), ");

                    int? stringSize = parameter.Type.GetStringSize();
                    if (parameter.IsOutput)
                    {
                        string clrTypeName = context.ResolveTypeName(parameter.Type);
                        writer.WriteRaw($"out IOutParameter<{clrTypeName}> {parameter.Name}Output");
                    }
                    else
                    {
                        writer.WriteRaw($"{parameter.Name}");

                        if (stringSize != null)
                            writer.WriteRaw($", size: {stringSize}");

                        if (parameter.Obfuscate)
                            writer.WriteRaw(", obfuscate: true");
                    }

                    writer.WriteLineRaw(")");
                }
            }

            writer.WriteLine(".Build();")
                  .ResetTemporaryIndent();
        }

        private static void WriteExecutor(StringWriter writer, SqlStatementDefinition definition, string resultTypeName, bool hasOutputParameters, CodeGenerationContext context)
        {
            if (definition.Results.Count > 1) // GridReader
            {
                WriteComplexResult(writer, definition, resultTypeName, hasOutputParameters, context);
            }
            else if (definition.Results.Count <= 1) // Execute or Query<T>/QuerySingle<T>/etc.
            {
                WriteSimpleResult(writer, definition, resultTypeName, hasOutputParameters, context);
            }
            else
            {
                throw new InvalidOperationException("Unable to determine executor");
            }
        }

        private static void WriteSimpleResult(StringWriter writer, SqlStatementDefinition definition, string resultTypeName, bool hasOutputParameters, CodeGenerationContext context)
        {
            SqlQueryResult singleResult = definition.Results.SingleOrDefault();
            bool isGridResult = singleResult?.Name != null;

            if (isGridResult)
            {
                WriteComplexResultInitializer(writer, definition, resultTypeName, hasOutputParameters);
                WriteComplexResultAssignment(writer, definition, singleResult, context, isFirstResult: true, WriteSimpleMethodCall);
            }

            writer.WriteIndent();

            if (singleResult != null) 
                WriteResultInitialization(writer, resultTypeName, hasOutputParameters);

            if (!isGridResult)
                WriteSimpleMethodCall(writer, definition, singleResult, context);
            else if (!hasOutputParameters)
                writer.WriteRaw("result");

            writer.WriteLineRaw(";");
        }

        private static void WriteSimpleMethodCall(StringWriter writer, SqlStatementDefinition definition, SqlQueryResult singleResult, CodeGenerationContext context)
        {
            string methodName = singleResult != null ? GetExecutorMethodName(singleResult.ResultMode) : "Execute";
            WriteMethodCall(writer, definition, methodName, singleResult, context);
        }

        private static void WriteComplexResult(StringWriter writer, SqlStatementDefinition definition, string resultTypeName, bool hasOutputParameters, CodeGenerationContext context)
        {
            if (hasOutputParameters)
            {
                writer.Write(resultTypeName)
                      .WriteLineRaw(" result;");
            }

            writer.Write("using (IMultipleResultReader reader = ");

            WriteMethodCall(writer, definition, "QueryMultiple", null, context);

            writer.WriteLineRaw(")")
                  .WriteLine("{")
                  .PushIndent();

            WriteComplexResultBody(writer, definition, resultTypeName, hasOutputParameters, context);

            writer.PopIndent()
                  .WriteLine("}");
        }

        private static void WriteComplexResultBody(StringWriter writer, SqlStatementDefinition definition, string resultTypeName, bool hasOutputParameters, CodeGenerationContext context)
        {
            WriteComplexResultInitializer(writer, definition, resultTypeName, hasOutputParameters);

            bool performNullCheck = false;
            for (int i = 0; i < definition.Results.Count; i++)
            {
                SqlQueryResult result = definition.Results[i];
                bool isFirstResult = i == 0;
                bool isLastResult = i + 1 == definition.Results.Count;

                WriteComplexResultAssignment(writer, definition, result, context, isFirstResult, WriteGridReaderMethodCall);

                // Make sure subsequent results are not merged, when the root result returned null
                if (isFirstResult) 
                    performNullCheck = definition.MergeGridResult && result.ResultMode == SqlQueryResultMode.SingleOrDefault;

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

        private static void WriteComplexResultInitializer(StringWriter writer, SqlStatementDefinition definition, string resultTypeName, bool hasOutputParameter)
        {
            writer.WriteIndent();

            if (!hasOutputParameter)
            {
                writer.WriteRaw(resultTypeName)
                      .WriteRaw(" ");
            }

            writer.WriteRaw("result = ");

            if (!definition.MergeGridResult)
            {
                writer.WriteRaw("new ")
                      .WriteRaw(resultTypeName)
                      .WriteLineRaw("();");
            }
        }

        private static void WriteComplexResultAssignment(StringWriter writer, SqlStatementDefinition definition, SqlQueryResult result, CodeGenerationContext context, bool isFirstResult, Action<StringWriter, SqlStatementDefinition, SqlQueryResult, CodeGenerationContext> valueWriter)
        {
            bool isEnumerable = result.ResultMode == SqlQueryResultMode.Many;
            if (!isFirstResult || !definition.MergeGridResult)
            {
                writer.Write("result")
                      .WriteRaw($".{result.Name.Value}")
                      .WriteRaw(isEnumerable ? ".ReplaceWith(" : " = ");
            }

            valueWriter(writer, definition, result, context);

            if (isEnumerable && (!isFirstResult || !definition.MergeGridResult))
                writer.WriteRaw(')'); // ReplaceWith

            writer.WriteLineRaw(";");
        }

        private static void WriteGridReaderMethodCall(StringWriter writer, SqlStatementDefinition definition, SqlQueryResult result, CodeGenerationContext context)
        {
            if (definition.Async)
                writer.WriteRaw("await ");

            writer.WriteRaw("reader.")
                  .WriteRaw(GetMultipleResultReaderMethodName(result.ResultMode));

            if (definition.Async)
                writer.WriteRaw("Async");

            WriteGenericTypeArguments(writer, result, context);

            ICollection<string> parameters = new Collection<string>();

            AppendMultiMapParameters(result, parameters, context);

            WriteMethodParameters(writer, parameters);

            if (definition.Async)
                writer.WriteRaw(".ConfigureAwait(false)");
        }

        private static void WriteMethodCall(StringWriter writer, SqlStatementDefinition definition, string methodName, SqlQueryResult singleResult, CodeGenerationContext context)
        {
            if (definition.Async)
                writer.WriteRaw("await ");

            writer.WriteRaw("accessor.")
                  .WriteRaw(methodName);

            if (singleResult?.ProjectToType != null)
                writer.WriteRaw("Projection");

            if (definition.Async)
                writer.WriteRaw("Async");

            if (singleResult != null)
                WriteGenericTypeArguments(writer, singleResult, context);

            ICollection<string> parameters = new Collection<string>();

            context.AddUsing<CommandType>();

            parameters.Add($"{definition.DefinitionName}{ConstantSuffix}");
            parameters.Add($"{nameof(CommandType)}.{definition.Statement.CommandType}");
            parameters.Add(definition.Parameters.Any() ? "@params" : "ParametersVisitor.Empty");

            if (singleResult != null)
                AppendMultiMapParameters(singleResult, parameters, context);

            if (definition.Async)
                parameters.Add("cancellationToken");

            WriteMethodParameters(writer, parameters);

            if (definition.Async)
                writer.WriteRaw(".ConfigureAwait(false)");
        }

        private static void WriteGenericTypeArguments(StringWriter writer, SqlQueryResult result, CodeGenerationContext context)
        {
            writer.WriteRaw('<')
                  .WriteRaw(context.ResolveTypeName(result.ReturnType, enumerableBehavior: EnumerableBehavior.None))
                  .WriteRaw('>');
        }

        private static void AppendMultiMapParameters(SqlQueryResult result, ICollection<string> parameters, CodeGenerationContext context)
        {
            if (result.Types.Count > 1)
            {
                if (!String.IsNullOrEmpty(result.Converter))
                    parameters.Add(result.Converter);

                parameters.Add($"new[] {{ {String.Join(", ", result.Types.Select(x => $"typeof({context.ResolveTypeName(x, enumerableBehavior: EnumerableBehavior.None)})"))} }}");
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
                return schema != null ? $"{schema.Namespace}.{schema.DefinitionName}" : null;
            }

            return schemaTypeReference.Key;
        }
        #endregion
    }
}