using System;
using System.Collections.Generic;
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
            IncrementalValuesProvider<AdditionalText> additionalTexts = context.AdditionalTextsProvider;
            var all = additionalTexts.Collect().Combine(context.AnalyzerConfigOptionsProvider);
            context.RegisterSourceOutput(all, (sourceProductionContext, source) => GenerateSource(sourceProductionContext, source.Left, source.Right));
        }

        private static void GenerateSource(SourceProductionContext sourceProductionContext, ImmutableArray<AdditionalText> files, AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider)
        {
            AnalyzerConfigOptions options = analyzerConfigOptionsProvider.GlobalOptions;
            string rootNamespace = GetRequiredMetadataProperty<string>(options, "build_property.rootnamespace");

            ImmutableArray<EmbeddedResourceItem> items = files.Where(x => ShouldGenerateAccessor(analyzerConfigOptionsProvider, x))
                                                              .Select(x => CreateItem(x, analyzerConfigOptionsProvider))
                                                              .ToImmutableArray();
            if (!items.Any())
                return;

            //CollectResourceUtility(sourceProductionContext, rootNamespace);
            CollectResourceAccessor(sourceProductionContext, items, rootNamespace);
        }

        private static EmbeddedResourceItem CreateItem(AdditionalText file, AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider)
        {
            AnalyzerConfigOptions options = analyzerConfigOptionsProvider.GetOptions(file);
            string name = Path.GetFileNameWithoutExtension(file.Path);
            string resourcePath = GetRequiredMetadataProperty<string>(options, "build_metadata.embeddedresource.logicalname");
            EmbeddedResourceItem item = new EmbeddedResourceItem(name, resourcePath);
            return item;
        }

        private static void CollectResourceAccessor(SourceProductionContext sourceProductionContext, ICollection<EmbeddedResourceItem> items, string rootNamespace)
        {
            string fieldsStr = String.Join(Environment.NewLine, items.Select(x => $"            public static readonly global::System.Lazy<string> {x.Name} = new global::System.Lazy<string>(() => ResourceUtility.GetEmbeddedResourceContent(\"{x.ResourcePath}\"));"));
            string propertiesStr = String.Join(Environment.NewLine, items.Select(x => $"        public static string {x.Name} => Cache.{x.Name}.Value;"));
            CollectSource(sourceProductionContext, "Resource", $@"{SourceGeneratorUtility.GeneratedCodeHeader}

namespace {rootNamespace}
{{
    internal static class Resource
    {{
{propertiesStr}

        private static class Cache
        {{
{fieldsStr}
        }}

        private static class ResourceUtility
        {{
            private static readonly global::System.Reflection.Assembly ThisAssembly = typeof(ResourceUtility).Assembly;

            public static string GetEmbeddedResourceContent(string resourceKey)
            {{
                using (global::System.IO.Stream stream = ThisAssembly.GetManifestResourceStream(resourceKey))
                {{
                    if (stream == null)
                        throw new global::System.InvalidOperationException($@""Resource not found: {{resourceKey}}
    {{ThisAssembly.Location}}"");

                    using (global::System.IO.TextReader reader = new global::System.IO.StreamReader(stream))
                    {{
                        string content = reader.ReadToEnd();
                        return content;
                    }}
                }}
            }}
        }}
    }}
}}");
        }

        private static void CollectSource(SourceProductionContext sourceProductionContext, string name, string content)
        {
            sourceProductionContext.AddSource($"{name}.g.cs", content);
        }

        private static bool ShouldGenerateAccessor(AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider, AdditionalText file)
        {
            AnalyzerConfigOptions options = analyzerConfigOptionsProvider.GetOptions(file);
            bool? generateTestAccessor = GetOptionalMetadataProperty<bool>(options, "build_metadata.embeddedresource.generateaccessor");
            return Equals(generateTestAccessor, true);
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

        private readonly struct EmbeddedResourceItem
        {
            public string Name { get; }
            public string ResourcePath { get; }

            public EmbeddedResourceItem(string name, string resourcePath)
            {
                Name = name;
                ResourcePath = resourcePath;
            }
        }
    }
}