using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Dibix.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dibix.Testing.Generators
{
    [Generator]
    public sealed class TestConfigurationGenerator : IIncrementalGenerator
    {
        private static readonly string LazyValidationAttributeName = typeof(LazyValidationAttribute).FullName!;

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValueProvider<string?> rootNamespace = context.AnalyzerConfigOptionsProvider.Select((x, _) => x.GetRootNamespace());
            IncrementalValueProvider<ImmutableArray<ClassDescriptor>> classes = context.SyntaxProvider
                                                                                       .CreateSyntaxProvider(predicate: MatchSyntaxNode, transform: Collect)
                                                                                       .Where(static x => x is not null)
                                                                                       .Collect()!;

            IncrementalValueProvider<(ImmutableArray<ClassDescriptor> Left, string? Right)> input = classes.Combine(rootNamespace);

            context.RegisterSourceOutput(input, (x, y) => GenerateCode(x, y.Left, y.Right));
            context.RegisterPostInitializationOutput(x => x.CollectEmbeddedSources(nameof(TestConfigurationGenerator)));
        }

        private static bool MatchSyntaxNode(SyntaxNode node, CancellationToken cancellationToken)
        {
            if (node is not ClassDeclarationSyntax @class)
                return false;

            foreach (MemberDeclarationSyntax member in @class.Members)
            {
                if (!member.IsKind(SyntaxKind.FieldDeclaration))
                    continue;

                if (!member.IsDefined("LazyValidation"))
                    continue;

                return true;
            }

            return false;
        }

        private static ClassDescriptor? Collect(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            ClassDeclarationSyntax @class = (ClassDeclarationSyntax)context.Node;
            INamedTypeSymbol? classSymbol = context.SemanticModel.GetDeclaredSymbol(@class);

            if (classSymbol == null)
                return null;

            ImmutableArray<MemberDescriptor>.Builder membersBuilder = ImmutableArray.CreateBuilder<MemberDescriptor>();

            foreach (MemberDeclarationSyntax member in @class.Members)
            {
                if (member is not FieldDeclarationSyntax field)
                    continue;

                foreach (VariableDeclaratorSyntax variable in field.Declaration.Variables)
                {
                    if (context.SemanticModel.GetDeclaredSymbol(variable) is not IFieldSymbol fieldSymbol)
                        continue;

                    AttributeData? lazyValidationAttribute = fieldSymbol.GetAttributes().FirstOrDefault(x => x.AttributeClass?.ToDisplayString() == LazyValidationAttributeName);

                    if (lazyValidationAttribute is null)
                        continue;

                    bool isPrimitive = IsPrimitiveType(fieldSymbol.Type);
                    SymbolDisplayFormat format;
                    if (isPrimitive)
                    {
                        format = SymbolDisplayFormat.CSharpErrorMessageFormat;
                    }
                    else
                    {
                        format = new SymbolDisplayFormat
                        (
                            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included
                          , typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces
                          , genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters
                          , miscellaneousOptions: SymbolDisplayMiscellaneousOptions.None
                        );
                    }

                    membersBuilder.Add(new MemberDescriptor
                    (
                        PropertyName: GeneratePropertyName(fieldSymbol.Name),
                        FieldName: fieldSymbol.Name,
                        TypeName: fieldSymbol.Type.ToDisplayString(format),
                        IsPrimitive: isPrimitive
                    ));
                }
            }

            ImmutableArray<MemberDescriptor> endpoints = membersBuilder.ToImmutable();
            string @namespace = classSymbol.ContainingNamespace?.ToDisplayString() ?? "Global";
            string className = classSymbol.Name;
            bool isSealed = @class.Modifiers.Any(x => x.IsKind(SyntaxKind.SealedKeyword));
            bool isAbstract = @class.Modifiers.Any(x => x.IsKind(SyntaxKind.AbstractKeyword));
            ClassDescriptor group = new ClassDescriptor(@namespace, className, endpoints, isSealed, isAbstract);

            return group;
        }

        private static bool IsPrimitiveType(ITypeSymbol type)
        {
            if (type.SpecialType != SpecialType.None)
                return true;

            if (type is not INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } namedType)
                return false;

            ITypeSymbol underlyingType = namedType.TypeArguments[0];
            bool result = underlyingType.SpecialType != SpecialType.None;
            return result;

        }

        private static string GeneratePropertyName(string fieldName)
        {
            int startIndex = 0;
            if (fieldName[0] == '_')
                startIndex = 1;

            char firstChar = Char.ToUpper(fieldName[startIndex]);
            string propertyName = $"{firstChar}{fieldName.Substring(startIndex + 1)}";
            return propertyName;
        }

        private static void GenerateCode(SourceProductionContext context, ImmutableArray<ClassDescriptor> classes, string? rootNamespace)
        {
            foreach (ClassDescriptor @class in classes)
            {
                context.AddSource($"{@class.ClassName}.g.cs", GenerateSharedTestClass(@class));
            }
        }

        private static string GenerateSharedTestClass(ClassDescriptor @class)
        {
            return $$"""
                     {{SourceGeneratorUtility.GeneratedCodeHeader}}

                     namespace {{@class.Namespace}}
                     {
                         public{{(@class.IsSealed ? " sealed" : null)}}{{(@class.IsAbstract ? " abstract" : null)}} partial class {{@class.ClassName}} : global::Dibix.Testing.ConfigurationInitializationTracker
                         {{{GenerateProperties(@class.Members)}}
                         }
                     }
                     """;
        }

        private static string GenerateProperties(IReadOnlyList<MemberDescriptor> members)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < members.Count; i++)
            {
                MemberDescriptor member = members[i];

                if (i > 0)
                    sb.AppendLine();

                sb.AppendLine()
                  .Append(member.IsPrimitive ? GeneratePrimitiveProperty(member) : GenerateComplexProperty(member));
            }

            string text = sb.ToString();
            return text;
        }

        private static string GeneratePrimitiveProperty(MemberDescriptor member)
        {
            string property = $$"""
                                        public {{member.TypeName}} {{member.PropertyName}}
                                        {
                                            get
                                            {
                                                PropertyInitializationTracker.Verify(nameof({{member.PropertyName}}));
                                                return {{member.FieldName}};
                                            }
                                            set
                                            {
                                                {{member.FieldName}} = value;
                                                PropertyInitializationTracker.Initialize(nameof({{member.PropertyName}}));
                                            }
                                        }
                                """;
            return property;
        }

        private static string GenerateComplexProperty(MemberDescriptor member)
        {
            string property = $$"""
                                        public {{member.TypeName}} {{member.PropertyName}} => {{member.FieldName}} ??= new {{member.TypeName}} { PropertyInitializationTracker = new global::Dibix.Testing.ConfigurationPropertyInitializationTracker(InitializationToken) };
                                """;
            return property;
        }

        private sealed record ClassDescriptor(string Namespace, string ClassName, IReadOnlyList<MemberDescriptor> Members, bool IsSealed, bool IsAbstract);

        private sealed record MemberDescriptor(string PropertyName, string FieldName, string TypeName, bool IsPrimitive);
    }
}