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
            IncrementalValueProvider<ImmutableArray<MemberDescriptor>> classes = context.SyntaxProvider
                                                                                        .ForAttributeWithMetadataName(LazyValidationAttributeName, predicate: MatchSyntaxNode, transform: Collect)
                                                                                        .Where(static x => x is not null)
                                                                                        .Collect()!;

            IncrementalValueProvider<(ImmutableArray<MemberDescriptor> Left, string? Right)> input = classes.Combine(rootNamespace);

            context.RegisterSourceOutput(input, (x, y) => GenerateCode(x, y.Left, y.Right));
            context.RegisterPostInitializationOutput(x => x.CollectEmbeddedSources(nameof(TestConfigurationGenerator)));
        }

        private static bool MatchSyntaxNode(SyntaxNode node, CancellationToken cancellationToken)
        {
            if (node is not VariableDeclaratorSyntax variableDeclarator)
                return false;

            if (variableDeclarator.Parent is not VariableDeclarationSyntax variableDeclaration)
                return false;

            if (variableDeclaration.Parent is not FieldDeclarationSyntax fieldDeclaration)
                return false;

            if (fieldDeclaration.Parent is not ClassDeclarationSyntax)
                return false;

            return true;
        }

        private static MemberDescriptor Collect(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
        {
            SemanticModel semanticModel = context.SemanticModel;
            VariableDeclaratorSyntax variableDeclarator = (VariableDeclaratorSyntax)context.TargetNode;
            VariableDeclarationSyntax variableDeclaration = (VariableDeclarationSyntax)variableDeclarator.Parent!;
            FieldDeclarationSyntax fieldDeclaration = (FieldDeclarationSyntax)variableDeclaration.Parent!;
            ClassDeclarationSyntax classDeclaration = (ClassDeclarationSyntax)fieldDeclaration.Parent!;

            IFieldSymbol fieldSymbol = (IFieldSymbol)semanticModel.GetDeclaredSymbol(variableDeclarator)!;
            ITypeSymbol typeSymbol = fieldSymbol.Type;
            bool isPrimitive = IsPrimitiveType(typeSymbol);

            MemberDescriptor member = new MemberDescriptor
            (
                PropertyName: GeneratePropertyName(fieldSymbol.Name),
                FieldName: fieldSymbol.Name,
                TypeName: typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                IsPrimitive: isPrimitive,
                ClassFactory: new ClassDescriptorFactory(context, classDeclaration)
            );
            return member;
        }

        private static bool IsPrimitiveType(ITypeSymbol type)
        {
            if (type.SpecialType != SpecialType.None)
                return true;

            if (type.TypeKind == TypeKind.Enum)
                return true;

            if (type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.Uri")
                return true;

            if (type is not INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } namedType)
                return false;

            ITypeSymbol underlyingType = namedType.TypeArguments[0];
            bool result = IsPrimitiveType(underlyingType);
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

        private static void GenerateCode(SourceProductionContext context, ImmutableArray<MemberDescriptor> members, string? rootNamespace)
        {
            HashSet<string> generatedClassNames = new HashSet<string>(members.Select(x => $"global::{x.ClassFactory.ClassAccessor.Value.Namespace}.{x.ClassFactory.ClassAccessor.Value.ClassName}"));
            foreach (IGrouping<ClassDescriptorFactory, MemberDescriptor>? memberGroup in members.GroupBy(x => x.ClassFactory))
            {
                ClassDescriptor @class = memberGroup.Key.ClassAccessor.Value;
                context.AddSource($"{@class.ClassName}.g.cs", GenerateSharedTestClass(@class, memberGroup.ToArray(), generatedClassNames));
            }
        }

        private static string GenerateSharedTestClass(ClassDescriptor @class, IReadOnlyList<MemberDescriptor> members, HashSet<string> generatedClassNames)
        {
            // We could use @class.BaseClass here but it will cause redundancy analyzer issues in the original class like "Base type '' is already specified in other parts"
            // See: https://www.jetbrains.com/help/resharper/RedundantExtendsListEntry.html
            string? @base = @class.BaseClass == null ? " : global::Dibix.Testing.ConfigurationInitializationTracker" : null;
            return $$"""
                     {{SourceGeneratorUtility.GeneratedCodeHeader}}

                     namespace {{@class.Namespace}}
                     {
                         public{{(@class.IsSealed ? " sealed" : null)}}{{(@class.IsAbstract ? " abstract" : null)}} partial class {{@class.ClassName}}{{@base}}
                         {{{GenerateProperties(members, generatedClassNames)}}
                         }
                     }
                     """;
        }

        private static string GenerateProperties(IReadOnlyList<MemberDescriptor> members, HashSet<string> generatedClassNames)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < members.Count; i++)
            {
                MemberDescriptor member = members[i];

                if (i > 0)
                    sb.AppendLine();

                sb.AppendLine()
                  .Append(member.IsPrimitive ? GeneratePrimitiveProperty(member) : GenerateComplexProperty(member, generatedClassNames));
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

        private static string GenerateComplexProperty(MemberDescriptor member, HashSet<string> generatedClassNames)
        {
            string initializer = generatedClassNames.Contains(member.TypeName) ? " { PropertyInitializationTracker = new global::Dibix.Testing.ConfigurationPropertyInitializationTracker(InitializationToken) }" : "()";
            string property = $"        public {member.TypeName} {member.PropertyName} => {member.FieldName} ??= new {member.TypeName}{initializer};";
            return property;
        }

        private sealed record ClassDescriptor(string Namespace, string ClassName, bool IsSealed, bool IsAbstract, string? BaseClass);

        private sealed record MemberDescriptor(string PropertyName, string FieldName, string TypeName, bool IsPrimitive, ClassDescriptorFactory ClassFactory);

        private sealed class ClassDescriptorFactory
        {
            private readonly GeneratorAttributeSyntaxContext _context;

            public ClassDeclarationSyntax ClassDeclaration { get; }
            public Lazy<ClassDescriptor> ClassAccessor { get; }

            public ClassDescriptorFactory(GeneratorAttributeSyntaxContext context, ClassDeclarationSyntax classDeclaration)
            {
                _context = context;
                ClassDeclaration = classDeclaration;
                ClassAccessor = new Lazy<ClassDescriptor>(CreateClassDescriptor);
            }

            public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is ClassDescriptorFactory other && Equals(other);

            public override int GetHashCode() => ClassDeclaration.GetHashCode();

            private ClassDescriptor CreateClassDescriptor()
            {
                INamedTypeSymbol classSymbol = _context.SemanticModel.GetDeclaredSymbol(ClassDeclaration)!;
                string @namespace = classSymbol.ContainingNamespace?.ToDisplayString() ?? "Global";
                string className = classSymbol.Name;
                bool isSealed = ClassDeclaration.Modifiers.Any(x => x.IsKind(SyntaxKind.SealedKeyword));
                bool isAbstract = ClassDeclaration.Modifiers.Any(x => x.IsKind(SyntaxKind.AbstractKeyword));
                string? baseClass = classSymbol.BaseType is { SpecialType: not SpecialType.System_Object } ? classSymbol.BaseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) : null;
                return new ClassDescriptor(@namespace, className, isSealed, isAbstract, baseClass);
            }

            private bool Equals(ClassDescriptorFactory? other)
            {
                if (other is null)
                    return false;

                if (ReferenceEquals(this, other))
                    return true;

                return ClassDeclaration.Equals(other.ClassDeclaration);
            }
        }
    }
}