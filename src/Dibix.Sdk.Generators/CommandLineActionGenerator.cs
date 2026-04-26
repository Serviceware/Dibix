using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Dibix.Generators;
using Dibix.Sdk;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dibix.Testing.Generators
{
    [Generator]
    public sealed class CommandLineActionGenerator : IIncrementalGenerator
    {
        private static readonly string ActionAttributeName = typeof(CommandLineActionAttribute).FullName!;
        private static readonly string ArgumentAttributeName = typeof(CommandLineActionArgumentAttribute).FullName!;
        private static readonly string InputPropertyAttributeName = typeof(CommandLineInputPropertyAttribute).FullName!;

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValueProvider<string?> rootNamespace = context.AnalyzerConfigOptionsProvider.Select((x, _) => x.GetRootNamespace());
            IncrementalValueProvider<ImmutableArray<CommandClassDescriptor>> classes = context.SyntaxProvider
                                                                                              .ForAttributeWithMetadataName(ActionAttributeName, predicate: MatchSyntaxNode, transform: Collect)
                                                                                              .Collect()!;

            IncrementalValueProvider<(ImmutableArray<CommandClassDescriptor> Left, string? Right)> input = classes.Combine(rootNamespace);

            context.RegisterSourceOutput(input, static (x, y) => GenerateCode(x, y.Left, y.Right));
            context.RegisterPostInitializationOutput(x => x.CollectEmbeddedSources(nameof(CommandLineActionGenerator)));
        }

        private static bool MatchSyntaxNode(SyntaxNode node, CancellationToken cancellationToken) => node is ClassDeclarationSyntax;

        private static CommandClassDescriptor Collect(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
        {
            INamedTypeSymbol classSymbol = (INamedTypeSymbol)context.TargetSymbol;
            ImmutableArray<AttributeData> attributes = classSymbol.GetAttributes();

            string @namespace = classSymbol.ContainingNamespace?.ToDisplayString() ?? "Global";
            string className = classSymbol.Name;

            AttributeData rootAttribute = context.Attributes[0];
            ImmutableArray<ArgumentDescriptor>.Builder argumentsBuilder = ImmutableArray.CreateBuilder<ArgumentDescriptor>();
            ImmutableArray<InputClassDescriptor>.Builder inputClassesBuilder = ImmutableArray.CreateBuilder<InputClassDescriptor>();
            List<CommandLineInputPropertyAttribute> inputProperties = new List<CommandLineInputPropertyAttribute>();
            HashSet<string> categories = new HashSet<string>();
            bool collectedInputPropertyFilePathArgument = false;

            foreach (AttributeData attribute in attributes)
            {
                string? attributeFullName = attribute.AttributeClass?.ToDisplayString();
                if (attributeFullName == ArgumentAttributeName)
                {
                    string argumentName = (string)attribute.ConstructorArguments[0].Value!;
                    string[] nameWords = argumentName.SplitWords().ToArray();
                    string argumentNameNormalized = nameWords.ToCamelCase();
                    string fieldName = $"_{argumentNameNormalized}Argument";
                    string argumentType = ((INamedTypeSymbol)attribute.ConstructorArguments[1].Value!).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    string argumentDescription = (string)attribute.ConstructorArguments[2].Value!;
                    ArgumentDescriptor argumentDescriptor = new ArgumentDescriptor
                    (
                        ArgumentName: argumentNameNormalized,
                        FieldName: fieldName,
                        FieldType: argumentType,
                        ParameterName: argumentNameNormalized,
                        ParameterType: argumentType,
                        Description: argumentDescription
                    );
                    argumentsBuilder.Add(argumentDescriptor);
                }
                else if (attributeFullName == InputPropertyAttributeName)
                {
                    string propertyName = (string)attribute.ConstructorArguments[0].Value!;
                    CommandLineInputPropertyType propertyType = (CommandLineInputPropertyType)attribute.ConstructorArguments[1].Value!;
                    string? category = (string?)attribute.NamedArguments.FirstOrDefault(x => x.Key == nameof(CommandLineInputPropertyAttribute.Category)).Value.Value;
                    inputProperties.Add(new CommandLineInputPropertyAttribute(propertyName, propertyType) { Category = category });

                    if (category != null)
                        categories.Add(category);

                    if (!collectedInputPropertyFilePathArgument)
                    {
                        ArgumentDescriptor argumentDescriptor = new ArgumentDescriptor
                        (
                            ArgumentName: "inputPropertyFilePath",
                            FieldName: "_inputPropertyFilePathArgument",
                            FieldType: "string",
                            ParameterName: "input",
                            ParameterType: $"global::{@namespace}.{className}Input",
                            Description: "Path to the input property file."
                        );
                        argumentsBuilder.Add(argumentDescriptor);
                        collectedInputPropertyFilePathArgument = true;
                    }
                }
            }

            if (inputProperties.Count > 0)
            {
                CollectInputClass(@namespace, className, category: null, CollectRootInputClassProperties(@namespace, inputProperties, categories), inputClassesBuilder);
            }

            foreach (IGrouping<string?, CommandLineInputPropertyAttribute> inputClassGroup in inputProperties.GroupBy(x => x.Category))
            {
                if (inputClassGroup.Key == null)
                    continue;

                CollectInputClass(@namespace, inputClassGroup.Key, inputClassGroup.Key, CollectInputClassProperties(inputClassGroup), inputClassesBuilder);
            }

            string commandName = (string)rootAttribute.ConstructorArguments[0].Value!;
            string description = (string)rootAttribute.ConstructorArguments[1].Value!;
            CommandClassDescriptor member = new CommandClassDescriptor
            (
                Namespace: @namespace,
                ClassName: className,
                CommandName: commandName,
                Description: description,
                Arguments: argumentsBuilder.ToImmutable(),
                InputClasses: inputClassesBuilder.ToImmutable()
            );
            return member;
        }

        private static void CollectInputClass(string @namespace, string className, string? category, IEnumerable<InputPropertyDescriptor> properties, ImmutableArray<InputClassDescriptor>.Builder target)
        {
            InputClassDescriptor inputClass = new InputClassDescriptor
            (
                Namespace: @namespace,
                ClassName: $"{className}Input",
                Category: category,
                Properties: properties.ToImmutableArray()
            );
            target.Add(inputClass);
        }

        private static IEnumerable<InputPropertyDescriptor> CollectRootInputClassProperties(string @namespace, IEnumerable<CommandLineInputPropertyAttribute> inputProperties, IEnumerable<string> categories)
        {
            foreach (CommandLineInputPropertyAttribute inputProperty in inputProperties)
            {
                if (inputProperty.Category != null)
                    continue;

                InputPropertyDescriptor propertyDescriptor = CollectInputClassProperty(inputProperty);
                yield return propertyDescriptor;
            }

            foreach (string category in categories)
            {
                InputPropertyDescriptor propertyDescriptor = new InputPropertyDescriptor
                (
                    PropertyName: category,
                    Type: $"global::{@namespace}.{category}Input",
                    IsItems: false,
                    IsNested: true
                );
                yield return propertyDescriptor;
            }
        }

        private static IEnumerable<InputPropertyDescriptor> CollectInputClassProperties(IEnumerable<CommandLineInputPropertyAttribute> inputProperties)
        {
            foreach (CommandLineInputPropertyAttribute property in inputProperties)
            {
                InputPropertyDescriptor propertyDescriptor = CollectInputClassProperty(property);
                yield return propertyDescriptor;
            }
        }

        private static InputPropertyDescriptor CollectInputClassProperty(CommandLineInputPropertyAttribute property)
        {
            InputPropertyDescriptor propertyDescriptor = new InputPropertyDescriptor
            (
                PropertyName: property.Name,
                Type: ToClrType(property.Type),
                IsItems: property.Type == CommandLineInputPropertyType.Items,
                IsNested: false
            );
            return propertyDescriptor;
        }

        private static void GenerateCode(SourceProductionContext context, ImmutableArray<CommandClassDescriptor> classes, string? rootNamespace)
        {
            for (int i = 0; i < classes.Length; i++)
            {
                CommandClassDescriptor @class = classes[i];

                if (i == 0)
                    context.AddSource("DibixRootCommand.g.cs", GenerateRootCommand(classes, @class.Namespace));

                context.AddSource($"{@class.ClassName}.g.cs", GenerateCommandClass(@class));

                foreach (InputClassDescriptor inputClass in @class.InputClasses)
                {
                    context.AddSource($"{inputClass.ClassName}.g.cs", GenerateInputClass(inputClass));
                }
            }
        }

        private static string GenerateRootCommand(ImmutableArray<CommandClassDescriptor> classes, string @namespace)
        {
            const string className = "DibixRootCommand";
            string output = $$"""
                              {{SourceGeneratorUtility.GeneratedCodeHeader}}

                              namespace {{@namespace}}
                              {
                              {{Annotation.ClassText}}
                                  public partial class {{className}} : global::System.CommandLine.RootCommand
                                  {
                                      public {{className}}(global::Dibix.Sdk.Abstractions.ILogger logger, string description = "") : base(description)
                                      {{{GenerateCommandRegistrations(classes)}}
                                      }
                                  }
                              }
                              """;
            return output;
        }

        private static string GenerateCommandRegistrations(ImmutableArray<CommandClassDescriptor> classes)
        {
            StringBuilder sb = new StringBuilder();

            foreach (CommandClassDescriptor @class in classes)
            {
                sb.AppendLine()
                  .Append(GenerateCommandRegistration(@class));
            }

            string output = sb.ToString();
            return output;
        }

        private static string GenerateCommandRegistration(CommandClassDescriptor @class)
        {
            string output = $"            Add(new global::{@class.Namespace}.{@class.ClassName}(logger));";
            return output;
        }

        private static string GenerateCommandClass(CommandClassDescriptor @class)
        {
            string output = $$"""
                              {{SourceGeneratorUtility.GeneratedCodeHeader}}

                              namespace {{@class.Namespace}}
                              {
                              {{Annotation.ClassText}}
                                  public partial class {{@class.ClassName}} : global::System.CommandLine.Command
                                  {
                                      private readonly global::Dibix.Sdk.Abstractions.ILogger _logger;{{GenerateArgumentFields(@class.Arguments)}}

                                      public {{@class.ClassName}}(global::Dibix.Sdk.Abstractions.ILogger logger) : base("{{@class.CommandName}}", "{{@class.Description}}")
                                      {
                                          _logger = logger;{{GenerateArgumentValues(@class.Arguments)}}{{GenerateArgumentRegistrations(@class.Arguments)}}

                                          SetAction(OnExecuteAction);
                                      }

                                      public partial global::System.Threading.Tasks.Task<int> Execute({{GenerateMethodParameterDeclarations(@class.Arguments)}});

                                      private async global::System.Threading.Tasks.Task<int> OnExecuteAction(global::System.CommandLine.ParseResult result, global::System.Threading.CancellationToken cancellationToken)
                                      {{{GenerateArgumentReads(@class.Arguments)}}{{GenerateInputConfiguration(@class.InputClasses)}}
                                          int exitCode = await Execute({{GenerateMethodParameterValues(@class.Arguments)}}).ConfigureAwait(false);
                                          return exitCode;
                                      }
                                  }
                              }
                              """;
            return output;
        }

        private static string GenerateInputClass(InputClassDescriptor @class)
        {
            string output = $$"""
                              {{SourceGeneratorUtility.GeneratedCodeHeader}}

                              namespace {{@class.Namespace}}
                              {
                              {{Annotation.ClassText}}
                                  public sealed class {{@class.ClassName}}
                                  {{{GenerateInputProperties(@class.Properties)}}
                                  }
                              }
                              """;
            return output;
        }

        private static string GenerateArgumentFields(ImmutableArray<ArgumentDescriptor> arguments)
        {
            StringBuilder sb = new StringBuilder();

            foreach (ArgumentDescriptor argument in arguments)
            {
                sb.AppendLine()
                  .Append(GenerateArgumentField(argument));
            }

            string output = sb.ToString();
            return output;
        }

        private static string GenerateArgumentValues(ImmutableArray<ArgumentDescriptor> arguments)
        {
            StringBuilder sb = new StringBuilder();

            foreach (ArgumentDescriptor argument in arguments)
            {
                sb.AppendLine()
                  .Append(GenerateArgumentValue(argument));
            }

            string output = sb.ToString();
            return output;
        }

        private static string GenerateArgumentRegistrations(ImmutableArray<ArgumentDescriptor> arguments)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < arguments.Length; i++)
            {
                if (i == 0)
                    sb.AppendLine();

                ArgumentDescriptor argument = arguments[i];
                sb.AppendLine()
                  .Append(GenerateArgumentRegistration(argument));
            }

            string output = sb.ToString();
            return output;
        }

        private static string GenerateMethodParameterDeclarations(ImmutableArray<ArgumentDescriptor> arguments)
        {
            string output = String.Join(", ", arguments.Select(x => $"{x.ParameterType} {x.ParameterName}"));
            return output;
        }

        private static string GenerateArgumentReads(ImmutableArray<ArgumentDescriptor> arguments)
        {
            StringBuilder sb = new StringBuilder();

            foreach (ArgumentDescriptor argument in arguments)
            {
                sb.AppendLine()
                  .Append(GenerateArgumentRead(argument));
            }

            string output = sb.ToString();
            return output;
        }

        private static string GenerateInputConfiguration(ImmutableArray<InputClassDescriptor> inputClasses)
        {
            if (inputClasses.IsEmpty)
                return "";

            IDictionary<string, InputClassDescriptor> inputClassMap = inputClasses.ToDictionary(x => x.Category ?? "");
            InputClassDescriptor root = inputClassMap[""];

            StringBuilder sb = new StringBuilder();

            sb.AppendLine()
              .Append($"""
                                  global::Dibix.Sdk.Abstractions.InputConfiguration inputConfiguration = global::Dibix.Sdk.Abstractions.InputConfiguration.Parse(inputPropertyFilePath);
                                  global::{root.Namespace}.{root.ClassName} input = new global::{root.Namespace}.{root.ClassName}
                      """);

            byte indentation = 3;

            GenerateInputConfigurationPropertyAssignments(sb, ref indentation, root, inputClassMap);

            sb.Append(';');

            string output = sb.ToString();
            return output;
        }

        private static void GenerateInputConfigurationPropertyAssignments(StringBuilder sb, ref byte indentation, InputClassDescriptor inputClass, IDictionary<string, InputClassDescriptor> inputClassMap)
        {
            sb.AppendLine();

            WriteIndent(sb, indentation);

            sb.Append('{');

            indentation++;

            ImmutableArray<InputPropertyDescriptor> properties = inputClass.Properties.AsImmutableArray();
            for (int i = 0; i < properties.Length; i++)
            {
                InputPropertyDescriptor property = properties[i];
                sb.AppendLine();

                WriteIndent(sb, indentation);

                sb.Append($"{property.PropertyName} =");

                if (property.IsNested)
                {
                    GenerateInputConfigurationPropertyAssignments(sb, ref indentation, inputClassMap[property.PropertyName], inputClassMap);
                }
                else
                {
                    sb.Append($""" inputConfiguration.{(property.IsItems ? "GetItems" : $"GetSingleValue<{property.Type}>")}("{property.PropertyName}")""");
                }

                if (i + 1 < properties.Length)
                {
                    sb.Append(',');
                }
            }

            indentation--;

            sb.AppendLine();

            WriteIndent(sb, indentation);

            sb.Append('}');
        }

        private static string GenerateInputProperties(ImmutableArray<InputPropertyDescriptor> properties)
        {
            StringBuilder sb = new StringBuilder();

            foreach (InputPropertyDescriptor property in properties)
            {
                string suffix = property.IsNested ? $$"""} = new {{property.Type}}();""" : "set; }";
                sb.AppendLine()
                  .Append($$"""        public {{property.Type}} {{property.PropertyName}} { get; {{suffix}}""");
            }

            string output = sb.ToString();
            return output;
        }

        private static string GenerateMethodParameterValues(ImmutableArray<ArgumentDescriptor> arguments)
        {
            string output = String.Join(", ", arguments.Select(x => x.ParameterName));
            return output;
        }

        private static string GenerateArgumentField(ArgumentDescriptor argument)
        {
            string output = $"        private readonly global::System.CommandLine.Argument<{argument.FieldType}> {argument.FieldName};";
            return output;
        }

        private static string GenerateArgumentValue(ArgumentDescriptor argument)
        {
            string output = $$"""            {{argument.FieldName}} = new global::System.CommandLine.Argument<{{argument.FieldType}}>("{{argument.ArgumentName}}") { Description = "{{argument.Description}}" };""";
            return output;
        }

        private static string GenerateArgumentRegistration(ArgumentDescriptor argument)
        {
            string output = $"            Add({argument.FieldName});";
            return output;
        }

        private static string GenerateArgumentRead(ArgumentDescriptor argument)
        {
            string output = $"            {argument.FieldType} {argument.ArgumentName} = result.GetValue({argument.FieldName});";
            return output;
        }

        private static string ToClrType(CommandLineInputPropertyType propertyType) => propertyType switch
        {
            CommandLineInputPropertyType.String => "string",
            CommandLineInputPropertyType.Boolean => "bool",
            CommandLineInputPropertyType.Int32 => "int",
            CommandLineInputPropertyType.Items => "global::System.Collections.Generic.ICollection<global::Dibix.Sdk.Abstractions.TaskItem>",
            _ => throw new ArgumentOutOfRangeException(nameof(propertyType), propertyType, null)
        };

        private static void WriteIndent(StringBuilder sb, byte tabs)
        {
            const byte tabLength = 4;
            sb.Append(' ', tabLength * tabs);
        }

        private sealed record CommandClassDescriptor(string Namespace, string ClassName, string CommandName, string Description, EquatableArray<ArgumentDescriptor> Arguments, EquatableArray<InputClassDescriptor> InputClasses);

        private sealed record InputClassDescriptor(string Namespace, string ClassName, string? Category, EquatableArray<InputPropertyDescriptor> Properties);

        private readonly record struct InputPropertyDescriptor(string PropertyName, string Type, bool IsItems, bool IsNested);

        private readonly record struct ArgumentDescriptor(string ArgumentName, string FieldName, string FieldType, string ParameterName, string ParameterType, string Description);
    }
}