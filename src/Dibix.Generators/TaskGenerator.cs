using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using Dibix.Sdk.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dibix.Generators
{
    [Generator]
    public sealed class TaskGenerator : IIncrementalGenerator
    {
        private static readonly string DefaultAnnotationsStr = ComputeDefaultAnnotationsStr();
        private static readonly string GeneratedCodeAnnotationStr = ComputeGeneratedCodeAnnotationStr();

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValueProvider<string?> rootNamespace = context.AnalyzerConfigOptionsProvider.Select((x, _) => x.GetRootNamespace());
            IncrementalValuesProvider<Task> tasks = context.SyntaxProvider
                                                           .CreateSyntaxProvider(IsClassWithAttribute, CollectCodeGenerationTask)
                                                           .Where(x => x != null)
                                                           .Select((x, _) => x!.Value);
            var all = tasks.Combine(context.CompilationProvider)
                           .Combine(rootNamespace);
            context.RegisterSourceOutput(all, (sourceProductionContext, source) => GenerateSource(sourceProductionContext, source.Left.Left, source.Right));
            context.RegisterPostInitializationOutput(x => x.CollectEmbeddedSources(nameof(TaskGenerator)));
        }

        private static bool IsClassWithAttribute(SyntaxNode node, CancellationToken cancellationToken)
        {
            bool isClassWithAttribute = node is ClassDeclarationSyntax @class && @class.AttributeLists.SelectMany(x => x.Attributes).Any();
            return isClassWithAttribute;
        }

        private static Task? CollectCodeGenerationTask(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            ClassDeclarationSyntax @class = (ClassDeclarationSyntax)context.Node;
            Task? task = null;
            ICollection<TaskProperty> properties = new Collection<TaskProperty>();
            foreach (AttributeListSyntax attributeList in @class.AttributeLists)
            {
                foreach (AttributeSyntax attribute in attributeList.Attributes)
                {
                    if (context.SemanticModel.GetSymbolInfo(attribute).Symbol is not IMethodSymbol methodSymbol)
                        continue;

                    if (attribute.ArgumentList?.Arguments == null)
                        continue;

                    string displayString = methodSymbol.ContainingType.ToDisplayString();
                    SeparatedSyntaxList<AttributeArgumentSyntax> arguments = attribute.ArgumentList.Arguments;

                    if (displayString == TypeNames.TaskAttributeTypeName)
                    {
                        if (arguments.Count != 1)
                            continue;

                        string? @namespace = GetNamespace(@class);
                        string className = @class.Identifier.ValueText;
                        string taskName = GetAttributeValue<string>(context, arguments, index: 0);
                        task = new Task(@namespace, className, taskName);
                    }
                    else if (displayString == TypeNames.TaskPropertyAttributeTypeName)
                    {
                        string propertyName = GetAttributeValue<string>(context, arguments, index: 0);
                        TaskPropertyType type = GetAttributeValue<TaskPropertyType>(context, arguments, index: 1);
                        TaskPropertySource source = GetAttributeValue<TaskPropertySource>(context, arguments, name: nameof(TaskPropertyAttribute.Source)) ?? TaskPropertySource.Core;
                        string? categoryName = GetAttributeValue(context, arguments, name: nameof(TaskPropertyAttribute.Category));
                        string? defaultValue = GetAttributeValue(context, arguments, name: nameof(TaskPropertyAttribute.DefaultValue));
                        TaskProperty property = new TaskProperty(propertyName, type, source, categoryName, defaultValue);
                        properties.Add(property);
                    }
                }
            }

            task?.Properties.AddRange(properties);

            return task;
        }

        private static void GenerateSource(SourceProductionContext context, Task task, string? rootNamespace)
        {
            string? @namespace = task.Namespace ?? rootNamespace;
            IDictionary<string, ICollection<TaskProperty>> propertyCategoryMap = task.Properties
                                                                                     .GroupBy(x => x.CategoryName)
                                                                                     .ToDictionary(x => x.Key ?? "", x => (ICollection<TaskProperty>)x.ToArray());
            GenerateTask(context, task, @namespace, propertyCategoryMap);
            GenerateTaskConfiguration(context, task, @namespace, propertyCategoryMap);
            GenerateConfigurations(context, @namespace, propertyCategoryMap);
        }

        private static void GenerateTask(SourceProductionContext context, Task task, string? @namespace, IDictionary<string, ICollection<TaskProperty>> propertyCategoryMap)
        {
            string configurationName = $"global::{@namespace}.{task.ClassName}Configuration";
            string propertyInitializersText = CollectTaskPropertyInitializersText(CollectTaskPropertyInitializers(propertyCategoryMap));
            string content = @$"{GenerationUtility.GeneratedCodeHeader}

namespace {@namespace}
{{
    public sealed partial class {task.ClassName} : global::Dibix.Sdk.Abstractions.ITask
    {{
        {GeneratedCodeAnnotationStr}
        private readonly global::Dibix.Sdk.Abstractions.ILogger _logger;

        {GeneratedCodeAnnotationStr}
        private readonly {configurationName} _configuration;

        {ComputeAnnotationStr(Annotation.GeneratedCode)}
        {ComputeAnnotationStr(Annotation.ExcludeFromCodeCoverage)}
        public {task.ClassName}(global::Dibix.Sdk.Abstractions.ILogger logger, global::Dibix.Sdk.Abstractions.InputConfiguration configuration)
        {{
            this._logger = logger;
            this._configuration = new {configurationName}
            {{{propertyInitializersText}
            }};
        }}

        bool global::Dibix.Sdk.Abstractions.ITask.Execute() => this.Execute();

        private partial bool Execute();
    }}
}}";
            context.AddSource($"{task.ClassName}.g.cs", content);
        }

        private static void GenerateTaskConfiguration(SourceProductionContext context, Task task, string? @namespace, IDictionary<string, ICollection<TaskProperty>> propertyCategoryMap)
        {
            string className = $"{task.ClassName}Configuration";

            string propertiesText = String.Join(Environment.NewLine, CollectTaskConfigurationProperties(@namespace, propertyCategoryMap));
            if (propertiesText.Length > 0)
            {
                propertiesText = $@"
{propertiesText}";
            }
            string content = @$"{GenerationUtility.GeneratedCodeHeader}

namespace {@namespace}
{{
{DefaultAnnotationsStr}
    public sealed class {className}
    {{{propertiesText}
    }}
}}";
            context.AddSource($"{className}.g.cs", content);
        }

        private static IEnumerable<string> CollectTaskConfigurationProperties(string? @namespace, IDictionary<string, ICollection<TaskProperty>> propertyCategoryMap)
        {
            if (propertyCategoryMap.TryGetValue("", out ICollection<TaskProperty> properties))
            {
                foreach (TaskProperty property in properties)
                {
                    yield return GenerateProperty(property);
                }
            }

            foreach (string categoryName in propertyCategoryMap.Select(x => x.Key).Where(x => x != ""))
            {
                yield return GenerateConfigurationProperty(categoryName, @namespace);
            }
        }

        private static void GenerateConfigurations(SourceProductionContext context, string? @namespace, IDictionary<string, ICollection<TaskProperty>> propertyCategoryMap)
        {
            foreach (KeyValuePair<string, ICollection<TaskProperty>> categoryGroup in propertyCategoryMap.Where(x => x.Key != ""))
            {
                GenerateConfiguration(context, @namespace, categoryGroup.Key, categoryGroup.Value);
            }
        }

        private static void GenerateConfiguration(SourceProductionContext context, string? @namespace, string categoryName, IEnumerable<TaskProperty> properties)
        {
            string className = $"{categoryName}Configuration";
            string propertiesText = String.Join(Environment.NewLine, properties.Select(GenerateProperty));
            if (propertiesText.Length > 0)
            {
                propertiesText = $@"
{propertiesText}";
            }
            string content = @$"{GenerationUtility.GeneratedCodeHeader}

namespace {@namespace}
{{
{DefaultAnnotationsStr}
    public sealed class {className}
    {{{propertiesText}
    }}
}}";
            context.AddSource($"{className}.g.cs", content);
        }

        private static string GenerateConfigurationProperty(string categoryName, string? @namespace)
        {
            string className = $"global::{@namespace}.{categoryName}Configuration";
            return $"        public {className} {categoryName} {{ get; }} = new {className}();";
        }

        private static string GenerateProperty(TaskProperty property)
        {
            StringBuilder sb = new StringBuilder($"        public {GetPropertyTypeName(property.Type)} {property.PropertyName} {{ get; set; }}");
            if (property.DefaultValue != null)
            {
                sb.Append($" = \"{property.DefaultValue}\";");
            }
            string propertyText = sb.ToString();
            return propertyText;
        }

        private static IEnumerable<string> CollectTaskPropertyInitializers(IDictionary<string, ICollection<TaskProperty>> propertyCategoryMap)
        {
            foreach (KeyValuePair<string, ICollection<TaskProperty>> categoryGroup in propertyCategoryMap)
            {
                if (categoryGroup.Key == "")
                {
                    foreach (string initializer in CollectTaskPropertyInitializers(categoryGroup.Value))
                    {
                        yield return initializer;
                    }
                }
                else
                {
                    string propertyInitializers = CollectTaskPropertyInitializersText(CollectTaskPropertyInitializers(categoryGroup.Value, indentation: "    "));
                    yield return $@"                {categoryGroup.Key} =
                {{{propertyInitializers}
                }}";
                }
            }
        }

        private static IEnumerable<string> CollectTaskPropertyInitializers(IEnumerable<TaskProperty> properties, string? indentation = null) => properties.Where(x => x.Source == TaskPropertySource.Core).Select(x => CollectTaskPropertyInitializer(x, indentation));

        private static string CollectTaskPropertyInitializersText(IEnumerable<string> propertyInitializers)
        {
            string propertyInitializersText = String.Join($",{Environment.NewLine}", propertyInitializers);
            if (propertyInitializersText.Length > 0)
            {
                propertyInitializersText = $@"
{propertyInitializersText}";
            }

            return propertyInitializersText;
        }

        private static string CollectTaskPropertyInitializer(TaskProperty property, string? indentation = null)
        {
            return $"{indentation}                {property.PropertyName} = configuration.{GetPropertyAccessor(property.Type)}(\"{property.PropertyName}\")";
        }

        private static string GetPropertyAccessor(TaskPropertyType propertyType)
        {
            switch (propertyType)
            {
                case TaskPropertyType.String: return "GetSingleValue<string>";
                case TaskPropertyType.Boolean: return "GetSingleValue<bool>";
                case TaskPropertyType.Int32: return "GetSingleValue<int>";
                case TaskPropertyType.Items: return "GetItems";
                default: throw new ArgumentOutOfRangeException(nameof(propertyType), propertyType, null);
            }
        }

        private static string GetPropertyTypeName(TaskPropertyType propertyType)
        {
            switch (propertyType)
            {
                case TaskPropertyType.String: return "string";
                case TaskPropertyType.Boolean: return "bool";
                case TaskPropertyType.Int32: return "int";
                case TaskPropertyType.Items: return "global::System.Collections.Generic.ICollection<global::Dibix.Sdk.Abstractions.TaskItem>";
                default: throw new ArgumentOutOfRangeException(nameof(propertyType), propertyType, null);
            }
        }

        private static T GetAttributeValue<T>(GeneratorSyntaxContext context, SeparatedSyntaxList<AttributeArgumentSyntax> arguments, int index)
        {
            SyntaxNode node = arguments[index].Expression;
            return (T)GetAttributeValue(context, node)!;
        }

        private static T? GetAttributeValue<T>(GeneratorSyntaxContext context, SeparatedSyntaxList<AttributeArgumentSyntax> arguments, string name) where T : struct
        {
            object? value = GetAttributeValueCore(context, arguments, name);
            // ReSharper disable once MergeConditionalExpression
            return value != null ? (T)value : null;
        }
        private static string? GetAttributeValue(GeneratorSyntaxContext context, SeparatedSyntaxList<AttributeArgumentSyntax> arguments, string name) => (string?)GetAttributeValueCore(context, arguments, name);
        private static object? GetAttributeValueCore(GeneratorSyntaxContext context, SeparatedSyntaxList<AttributeArgumentSyntax> arguments, string name)
        {
            SyntaxNode? node = arguments.SingleOrDefault(x => x.NameEquals?.Name?.Identifier.ValueText == name)?.Expression;
            return node != null ? GetAttributeValue(context, node) : null;
        }
        private static object? GetAttributeValue(GeneratorSyntaxContext context, SyntaxNode node)
        {
            Optional<object?> constantValue = context.SemanticModel.GetConstantValue(node);
            return constantValue.Value;
        }

        private static string ComputeDefaultAnnotationsStr() => String.Join(Environment.NewLine, Annotation.All.Select(x => $"    {ComputeAnnotationStr(x)}"));

        private static string ComputeGeneratedCodeAnnotationStr() => ComputeAnnotationStr(Annotation.GeneratedCode);

        private static string ComputeAnnotationStr(Annotation annotation) => $"[{annotation.Name}{annotation.Arguments}]";

        private static string? GetNamespace(SyntaxNode node) => TryGetParentSyntax(node, out NamespaceDeclarationSyntax? result) ? result!.Name.ToString() : null;

        private static bool TryGetParentSyntax<T>(SyntaxNode? syntaxNode, out T? result) where T : SyntaxNode
        {
            while (true)
            {
                syntaxNode = syntaxNode?.Parent;

                if (syntaxNode == null)
                {
                    result = null;
                    return false;
                }

                if (syntaxNode.GetType() != typeof(T)) 
                    continue;

                result = syntaxNode as T;
                return true;
            }
        }

        private readonly struct Task
        {
            public string? Namespace { get; }
            public string ClassName { get; }
            public string TaskName { get; }
            public ICollection<TaskProperty> Properties { get; }

            public Task(string? @namespace, string className, string taskName)
            {
                this.Namespace = @namespace;
                this.ClassName = className;
                this.TaskName = taskName;
                this.Properties = new Collection<TaskProperty>();
            }
        }

        private readonly struct TaskProperty
        {
            public string PropertyName { get; }
            public TaskPropertyType Type { get; }
            public TaskPropertySource Source { get; }
            public string? CategoryName { get; }
            public string? DefaultValue { get; }

            public TaskProperty(string propertyName, TaskPropertyType type, TaskPropertySource source, string? categoryName, string? defaultValue)
            {
                this.PropertyName = propertyName;
                this.Type = type;
                Source = source;
                this.CategoryName = categoryName;
                this.DefaultValue = defaultValue;
            }
        }
    }
}