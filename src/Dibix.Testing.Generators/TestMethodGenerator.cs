using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Dibix.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dibix.Testing.Generators
{
    [Generator]
    public sealed class TestMethodGenerator : IIncrementalGenerator
    {
        private static readonly string AttributeTypeName = typeof(TestMethodGenerationAttribute).FullName!;

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValueProvider<string?> rootNamespace = context.AnalyzerConfigOptionsProvider.Select((x, _) => x.GetRootNamespace());
            IncrementalValuesProvider<EquatableArray<CodeGenerationTask>> tasks = context.SyntaxProvider.ForAttributeWithMetadataName(AttributeTypeName, predicate: MatchSyntaxNode, transform: Collect);

            IncrementalValuesProvider<(EquatableArray<CodeGenerationTask> Left, string? Right)> combined = tasks.Combine(rootNamespace);

            context.RegisterSourceOutput(combined, static (sourceProductionContext, source) => GenerateSource(sourceProductionContext, source.Left, source.Right));
            context.RegisterPostInitializationOutput(x => x.CollectEmbeddedSources(nameof(TestMethodGenerator)));
        }

        private static bool MatchSyntaxNode(SyntaxNode node, CancellationToken cancellationToken) => node is CompilationUnitSyntax;

        private static EquatableArray<CodeGenerationTask> Collect(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
        {
            return context.Attributes
                          .Select(x => CollectCodeGenerationTask(context, x, cancellationToken))
                          .Where(x => x != null)
                          .Select(x => x!)
                          .ToImmutableArray()
                          .AsEquatableArray();
        }

        private static CodeGenerationTask? CollectCodeGenerationTask(GeneratorAttributeSyntaxContext context, AttributeData attribute, CancellationToken cancellationToken)
        {
            ITypeSymbol typeSymbol = (ITypeSymbol)attribute.ConstructorArguments[0].Value!;
            string? configuredNamespace = CollectNamespaceFromAttribute(context, attribute);
            string? assemblyName = context.SemanticModel.Compilation.AssemblyName;
            string baseTypeNamespace = CollectNamespaceFromType(typeSymbol);
            DerivedTypeVisitor typeVisitor = new DerivedTypeVisitor(typeSymbol, cancellationToken);
            context.SemanticModel.Compilation.GlobalNamespace.Accept(typeVisitor);

            return new CodeGenerationTask(configuredNamespace, assemblyName, typeSymbol.Name, baseTypeNamespace, typeVisitor.TypeNames);
        }

        private static string CollectNamespaceFromType(ITypeSymbol type) => type.ContainingNamespace.ToDisplayString();

        private static string? CollectNamespaceFromAttribute(GeneratorAttributeSyntaxContext context, AttributeData attribute)
        {
            string? @namespace;

            if (attribute.ConstructorArguments.Length > 1)
            {
                @namespace = (string?)attribute.ConstructorArguments[1].Value;
                return @namespace;
            }

            @namespace = (string?)attribute.NamedArguments.FirstOrDefault(x => x.Key == nameof(TestMethodGenerationAttribute.Namespace)).Value.Value;
            return @namespace;
        }

        private static void GenerateSource(SourceProductionContext context, IEnumerable<CodeGenerationTask> tasks, string? rootNamespace)
        {
            foreach (CodeGenerationTask task in tasks)
            {
                GenerateSource(context, task, rootNamespace);
            }
        }
        private static void GenerateSource(SourceProductionContext context, CodeGenerationTask task, string? rootNamespace)
        {
            string className = $"{task.BaseTypeName}Tests";
            string TestMethodTemplate(string name) => $"""
                                                               [global::Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
                                                               public void {name}() => this.Execute();
                                                       """;

            string testMethods = String.Join($"{Environment.NewLine}{Environment.NewLine}", task.TypeNames.Select(TestMethodTemplate));
            string @namespace = task.Namespace ?? rootNamespace ?? task.AssemblyName ?? $"{task.BaseTypeNamespace}.Tests";
            string source = $$"""
                              {{SourceGeneratorUtility.GeneratedCodeHeader}}

                              namespace {{@namespace}}
                              {
                              {{Annotation.ClassText}}
                                  [global::Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
                                  public sealed partial class {{className}}
                                  {
                              {{testMethods}}

                                      partial void Execute();
                                  }
                              }
                              """;
            context.AddSource($"{className}.g.cs", source);
        }

        private sealed record CodeGenerationTask(string? Namespace, string? AssemblyName, string BaseTypeName, string BaseTypeNamespace, EquatableArray<string> TypeNames);
    }
}