using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Dibix.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Dibix.Testing.Generators
{
    [Generator]
    public sealed class EmbeddedResourceAccessorGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValueProvider<string> rootNamespace = context.AnalyzerConfigOptionsProvider.Select(static (x, _) =>
            {
                string rootNamespace = GetRequiredMetadataProperty<string>(x.GlobalOptions, "build_property.rootnamespace");
                return rootNamespace;
            });
            IncrementalValueProvider<ImmutableArray<EmbeddedResourceClassDescriptor>> classes = context.AdditionalTextsProvider
                                                                                                       .Combine(context.AnalyzerConfigOptionsProvider)
                                                                                                       .Select(static (x, _) =>
                                                                                                       {
                                                                                                           AnalyzerConfigOptions options = x.Right.GetOptions(x.Left);
                                                                                                           bool generate = GetOptionalMetadataProperty<bool>(options, "build_metadata.embeddedresource.generateaccessor");
                                                                                                           if (!generate)
                                                                                                               return ((string ClassName, EmbeddedResourceMemberDescriptor Member)?)null;

                                                                                                           string name = Path.GetFileNameWithoutExtension(x.Left.Path);
                                                                                                           string resourcePath = GetRequiredMetadataProperty<string>(options, "build_metadata.embeddedresource.logicalname");
                                                                                                           string className = GetOptionalMetadataProperty<string>(options, "build_metadata.embeddedresource.accessorname") ?? "Resource";
                                                                                                           return (ClassName: className, Member: new EmbeddedResourceMemberDescriptor(name, resourcePath));
                                                                                                       })
                                                                                                       .Where(static x => x != null)
                                                                                                       .Select(static (x, _) => x!.Value)
                                                                                                       .Collect()
                                                                                                       .Select(static (x, _) => x.GroupBy(z => z.ClassName, (a, b) => new EmbeddedResourceClassDescriptor(a, b.Select(z => z.Member)
                                                                                                                                                                                                              .ToImmutableArray()
                                                                                                                                                                                                              .AsEquatableArray()))
                                                                                                                                 .ToImmutableArray());

            IncrementalValueProvider<(ImmutableArray<EmbeddedResourceClassDescriptor> Left, string Right)> combined = classes.Combine(rootNamespace);

            context.RegisterSourceOutput(combined, static (x, y) => GenerateSource(x, y.Left, y.Right));
        }

        private static void GenerateSource(SourceProductionContext sourceProductionContext, ImmutableArray<EmbeddedResourceClassDescriptor> classes, string rootNamespace)
        {
            foreach (EmbeddedResourceClassDescriptor @class in classes)
            {
                CollectResourceAccessor(sourceProductionContext, @class.Members.AsImmutableArray(), rootNamespace, className: @class.ClassName);
            }
        }

        private static void CollectResourceAccessor(SourceProductionContext sourceProductionContext, ImmutableArray<EmbeddedResourceMemberDescriptor> members, string rootNamespace, string className)
        {
            string fieldsStr = String.Join(Environment.NewLine, members.Select(x => $"            public static readonly global::System.Lazy<string> {x.Name} = new global::System.Lazy<string>(() => ResourceUtility.GetEmbeddedResourceContent(\"{x.ResourcePath}\"));"));
            string propertiesStr = String.Join(Environment.NewLine, members.Select(x => $"        public static string {x.Name} => Cache.{x.Name}.Value;"));
            string template = $$"""
                               {{SourceGeneratorUtility.GeneratedCodeHeader}}

                               namespace {{rootNamespace}}
                               {
                               {{Annotation.ClassText}}
                                   internal static class {{className}}
                                   {
                               {{propertiesStr}}

                                       private static class Cache
                                       {
                               {{fieldsStr}}
                                       }

                                       private static class ResourceUtility
                                       {
                                           private static readonly global::System.Reflection.Assembly ThisAssembly = typeof(ResourceUtility).Assembly;

                                           public static string GetEmbeddedResourceContent(string resourceKey)
                                           {
                                               using (global::System.IO.Stream stream = ThisAssembly.GetManifestResourceStream(resourceKey))
                                               {
                                                   if (stream == null)
                                                       throw new global::System.InvalidOperationException($@"Resource not found: {resourceKey}
                                   {ThisAssembly.Location}");

                                                   using (global::System.IO.TextReader reader = new global::System.IO.StreamReader(stream))
                                                   {
                                                       string content = reader.ReadToEnd();
                                                       return content;
                                                   }
                                               }
                                           }
                                       }
                                   }
                               }
                               """;
            CollectSource(sourceProductionContext, className, template);
        }

        private static void CollectSource(SourceProductionContext sourceProductionContext, string name, string content)
        {
            sourceProductionContext.AddSource($"{name}.g.cs", content);
        }

        private static T? GetOptionalMetadataProperty<T>(AnalyzerConfigOptions options, string key)
        {
            if (!options.TryGetValue(key, out string? value) || value == "")
                return default;

            return (T)Convert.ChangeType(value, typeof(T));
        }

        private static T GetRequiredMetadataProperty<T>(AnalyzerConfigOptions options, string key)
        {
            if (!options.TryGetValue(key, out string? value) || value == "")
                throw new InvalidOperationException($"Missing analyzer property: '{key}'");

            return (T)Convert.ChangeType(value, typeof(T));
        }

        private readonly record struct EmbeddedResourceMemberDescriptor(string Name, string ResourcePath);

        private sealed record EmbeddedResourceClassDescriptor(string ClassName, EquatableArray<EmbeddedResourceMemberDescriptor> Members);

    }
}