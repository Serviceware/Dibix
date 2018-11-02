using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Dibix.Sdk
{
    public sealed class SqlDaoWriter : SqlWriter, IWriter
    {
        private static readonly bool WriteGuardChecks = false;
        private const string ConstantSuffix = "CommandText";
        private const string MethodPrefix = "";//"Execute";
        private const string ComplexResultTypeSuffix = "Result";
        private const string MethodinfoSuffix = "MethodInfo";

        protected override void Write(StringWriter writer, string projectName, IList<SqlStatementInfo> statements)
        {
            bool returnsEnumerable = false, hasCollectionProperties = false;
            CSharpWriter output = CSharpWriter.Init(writer, base.Namespace)
                                              .AddUsing("System.Reflection")
                                              .AddUsing("Dibix");

            CSharpClass @class = output.AddClass(base.ClassName, CSharpModifiers.Internal | CSharpModifiers.Static);

            for (int i = 0; i < statements.Count; i++)
            {
                SqlStatementInfo statement = statements[i];
                //@class.AddComment(String.Concat("file:///", statement.SourcePath.Replace(" ", "%20").Replace(@"\", "/")), false);
                @class.AddComment(statement.Name, false);
                @class.AddConstant(name: String.Concat(statement.Name, ConstantSuffix)
                                 , type: typeof(string).ToCSharpTypeName()
                                 , value: base.Format(statement.Content)
                                 , verbatim: base.Formatting.HasFlag(SqlQueryOutputFormatting.Verbatim));

                if (i + 1 < statements.Count)
                    @class.AddSeparator();
            }

            @class.AddSeparator();

            foreach (SqlStatementInfo statement in statements)
            {
                bool isSingleResult = statement.Results.Count == 1;

                if (isSingleResult && statement.Results[0].ResultMode == SqlQueryResultMode.Many)
                    returnsEnumerable = true;

                string resultTypeName = DetermineResultTypeName(statement);

                CSharpMethod method = @class.AddMethod(name: String.Concat(MethodPrefix, statement.Name)
                                                     , type: resultTypeName
                                                     , body: GenerateMethodBody(statement, ref hasCollectionProperties)
                                                     , isExtension: true
                                                     , modifiers: CSharpModifiers.Public | CSharpModifiers.Static);
                method.AddParameter("databaseAccessorFactory", "IDatabaseAccessorFactory");
                foreach (SqlQueryParameter parameter in statement.Parameters)
                    method.AddParameter(parameter.Name, parameter.ClrTypeName);
            }

            ICollection<SqlStatementInfo> multiResultStatements = statements.Where(x => x.Results.Count > 1 && x.ResultTypeName == null).ToArray();
            if (multiResultStatements.Any())
                @class.AddSeparator();

            foreach (SqlStatementInfo statement in multiResultStatements)
            {
                CSharpClass complexType = @class.AddClass(GetComplexTypeName(statement));

                IList<SqlQueryResult> collectionProperties = statement.Results.Where(x => x.ResultMode == SqlQueryResultMode.Many).ToArray();

                foreach (SqlQueryResult result in statement.Results)
                {
                    bool isEnumerable = result.ResultMode == SqlQueryResultMode.Many;
                    string resultTypeName = result.Types.First().Name;
                    if (isEnumerable)
                        resultTypeName = MakeCollectionType(resultTypeName);

                    complexType.AddProperty(result.Name, resultTypeName)
                               .Getter(null)
                               .Setter(null, isEnumerable ? CSharpModifiers.Private : default);
                }

                if (!collectionProperties.Any())
                    continue;

                StringBuilder ctorBodyWriter = new StringBuilder();
                for (int i = 0; i < collectionProperties.Count; i++)
                {
                    SqlQueryResult property = collectionProperties[i];
                    ctorBodyWriter.Append("this.")
                                  .Append(property.Name)
                                  .Append(" = new Collection<")
                                  .Append(property.Types.First().Name)
                                  .Append(">();");

                    if (i + 1 < collectionProperties.Count)
                        ctorBodyWriter.AppendLine();
                }

                complexType.AddSeparator()
                           .AddConstructor(ctorBodyWriter.ToString());
            }

            @class.AddSeparator();

            foreach (SqlStatementInfo statement in statements)
            {
                @class.AddProperty(String.Concat(statement.Name, MethodinfoSuffix), "MethodInfo", CSharpModifiers.Public | CSharpModifiers.Static)
                      .Getter(String.Format(CultureInfo.InvariantCulture, "return typeof({0}).GetMethod(\"{1}\");", base.ClassName, statement.Name));
            }

            if (returnsEnumerable || hasCollectionProperties)
                output.AddUsing("System.Collections.Generic");

            if (hasCollectionProperties)
            {
                output.AddUsing("System.Collections.ObjectModel");
            }

            output.Generate();
        }

        private static string DetermineResultTypeName(SqlStatementInfo query)
        {
            if (query.Results.Count == 0) // Execute/ExecuteScalar.
            {
                return typeof(int).ToCSharpTypeName();
            }

            if (query.Results.Count == 1) // Query<T>/QuerySingle/etc.
            {
                string resultTypeName = query.Results[0].Types.First().Name;
                if (query.Results[0].ResultMode == SqlQueryResultMode.Many)
                    resultTypeName = MakeEnumerableType(resultTypeName);

                return resultTypeName;
            }

            // GridReader
            return GetComplexTypeName(query);
        }

        private static string GenerateMethodBody(SqlStatementInfo statement, ref bool hasCollectionProperties)
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

            WriteExecutor(writer, statement, ref hasCollectionProperties);

            writer.PopIndent()
                  .Write("}");

            return writer.ToString();
        }

        private static void WriteGuardCheck(StringWriter writer, ContractCheck mode, string parameterName)
        {
            //writer.Write("Common.Check.Argument");
            switch (mode)
            {
                case ContractCheck.None:
                    break;
                case ContractCheck.NotNull:
                case ContractCheck.NotNullOrEmpty:
                    writer.WriteRaw(mode);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("mode", mode, null);
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

        private static void WriteExecutor(StringWriter writer, SqlStatementInfo query, ref bool hasCollectionProperties)
	    {
	        if (query.Results.Count == 0) // Execute/ExecuteScalar.
	        {
	            WriteNoResult(writer, query);
	        }
	        else if (query.Results.Count == 1) // Query<T>/QuerySingle/etc.
            {
                WriteSingleResult(writer, query);
            }
	        else // GridReader
	        {
	            WriteComplexResult(writer, query, ref hasCollectionProperties);
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
                writer.WriteRaw(returnType.Name);
                if (i + 1 < singleResult.Types.Count)
                    writer.WriteRaw(", ");
            }
            writer.WriteRaw(">(")
                  .WriteRaw(query.Name)
                  .WriteRaw(ConstantSuffix);

            if (query.CommandType.HasValue)
                writer.WriteRaw(", System.Data.CommandType.")
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

        private static void WriteComplexResult(StringWriter writer, SqlStatementInfo query, ref bool hasCollectionProperties)
        {
            writer.Write("using (IMultipleResultReader reader = accessor.QueryMultiple(")
                  .WriteRaw(query.Name)
                  .WriteRaw(ConstantSuffix);

            if (query.CommandType.HasValue)
                writer.WriteRaw(", System.Data.CommandType.")
                      .WriteRaw(query.CommandType.Value);

            if (query.Parameters.Any())
                writer.WriteRaw(", @params");

            string clrTypeName = GetComplexTypeName(query);
            writer.WriteLineRaw("))")
                  .WriteLine("{")
                  .PushIndent()
                  .Write(clrTypeName)
                  .WriteRaw(" result = new ")
                  .WriteRaw(clrTypeName)
                  .WriteLineRaw("();");

            foreach (SqlQueryResult result in query.Results)
            {
                writer.Write("result.")
                      .WriteRaw(result.Name);

                bool isEnumerable = result.ResultMode == SqlQueryResultMode.Many;
                if (isEnumerable)
                {
                    writer.WriteRaw(".ReplaceWith(");
                    hasCollectionProperties = true;
                }
                else
                    writer.WriteRaw(" = ");

                writer.WriteRaw("reader.")
                      .WriteRaw(GetMultipleResultReaderMethodName(result.ResultMode))
                      .WriteRaw('<');

                for (int i = 0; i < result.Types.Count; i++)
                {
                    TypeInfo returnType = result.Types[i];
                    writer.WriteRaw(returnType.Name);
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

            writer.WriteLine("return result;")
                  .PopIndent()
                  .WriteLine("}");
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
                default: throw new ArgumentOutOfRangeException("mode", mode, null);
            }
        }

        private static string GetMultipleResultReaderMethodName(SqlQueryResultMode mode)
        {
            switch (mode)
            {
                case SqlQueryResultMode.Many: return "ReadMany";
                case SqlQueryResultMode.Single: return "ReadSingle";
                default: throw new ArgumentOutOfRangeException("mode", mode, null);
            }
        }

        private static string MakeEnumerableType(string typeName)
        {
            return String.Concat("IEnumerable<", typeName, '>');
        }

        private static string MakeCollectionType(string typeName)
        {
            return String.Concat("ICollection<", typeName, '>');
        }

        private static string GetComplexTypeName(SqlStatementInfo statement)
        {
            return statement.ResultTypeName ?? String.Concat(statement.Name, ComplexResultTypeSuffix);
        }
    }
}