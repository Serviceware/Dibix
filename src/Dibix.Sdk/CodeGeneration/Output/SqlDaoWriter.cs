using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class SqlDaoWriter : SqlWriter, IWriter
    {
        #region Fields
        private static readonly bool WriteGuardChecks = false;
        private static readonly string GeneratorName = typeof(SqlDaoWriter).Assembly.GetName().Name;
        private static readonly string Version = FileVersionInfo.GetVersionInfo(typeof(SqlDaoWriter).Assembly.Location).FileVersion;
        private const string ConstantSuffix = "CommandText";
        private const string MethodPrefix = "";//"Execute";
        private const string ComplexResultTypeSuffix = "Result";
        #endregion

        #region Overrides
        protected override void Write(StringWriter writer, string projectName, string @namespace, string className, SqlQueryOutputFormatting formatting, IList<SqlStatementInfo> statements)
        {
            Type generatedCodeAttributeType = typeof(GeneratedCodeAttribute);
            Type methodInfoType = typeof(MethodInfo);
            bool usesEnumerable = false, usesCollections = false;
            IDictionary<SqlStatementInfo, string> methodReturnTypeMap = statements.ToDictionary(x => x, DetermineResultTypeName);

            // Prepare writer
            CSharpWriter output = CSharpWriter.Init(writer, @namespace)
                                              .AddUsing(generatedCodeAttributeType.Namespace)
                                              .AddUsing(methodInfoType.Namespace)
                                              .AddUsing("Dibix");

            // Class
            string generatedCodeAnnotation = $"{generatedCodeAttributeType.Name}(\"{GeneratorName}\", \"{Version}\")";
            CSharpClass @class = output.AddClass(className, CSharpModifiers.Internal | CSharpModifiers.Static, generatedCodeAnnotation);

            // Command text constants
            AddCommandTextConstants(@class, statements, formatting);
            @class.AddSeparator();

            // Execution methods
            AddExecutionMethods(@class, statements, methodReturnTypeMap, ref usesEnumerable);

            // Grid result types
            AddGridResultTypes(@class, statements, ref usesCollections);

            @class.AddSeparator();

            // Add method info accessor fields
            // This is useful for dynamic invocation like in WAX
            AddMethodInfoFields(@class, className, statements, methodInfoType);

            if (usesEnumerable || usesCollections)
                output.AddUsing(typeof(IEnumerable<>).Namespace);

            if (usesCollections)
                output.AddUsing(typeof(Collection<>).Namespace);

            output.Generate();
        }
        #endregion

        #region Private Methods
        private static void AddCommandTextConstants(CSharpClass @class, IList<SqlStatementInfo> statements, SqlQueryOutputFormatting formatting)
        {
            for (int i = 0; i < statements.Count; i++)
            {
                SqlStatementInfo statement = statements[i];
                //@class.AddComment(String.Concat("file:///", statement.SourcePath.Replace(" ", "%20").Replace(@"\", "/")), false);
                @class.AddComment(statement.Name, false);
                @class.AddField(name: String.Concat(statement.Name, ConstantSuffix)
                              , type: typeof(string).ToCSharpTypeName()
                              , value: new CSharpStringValue(Format(statement.Content, formatting), formatting.HasFlag(SqlQueryOutputFormatting.Verbatim))
                              , modifiers: CSharpModifiers.Public | CSharpModifiers.Const);

                if (i + 1 < statements.Count)
                    @class.AddSeparator();
            }
        }

        private static void AddExecutionMethods(CSharpClass @class, IEnumerable<SqlStatementInfo> statements, IDictionary<SqlStatementInfo, string> methodReturnTypeMap, ref bool usesEnumerable)
        {
            foreach (SqlStatementInfo statement in statements)
            {
                bool isSingleResult = statement.Results.Count == 1;

                if (isSingleResult && statement.Results[0].ResultMode == SqlQueryResultMode.Many)
                    usesEnumerable = true;

                string resultTypeName = methodReturnTypeMap[statement];
                CSharpMethod method = @class.AddMethod(name: String.Concat(MethodPrefix, statement.Name)
                                                     , type: resultTypeName
                                                     , body: GenerateMethodBody(statement)
                                                     , isExtension: true
                                                     , modifiers: CSharpModifiers.Public | CSharpModifiers.Static);
                method.AddParameter("databaseAccessorFactory", "IDatabaseAccessorFactory");
                foreach (SqlQueryParameter parameter in statement.Parameters)
                    method.AddParameter(parameter.Name, parameter.ClrTypeName);
            }
        }

        private static void AddGridResultTypes(CSharpClass @class, IEnumerable<SqlStatementInfo> statements, ref bool usesCollections)
        {
            ICollection<SqlStatementInfo> gridResultStatements = statements.Where(x => x.Results.Count > 1 && x.ResultTypeName == null).ToArray();
            if (gridResultStatements.Any())
                @class.AddSeparator();

            foreach (SqlStatementInfo statement in gridResultStatements)
            {
                CSharpClass complexType = @class.AddClass(GetComplexTypeName(statement));

                IList<SqlQueryResult> collectionProperties = statement.Results.Where(x => x.ResultMode == SqlQueryResultMode.Many).ToArray();

                foreach (SqlQueryResult result in statement.Results)
                {
                    bool isEnumerable = result.ResultMode == SqlQueryResultMode.Many;
                    string resultTypeName = result.Types.First().Name.SimplifiedTypeName;
                    if (isEnumerable)
                        resultTypeName = MakeCollectionInterfaceType(resultTypeName);

                    complexType.AddProperty(result.Name, resultTypeName)
                               .Getter(null)
                               .Setter(null, isEnumerable ? CSharpModifiers.Private : default);
                }

                if (!collectionProperties.Any())
                    continue;

                if (!usesCollections)
                    usesCollections = collectionProperties.Any();

                StringBuilder ctorBodyWriter = new StringBuilder();
                for (int i = 0; i < collectionProperties.Count; i++)
                {
                    SqlQueryResult property = collectionProperties[i];
                    ctorBodyWriter.Append("this.")
                                  .Append(property.Name)
                                  .Append(" = new ")
                                  .Append(MakeCollectionType(property.Types.First().Name.SimplifiedTypeName))
                                  .Append("();");

                    if (i + 1 < collectionProperties.Count)
                        ctorBodyWriter.AppendLine();
                }

                complexType.AddSeparator()
                           .AddConstructor(ctorBodyWriter.ToString());
            }
        }

        private static void AddMethodInfoFields(CSharpClass @class, string className, IEnumerable<SqlStatementInfo> statements, Type methodInfoType)
        {
            foreach (SqlStatementInfo statement in statements)
            {
                @class.AddField(name: String.Concat(statement.Name, methodInfoType.Name)
                              , type: methodInfoType.Name
                              , value: new CSharpValue($"typeof({className}).GetMethod(\"{statement.Name}\")")
                              , modifiers: CSharpModifiers.Public | CSharpModifiers.Static | CSharpModifiers.ReadOnly);
            }
        }

        private static string DetermineResultTypeName(SqlStatementInfo query)
        {
            if (query.Results.Count == 0) // Execute/ExecuteScalar.
            {
                return typeof(int).ToCSharpTypeName();
            }

            if (query.Results.Count == 1) // Query<T>/QuerySingle/etc.
            {
                string resultTypeName = query.Results[0].Types.First().Name.SimplifiedTypeName;
                if (query.Results[0].ResultMode == SqlQueryResultMode.Many)
                    resultTypeName = MakeEnumerableType(resultTypeName);

                return resultTypeName;
            }

            // GridReader
            return GetComplexTypeName(query);
        }

        private static string GenerateMethodBody(SqlStatementInfo statement)
        {
            StringWriter writer = new StringWriter();

            if (WriteGuardChecks)
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
            if (query.Results.Count == 0) // Execute/ExecuteScalar.
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

            for (int i = 0; i < singleResult.Types.Count; i++)
            {
                TypeInfo returnType = singleResult.Types[i];
                writer.WriteRaw(returnType.Name.SimplifiedTypeName);
                if (i + 1 < singleResult.Types.Count)
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

            if (query.Results[0].Types.Count > 1)
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

                for (int i = 0; i < result.Types.Count; i++)
                {
                    TypeInfo returnType = result.Types[i];
                    writer.WriteRaw(returnType.Name.SimplifiedTypeName);
                    if (i + 1 < result.Types.Count)
                        writer.WriteRaw(", ");
                }

                writer.WriteRaw(">(");

                if (result.Types.Count > 1)
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
                case SqlQueryResultMode.Scalar: return "ExecuteScalar";
                case SqlQueryResultMode.First: return "QueryFirst";
                case SqlQueryResultMode.FirstOrDefault: return "QueryFirstOrDefault";
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

        private static string MakeCollectionInterfaceType(string typeName)
        {
            return String.Concat("ICollection<", typeName, '>');
        }

        private static string MakeCollectionType(string typeName)
        {
            return String.Concat("Collection<", typeName, '>');
        }

        private static string GetComplexTypeName(SqlStatementInfo statement)
        {
            return statement.ResultTypeName ?? String.Concat(statement.Name, ComplexResultTypeSuffix);
        }
        #endregion
    }
}