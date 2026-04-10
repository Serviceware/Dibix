using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dibix.Generators
{
    internal static class EmbeddedSourceUtility
    {
        private static readonly Assembly ThisAssembly = typeof(EmbeddedSourceUtility).Assembly;
        private static readonly string EmbeddedSourcePrefix = $"{ThisAssembly.GetName().Name}.EmbeddedSources";

        public static void CollectEmbeddedSources(this IncrementalGeneratorPostInitializationContext context, string name)
        {
            bool collectedEmbeddedAttributeDefinition = false;

            string prefix = $"{EmbeddedSourcePrefix}.{name}";
            foreach (string resourceName in ThisAssembly.GetManifestResourceNames())
            {
                if (!resourceName.StartsWith(prefix, StringComparison.Ordinal))
                    continue;

                string fileName = resourceName.Substring(prefix.Length + 1);
                int extensionIndex = fileName.LastIndexOf('.');
                if (extensionIndex < 0)
                    extensionIndex = fileName.Length;

                fileName = fileName.Insert(extensionIndex, ".g");

                string content;
                using (Stream stream = ThisAssembly.GetManifestResourceStream(resourceName)!)
                {
                    using (TextReader reader = new StreamReader(stream))
                    {
                        content = reader.ReadToEnd();
                    }
                }

                if (!collectedEmbeddedAttributeDefinition)
                {
                    context.AddEmbeddedAttributeDefinition();
                    collectedEmbeddedAttributeDefinition = true;
                }

                context.AddSource(fileName, $"""
                                             {SourceGeneratorUtility.GeneratedCodeHeader}

                                             {NormalizeEmbeddedSource(content)}
                                             """);
            }
        }

        private static string NormalizeEmbeddedSource(string content)
        {
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(content);
            EmbeddedSourceNormalizationVisitor visitor = new EmbeddedSourceNormalizationVisitor();
            SyntaxNode normalizedNode = visitor.Visit(syntaxTree.GetRoot())!;
            string normalizedContent = normalizedNode.ToFullString();
            return normalizedContent;
        }

        private sealed class EmbeddedSourceNormalizationVisitor : CSharpSyntaxRewriter
        {
            public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node) => node.WithAttributeLists(CreateAttributeLists(node, Annotation.Class.Append(Annotation.Embedded)));

            public override SyntaxNode? VisitEnumDeclaration(EnumDeclarationSyntax node) => node.WithAttributeLists(CreateAttributeLists(node, EnumerableExtensions.Create(Annotation.GeneratedCode)));

            private static SyntaxList<AttributeListSyntax> CreateAttributeLists(MemberDeclarationSyntax node, IEnumerable<Annotation> annotations)
            {
                IEnumerable<AttributeListSyntax> attributeLists = annotations.Select(x => SyntaxFactory.AttributeList(new SeparatedSyntaxList<AttributeSyntax>().Add(CreateAttribute(x.Name, x.Arguments)))
                                                                                                       .WithLeadingTrivia(SyntaxFactory.Whitespace(new string(' ', 4)))
                                                                                                       .WithTrailingTrivia(SyntaxFactory.EndOfLine(Environment.NewLine)))
                                                                             .Concat(node.AttributeLists);
                return new SyntaxList<AttributeListSyntax>(attributeLists);
            }

            private static AttributeSyntax CreateAttribute(string name, string? arguments)
            {
                AttributeArgumentListSyntax? argumentList = null;
                if (arguments != null)
                    argumentList = SyntaxFactory.ParseAttributeArgumentList(arguments);

                AttributeSyntax attribute = SyntaxFactory.Attribute(SyntaxFactory.ParseName(name), argumentList);
                return attribute;
            }
        }
    }
}