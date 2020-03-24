using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dibix.Sdk.CodeAnalysis;
using Dibix.Sdk.CodeGeneration;

namespace Dibix.Sdk.Cli
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
#if DEBUG
            System.Diagnostics.Debugger.Launch();
#endif
            IDictionary<string, string> arguments = ParseArguments(args[1]).ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
            string[] additionalAssemblyReferences;
            ILogger logger = new ConsoleLogger();
            switch (args[0])
            {
                case "SqlCodeAnalysisTask":
                    SqlCodeAnalysisTask.Execute
                    (
                        namingConventionPrefix: GetArgument<string>(arguments, "NamingConventionPrefix")
                      , databaseSchemaProviderName: GetArgument<string>(arguments, "DatabaseSchemaProviderName")
                      , modelCollation: GetArgument<string>(arguments, "ModelCollation")
                      , source: GetArgument<string>(arguments, "Source").AsEnumerable().ToArray()
                      , scriptSource: GetArgument<string>(arguments, "ScriptSource").AsEnumerable()
                      , sqlReferencePath: GetArgument<string>(arguments, "SqlReferencePath").AsEnumerable()
                      , logger: logger
                    );
                    break;

                case "CodeGenerationTask":
                    CodeGenerationTask.Execute
                    (
                        projectDirectory: GetArgument<string>(arguments, "ProjectDirectory")
                      , productName: GetArgument<string>(arguments, "ProductName")
                      , areaName: GetArgument<string>(arguments, "AreaName")
                      , defaultOutputFilePath: GetArgument<string>(arguments, "DefaultOutputFilePath")
                      , clientOutputFilePath: GetArgument<string>(arguments, "ClientOutputFilePath")
                      , source: GetArgument<string>(arguments, "Source").AsEnumerable().ToArray()
                      , contracts: GetArgument<string>(arguments, "Contracts").AsEnumerable()
                      , endpoints: GetArgument<string>(arguments, "Endpoints").AsEnumerable()
                      , references: GetArgument<string>(arguments, "References").AsEnumerable()
                      , embedStatements: GetArgument<bool>(arguments, "EmbedStatements")
                      , databaseSchemaProviderName: GetArgument<string>(arguments, "DatabaseSchemaProviderName")
                      , modelCollation: GetArgument<string>(arguments, "ModelCollation")
                      , sqlReferencePath: GetArgument<string>(arguments, "SqlReferencePath").AsEnumerable()
                      , logger: logger
                      , additionalAssemblyReferences: out additionalAssemblyReferences
                    );
                    break;

                default:
                    throw new InvalidOperationException($"Invalid task: {args[0]}");
            }
        }

        private static T GetArgument<T>(IDictionary<string, string> arguments, string item)
        {
            return (T)Convert.ChangeType(arguments[item], typeof(T));
        }

        private static IEnumerable<KeyValuePair<string, string>> ParseArguments(string inputFile)
        {
            return from arg in File.ReadAllLines(inputFile)
                   let parts = arg.Split(new [] { ' ' }, 2)
                   select new KeyValuePair<string, string>(parts[0], parts.Length > 1 ? parts[1] : null);
        }

        private static IEnumerable<string> AsEnumerable(this string str)
        {
            return str?.Split('|') ?? Enumerable.Empty<string>();
        }
    }
}