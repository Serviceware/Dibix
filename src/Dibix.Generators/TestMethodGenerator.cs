using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dibix.Generators
{
    [Generator]
    public sealed class TestMethodGenerator : IIncrementalGenerator
    {
        private static readonly string AttributeTypeName = typeof(TestMethodGenerationAttribute).FullName;

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var classDeclarations = context.SyntaxProvider.CreateSyntaxProvider(IsSyntaxTargetForGeneration, GetSemanticTargetForGeneration);
            var compilationAndClasses = context.CompilationProvider.Combine(classDeclarations.Collect());
            context.RegisterSourceOutput(compilationAndClasses, (sourceProductionContext, source) => GenerateSource(sourceProductionContext, source.Right));
        }

        private static bool IsSyntaxTargetForGeneration(SyntaxNode node, CancellationToken cancellationToken)
        {
            return node is AttributeListSyntax { Target: { } } attributeList && attributeList.Target.Identifier.IsKind(SyntaxKind.AssemblyKeyword);
        }

        private static IEnumerable<CodeGenerationTask> GetSemanticTargetForGeneration(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            AttributeListSyntax attributeList = (AttributeListSyntax)context.Node;
            var query = from attribute in attributeList.Attributes
                        let task = CollectCodeGenerationTask(context, attribute)
                        where task != null
                        select task.Value;
            return query;
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

            string? @namespace = CollectNamespace(context, arguments);
            if (@namespace == null) 
                return null;

            DerivedTypeVisitor typeVisitor = new DerivedTypeVisitor(baseTypeSymbol);
            context.SemanticModel.Compilation.GlobalNamespace.Accept(typeVisitor);

            return new CodeGenerationTask(@namespace, baseTypeSymbol.Name, typeVisitor.TypeNames);
        }

        private static string? CollectNamespace(GeneratorSyntaxContext context, SeparatedSyntaxList<AttributeArgumentSyntax> arguments)
        {
            string @namespace;
            if (arguments.Count > 1)
            {
                if (arguments[1].Expression is not LiteralExpressionSyntax namespaceExpression)
                    return null;

                @namespace = namespaceExpression.Token.ValueText;
            }
            else
            {
                @namespace = context.SemanticModel.Compilation.AssemblyName!;
            }
            return @namespace;
        }

        private static void GenerateSource(SourceProductionContext context, ImmutableArray<IEnumerable<CodeGenerationTask>> taskGroups)
        {
            taskGroups.SelectMany(x => x).Each(x => GenerateSource(context, x));
        }
        private static void GenerateSource(SourceProductionContext context, CodeGenerationTask task)
        {
            string className = $"{task.BaseTypeName}Tests";

            string TestMethodTemplate(string name) => $@"        [TestMethod]
        public void {name}() => this.Execute();";

            string testMethods = String.Join($"{Environment.NewLine}{Environment.NewLine}", task.TypeNames.Select(TestMethodTemplate));
            context.AddSource($"{className}.generated.cs", @$"using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace {task.Namespace}
{{
    [TestClass]
    public sealed partial class {className}
    {{
{testMethods}

        partial void Execute([CallerMemberName] string testName = null);
    }}
}}");
        }

        private readonly struct CodeGenerationTask
        {
            public string Namespace { get; }
            public string BaseTypeName { get; }
            public ImmutableArray<string> TypeNames { get; }

            public CodeGenerationTask(string @namespace, string baseTypeName, IEnumerable<string> typeNames)
            {
                this.Namespace = @namespace;
                this.BaseTypeName = baseTypeName;
                this.TypeNames = typeNames.ToImmutableArray();
            }
        }
    }
}