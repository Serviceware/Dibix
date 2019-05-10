using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class DaoExecutorWriter : IDaoWriter
    {
        #region Fields
        private const string ConstantSuffix = "CommandText";
        private const string MethodPrefix = "";//"Execute";
        private const string ComplexResultTypeSuffix = "Result";
        #endregion

        #region Properties
        public string RegionName => "Accessor";
        #endregion

        #region IDaoWriter Members
        public bool HasContent(IEnumerable<SqlStatementInfo> statements) => statements.Any();

        public void Write(DaoWriterContext context)
        {
            context.Output.AddUsing(typeof(GeneratedCodeAttribute).Namespace);

            // Class
            CSharpClass @class = context.Output.AddClass(context.ClassName, CSharpModifiers.Internal | CSharpModifiers.Static, context.GeneratedCodeAnnotation);

            // Command text constants
            AddCommandTextConstants(@class, context);

            // Execution methods
            @class.AddSeparator();
            AddExecutionMethods(@class, context);

            // Add method info accessor fields
            // This is useful for dynamic invocation like in WAX
            @class.AddSeparator();
            AddMethodInfoFields(@class, context);
        }
        #endregion

        #region Private Methods
        private static void AddCommandTextConstants(CSharpClass @class, DaoWriterContext context)
        {
            for (int i = 0; i < context.Statements.Count; i++)
            {
                SqlStatementInfo statement = context.Statements[i];
                //@class.AddComment(String.Concat("file:///", statement.SourcePath.Replace(" ", "%20").Replace(@"\", "/")), false);
                @class.AddComment(statement.Name, false);
                @class.AddField(name: String.Concat(statement.Name, ConstantSuffix)
                              , type: typeof(string).ToCSharpTypeName()
                              , value: new CSharpStringValue(context.FormatCommandText(statement.Content, context.Formatting), context.Formatting.HasFlag(CommandTextFormatting.Verbatim))
                              , modifiers: CSharpModifiers.Public | CSharpModifiers.Const);

                if (i + 1 < context.Statements.Count)
                    @class.AddSeparator();
            }
        }

        private static void AddExecutionMethods(CSharpClass @class, DaoWriterContext context)
        {
            context.Output.AddUsing("Dibix");

            IDictionary<SqlStatementInfo, string> methodReturnTypeMap = context.Statements.ToDictionary(x => x, DetermineResultTypeName);

            for (int i = 0; i < context.Statements.Count; i++)
            {
                SqlStatementInfo statement = context.Statements[i];
                bool isSingleResult = statement.Results.Count == 1;

                if (isSingleResult && statement.Results[0].ResultMode == SqlQueryResultMode.Many)
                    context.Output.AddUsing(typeof(IEnumerable<>).Namespace);

                string resultTypeName = methodReturnTypeMap[statement];
                CSharpMethod method = @class.AddMethod(name: String.Concat(MethodPrefix, statement.Name)
                                                     , type: resultTypeName
                                                     , body: GenerateMethodBody(statement, context)
                                                     , isExtension: true
                                                     , modifiers: CSharpModifiers.Public | CSharpModifiers.Static);
                method.AddParameter("databaseAccessorFactory", "IDatabaseAccessorFactory");
                foreach (SqlQueryParameter parameter in statement.Parameters)
                    method.AddParameter(parameter.Name, parameter.ClrTypeName);

                if (i + 1 < context.Statements.Count)
                    @class.AddSeparator();
            }
        }

        private static string DetermineResultTypeName(SqlStatementInfo query)
        {
            if (query.Results.Count == 0) // Execute/ExecutePrimitive.
            {
                return typeof(int).ToCSharpTypeName();
            }

            if (query.Results.Count == 1) // Query<T>/QuerySingle/etc.
            {
                string resultTypeName = query.Results[0].Contracts.First().Name.ToString();
                if (query.Results[0].ResultMode == SqlQueryResultMode.Many)
                    resultTypeName = MakeEnumerableType(resultTypeName);

                return resultTypeName;
            }

            // GridReader
            return GetComplexTypeName(query);
        }

        private static void AddMethodInfoFields(CSharpClass @class, DaoWriterContext context)
        {
            Type methodInfoType = typeof(MethodInfo);
            context.Output.AddUsing(methodInfoType.Namespace);
            foreach (SqlStatementInfo statement in context.Statements)
            {
                @class.AddField(name: String.Concat(statement.Name, methodInfoType.Name)
                              , type: methodInfoType.Name
                              , value: new CSharpValue($"typeof({context.ClassName}).GetMethod(\"{statement.Name}\")")
                              , modifiers: CSharpModifiers.Public | CSharpModifiers.Static | CSharpModifiers.ReadOnly);
            }
        }

        private static string GenerateMethodBody(SqlStatementInfo statement, DaoWriterContext context)
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
                  .PushCustomIndent(37);

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
                  .PopCustomIndent();
        }

        private static void WriteExecutor(StringWriter writer, SqlStatementInfo query)
        {
            if (query.Results.Count == 0) // Execute/ExecutePrimitive.
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
                string returnTypeName = singleResult.Contracts[i].Name.ToString();
                writer.WriteRaw(returnTypeName);
                if (i + 1 < singleResult.Contracts.Count)
                    writer.WriteRaw(", ");
            }
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
                writer.WriteRaw(", ")
                      .WriteRaw(query.Results[0].Converter)
                      .WriteRaw(", \"")
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
            string clrTypeName = GetComplexTypeName(query);

            writer.Write(clrTypeName)
                  .WriteRaw(" result = new ")
                  .WriteRaw(clrTypeName)
                  .WriteLineRaw("();");

            foreach (SqlQueryResult result in query.Results)
            {
                writer.Write("result.")
                      .WriteRaw(result.Name);

                bool isEnumerable = result.ResultMode == SqlQueryResultMode.Many;
                if (isEnumerable)
                    writer.WriteRaw(".ReplaceWith(");
                else
                    writer.WriteRaw(" = ");

                writer.WriteRaw("reader.")
                      .WriteRaw(GetMultipleResultReaderMethodName(result.ResultMode))
                      .WriteRaw('<');

                for (int i = 0; i < result.Contracts.Count; i++)
                {
                    string returnTypeName = result.Contracts[i].Name.ToString();
                    writer.WriteRaw(returnTypeName);
                    if (i + 1 < result.Contracts.Count)
                        writer.WriteRaw(", ");
                }

                writer.WriteRaw(">(");

                if (result.Contracts.Count > 1)
                {
                    writer.WriteRaw(result.Converter)
                          .WriteRaw(", \"")
                          .WriteRaw(result.SplitOn)
                          .WriteRaw('"');
                }

                writer.WriteRaw(')');

                if (isEnumerable)
                    writer.WriteRaw(')');

                writer.WriteLineRaw(";");
            }

            writer.WriteLine("return result;");
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
            return statement.ResultTypeName ?? String.Concat(statement.Name, ComplexResultTypeSuffix);
        }
        #endregion
    }
}