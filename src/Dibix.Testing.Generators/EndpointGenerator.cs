using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Dibix.Generators;
using Dibix.Http;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dibix.Testing.Generators
{
    [Generator]
    public sealed class EndpointGenerator : IIncrementalGenerator
    {
        private static readonly string EndpointAttributeName = typeof(EndpointAttribute).FullName!;

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValueProvider<string?> rootNamespace = context.AnalyzerConfigOptionsProvider.Select((x, _) => x.GetRootNamespace());
            IncrementalValueProvider<ImmutableArray<ClassWithEndpointDescriptor>> classes = context.SyntaxProvider
                                                                                                   .ForAttributeWithMetadataName(EndpointAttributeName, predicate: MatchSyntaxNode, transform: Collect)
                                                                                                   .Where(static x => x is not null)
                                                                                                   .Select(static (x, _) => x!.Value)
                                                                                                   .Collect()
                                                                                                   .Select(static (x, _) => x.GroupBy(z => z.Class, (a, b) => new ClassWithEndpointDescriptor(a, b.Select(z => z.Endpoint)
                                                                                                                                                                                                  .ToImmutableArray()
                                                                                                                                                                                                  .AsEquatableArray()))
                                                                                                                             .ToImmutableArray());

            IncrementalValueProvider<(ImmutableArray<ClassWithEndpointDescriptor> Left, string? Right)> combined = classes.Combine(rootNamespace);

            context.RegisterSourceOutput(combined, static (x, y) => GenerateCode(x, y.Left, y.Right));
            context.RegisterPostInitializationOutput(x => x.CollectEmbeddedSources(nameof(EndpointGenerator)));
        }

        private static bool MatchSyntaxNode(SyntaxNode node, CancellationToken cancellationToken) => node is MethodDeclarationSyntax;

        private static (ClassDescriptor Class, EndpointDescriptor Endpoint)? Collect(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
        {
            IMethodSymbol methodSymbol = (IMethodSymbol)context.TargetSymbol;
            INamedTypeSymbol classSymbol = methodSymbol.ContainingType;

            bool hasTestClassAttribute = classSymbol.GetAttributes().Any(x => x.AttributeClass?.ToDisplayString() == "Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute");
            if (!hasTestClassAttribute)
                return null;

            bool hasTestMethodAttribute = methodSymbol.GetAttributes().Any(x => x.AttributeClass?.ToDisplayString() == "Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute");
            if (!hasTestMethodAttribute)
                return null;

            string @namespace = classSymbol.ContainingNamespace?.ToDisplayString() ?? "Global";
            string className = classSymbol.Name;
            ClassDescriptor @class = new ClassDescriptor(@namespace, className);

            AttributeData endpointAttribute = context.Attributes[0];
            string? actionName = GetAttributeNamedArgumentValue<string>(endpointAttribute, nameof(EndpointAttribute.ActionName));
            bool? withAuthorization = GetAttributeNamedArgumentValue<bool?>(endpointAttribute, nameof(EndpointAttribute.WithAuthorization));
            bool? anonymous = GetAttributeNamedArgumentValue<bool?>(endpointAttribute, nameof(EndpointAttribute.Anonymous));

            ImmutableArray<EndpointAuthorizationDescriptor>.Builder authorizationBuilder = ImmutableArray.CreateBuilder<EndpointAuthorizationDescriptor>();
            if (withAuthorization.Equals(true))
                authorizationBuilder.Add(new EndpointAuthorizationDescriptor($"{methodSymbol.Name}_Authorization"));

            EndpointDescriptor endpoint = new EndpointDescriptor
            (
                MethodName: $"{methodSymbol.Name}_Endpoint",
                ActionName: actionName,
                Authorization: authorizationBuilder.ToImmutable().AsEquatableArray(),
                Anonymous: anonymous.Equals(true)
            );

            return (Class: @class, Endpoint: endpoint);
        }

        private static T? GetAttributeNamedArgumentValue<T>(AttributeData attribute, string name) => (T?)attribute.NamedArguments.FirstOrDefault(x => x.Key == name).Value.Value;

        private static void GenerateCode(SourceProductionContext context, ImmutableArray<ClassWithEndpointDescriptor> classes, string? rootNamespace)
        {
            if (classes.IsEmpty)
                return;

            string @namespace = rootNamespace ?? classes[0].Class.Namespace;
            context.AddSource("TestHttpApiDescriptor.g.cs", GenerateApiDescriptor(classes, @namespace));
            context.AddSource("HttpClientExtensions.g.cs", GenerateHttpClientExtensions(classes, @namespace));

            foreach (ClassWithEndpointDescriptor @class in classes)
            {
                context.AddSource($"{@class.Class.ClassName}.g.cs", GenerateSharedTestClass(@class));
            }
        }

        private static string GenerateApiDescriptor(ImmutableArray<ClassWithEndpointDescriptor> classes, string @namespace)
        {
            return $$"""
                     {{SourceGeneratorUtility.GeneratedCodeHeader}}

                     namespace {{@namespace}}
                     {
                     {{Annotation.ClassText}}
                         internal sealed partial class TestHttpApiDescriptor : global::Dibix.Http.Server.HttpApiDescriptor
                         {
                             public TestHttpApiDescriptor()
                             {
                                 Metadata.ProductName = "Dibix";
                                 Metadata.AreaName = "Tests";
                             }
                             
                             public override void Configure(global::Dibix.Http.Server.IHttpApiDiscoveryContext context)
                             {{{GenerateControllerRegistrations(classes)}}
                             }
                         }
                     }
                     """;
        }

        private static string GenerateHttpClientExtensions(ImmutableArray<ClassWithEndpointDescriptor> classes, string @namespace)
        {
            return $$"""
                     {{SourceGeneratorUtility.GeneratedCodeHeader}}

                     namespace {{@namespace}}
                     {
                     {{Annotation.ClassText}}
                         internal static partial class HttpClientExtensions
                         {{{GenerateHttpClientExtensions(classes)}}
                         }
                     }
                     """;
        }
        private static string GenerateHttpClientExtensions(ImmutableArray<ClassWithEndpointDescriptor> classes)
        {
            StringBuilder sb = new StringBuilder();

            foreach ((ClassWithEndpointDescriptor @class, EndpointDescriptor endpoint) in classes.SelectMany(x => x.Endpoints, (x, y) => (x, y)))
            {
                sb.AppendLine();

                string url = $"api/Tests/{@class.Class.ClassName}";
                if (endpoint.ActionName != null)
                    url = $"{url}/{endpoint.ActionName}";

                sb.Append($$"""
                                    public static global::System.Threading.Tasks.Task<global::System.Net.Http.HttpResponseMessage> {{@class.Class.ClassName}}_{{endpoint.MethodName}}(this global::System.Net.Http.HttpClient httpClient)
                                    {
                                        return httpClient.GetAsync("{{url}}");
                                    }
                            """);
            }

            string text = sb.ToString();
            return text;
        }

        private static string GenerateSharedTestClass(ClassWithEndpointDescriptor @class)
        {
            return $$"""
                    {{SourceGeneratorUtility.GeneratedCodeHeader}}
                    
                    namespace {{@class.Class.Namespace}}
                    {
                    {{Annotation.ClassText}}
                        public sealed partial class {{@class.Class.ClassName}}
                        {{{GenerateEndpointHandlers(@class.Endpoints)}}
                        }
                    }
                    """;
        }

        private static string GenerateControllerRegistrations(ImmutableArray<ClassWithEndpointDescriptor> classes)
        {
            StringBuilder sb = new StringBuilder();

            foreach (ClassWithEndpointDescriptor @class in classes)
            {
                sb.AppendLine();

                string fullTypeName = $"global::{@class.Class.Namespace}.{@class.Class.ClassName}";
                sb.Append($$"""
                                       RegisterController(nameof({{fullTypeName}}), x =>
                                       {{{GenerateActionRegistrations(@class, fullTypeName)}}
                                       });
                           """);
            }

            string text = sb.ToString();
            return text;
        }

        private static string GenerateActionRegistrations(ClassWithEndpointDescriptor @class, string fullTypeName)
        {
            string CreateActionTarget(string methodName) => $"global::Dibix.Http.Server.LocalReflectionHttpActionTarget.Create(typeof({fullTypeName}), nameof({fullTypeName}.{methodName}))";

            StringBuilder sb = new StringBuilder();

            foreach (EndpointDescriptor endpoint in @class.Endpoints)
            {
                sb.AppendLine();

                sb.Append($$"""
                                            x.AddAction({{CreateActionTarget(endpoint.MethodName)}}, y =>
                                            {
                                                y.Method = global::Dibix.Http.HttpApiMethod.Get;
                                                y.RegisterDelegate((global::Microsoft.AspNetCore.Http.HttpContext httpContext, global::Dibix.Http.Server.AspNetCore.IHttpActionDelegator actionDelegator, global::System.Threading.CancellationToken cancellationToken) => actionDelegator.Delegate(httpContext, new global::System.Collections.Generic.Dictionary<string, object>(), cancellationToken));
                           
                            """);

                if (endpoint.ActionName != null)
                {
                    sb.Append(new string(' ', 20))
                      .AppendLine($"y.ChildRoute = \"{endpoint.ActionName}\";");
                }

                sb.Append(new string(' ', 20))
                  .AppendLine($"""y.SecuritySchemes.Add("{(endpoint.Anonymous ? SecuritySchemeNames.Anonymous : SecuritySchemeNames.Bearer)}");""");

                foreach (EndpointAuthorizationDescriptor authorization in endpoint.Authorization)
                {
                    sb.Append(new string(' ', 20))
                      .AppendLine($"y.AddAuthorizationBehavior({CreateActionTarget(authorization.MethodName)}, _ => {{ }});");
                }

                sb.Append(new string(' ', 16))
                  .Append("});");
            }

            string text = sb.ToString();
            return text;
        }

        private static string GenerateEndpointHandlers(IEnumerable<EndpointDescriptor> endpoints)
        {
            StringBuilder sb = new StringBuilder();

            IEnumerable<string> methods = CollectMethods(endpoints);

            foreach (string method in methods)
            {
                sb.AppendLine()
                  .Append(new string(' ', 8));

                sb.Append($"internal static partial void {method}(global::Dibix.IDatabaseAccessorFactory databaseAccessorFactory);");
            }

            string text = sb.ToString();
            return text;
        }

        private static IEnumerable<string> CollectMethods(IEnumerable<EndpointDescriptor> endpoints)
        {
            foreach (EndpointDescriptor endpoint in endpoints)
            {
                yield return endpoint.MethodName;

                foreach (EndpointAuthorizationDescriptor authorization in endpoint.Authorization)
                {
                    yield return authorization.MethodName;
                }
            }
        }

        private readonly record struct ClassDescriptor(string Namespace, string ClassName);

        private sealed record EndpointDescriptor(string MethodName, string? ActionName, EquatableArray<EndpointAuthorizationDescriptor> Authorization, bool Anonymous);

        private readonly record struct EndpointAuthorizationDescriptor(string MethodName);

        private sealed record ClassWithEndpointDescriptor(ClassDescriptor Class, EquatableArray<EndpointDescriptor> Endpoints);
    }
}