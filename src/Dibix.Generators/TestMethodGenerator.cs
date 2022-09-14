﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Dibix.Generators
{
    [Generator]
    public sealed class TestMethodGenerator : IIncrementalGenerator
    {
        private static readonly Assembly ThisAssembly = typeof(TestMethodGenerator).Assembly;
        private static readonly string AttributeTypeName = typeof(TestMethodGenerationAttribute).FullName;
        private const string EmbeddedSourcePrefix = $"{nameof(Dibix)}.{nameof(Generators)}.EmbeddedSources";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValueProvider<string?> rootNamespace = context.AnalyzerConfigOptionsProvider.Select((x, _) => GetRootNamespace(x));
            IncrementalValuesProvider<IEnumerable<CodeGenerationTask>> classDeclarations = context.SyntaxProvider.CreateSyntaxProvider(IsAssemblyAttribute, CollectCodeGenerationTasks);
            var all = context.CompilationProvider
                             .Combine(classDeclarations.Collect())
                             .Combine(rootNamespace);
            context.RegisterSourceOutput(all, (sourceProductionContext, source) => GenerateSource(sourceProductionContext, source.Left.Right, source.Right));
            context.RegisterPostInitializationOutput(RegisterPostInitializationOutput);
        }

        private static IEnumerable<CodeGenerationTask> CollectCodeGenerationTasks(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            AttributeListSyntax attributeList = (AttributeListSyntax)context.Node;
            var query = from attribute in attributeList.Attributes
                        let task = CollectCodeGenerationTask(context, attribute)
                        where task != null
                        select task.Value;
            return query;
        }

        private static void RegisterPostInitializationOutput(IncrementalGeneratorPostInitializationContext context)
        {
            foreach (string resourceName in ThisAssembly.GetManifestResourceNames())
            {
                if (!resourceName.StartsWith(EmbeddedSourcePrefix, StringComparison.Ordinal))
                    continue;

                string fileName = resourceName.Substring(EmbeddedSourcePrefix.Length + 1);
                int extensionIndex = fileName.LastIndexOf('.');
                if (extensionIndex < 0)
                    extensionIndex = fileName.Length;

                fileName = fileName.Insert(extensionIndex, ".generated");

                string content;
                using (Stream stream = ThisAssembly.GetManifestResourceStream(resourceName)!)
                {
                    using (TextReader reader = new StreamReader(stream))
                    {
                        content = reader.ReadToEnd();
                    }
                }

                context.AddSource(fileName, content);
            }
        }

        private static CodeGenerationTask? CollectCodeGenerationTask(GeneratorSyntaxContext context, AttributeSyntax attribute)
        {
            if (context.SemanticModel.GetSymbolInfo(attribute).Symbol is not IMethodSymbol attributeSymbol)
                return null;

            string displayString = attributeSymbol.ContainingType.ToDisplayString();
            if (displayString != AttributeTypeName)
                return null;

            if (attribute.ArgumentList?.Arguments == null)
                return null;

            SeparatedSyntaxList<AttributeArgumentSyntax> arguments = attribute.ArgumentList.Arguments;
            TypeSyntax? typeSyntax = (arguments[0].Expression as TypeOfExpressionSyntax)?.Type;
            if (typeSyntax == null)
                return null;

            if (context.SemanticModel.GetSymbolInfo(typeSyntax).Symbol is not ITypeSymbol baseTypeSymbol)
                return null;

            string? configuredNamespace = CollectNamespaceFromAttribute(context, arguments);
            string? assemblyName = context.SemanticModel.Compilation.AssemblyName;
            string baseTypeNamespace = CollectNamespaceFromType(baseTypeSymbol);
            DerivedTypeVisitor typeVisitor = new DerivedTypeVisitor(baseTypeSymbol);
            context.SemanticModel.Compilation.GlobalNamespace.Accept(typeVisitor);

            return new CodeGenerationTask(configuredNamespace, assemblyName, baseTypeSymbol.Name, baseTypeNamespace, typeVisitor.TypeNames);
        }

        private static string CollectNamespaceFromType(ITypeSymbol type) => type.ContainingNamespace.ToDisplayString();

        private static string? CollectNamespaceFromAttribute(GeneratorSyntaxContext context, SeparatedSyntaxList<AttributeArgumentSyntax> arguments)
        {
            if (arguments.Count <= 1) 
                return null;

            Optional<object?> value = context.SemanticModel.GetConstantValue(arguments[1].Expression);
            if (!value.HasValue)
                return null;

            string? result = value.Value as string;
            return result;
        }

        private static bool IsAssemblyAttribute(SyntaxNode node, CancellationToken cancellationToken)
        {
            return node is AttributeListSyntax { Target: { } } attributeList && attributeList.Target.Identifier.IsKind(SyntaxKind.AssemblyKeyword);
        }

        private static string? GetRootNamespace(AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider)
        {
            return analyzerConfigOptionsProvider.GlobalOptions.TryGetValue("build_property.rootnamespace", out string? rootNamespace) ? rootNamespace : null;
        }

        private static void GenerateSource(SourceProductionContext context, ImmutableArray<IEnumerable<CodeGenerationTask>> taskGroups, string? rootNamespace)
        {
            taskGroups.SelectMany(x => x).Each(x => GenerateSource(context, x, rootNamespace));
        }
        private static void GenerateSource(SourceProductionContext context, CodeGenerationTask task, string? rootNamespace)
        {
            string className = $"{task.BaseTypeName}Tests";

            string TestMethodTemplate(string name) => $@"        [TestMethod]
        public void {name}() => this.Execute();";

            string testMethods = String.Join($"{Environment.NewLine}{Environment.NewLine}", task.TypeNames.Select(TestMethodTemplate));
            string @namespace = task.Namespace ?? rootNamespace ?? task.AssemblyName ?? $"{task.BaseTypeNamespace}.Tests";
            string source = @$"using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace {@namespace}
{{
    [TestClass]
    public sealed partial class {className}
    {{
{testMethods}

        partial void Execute([CallerMemberName] string testName = null);
    }}
}}";
            context.AddSource($"{className}.generated.cs", source);
        }

        private readonly struct CodeGenerationTask
        {
            public string? Namespace { get; }
            public string? AssemblyName { get; }
            public string BaseTypeName { get; }
            public string BaseTypeNamespace { get; }
            public ImmutableArray<string> TypeNames { get; }

            public CodeGenerationTask(string? @namespace, string? assemblyName, string baseTypeName, string baseTypeNamespace, IEnumerable<string> typeNames)
            {
                this.Namespace = @namespace;
                this.AssemblyName = assemblyName;
                this.BaseTypeName = baseTypeName;
                this.BaseTypeNamespace = baseTypeNamespace;
                this.TypeNames = typeNames.ToImmutableArray();
            }
        }
    }
}