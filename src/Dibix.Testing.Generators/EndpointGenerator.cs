using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Dibix.Generators;
using Dibix.Http;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
            IncrementalValueProvider<ImmutableArray<EndpointGroupDescriptor>> endpoints = context.SyntaxProvider
                                                                                                 .CreateSyntaxProvider(predicate: MatchSyntaxNode, transform: Collect)
                                                                                                 .Where(static x => x is not null)
                                                                                                 .Collect()!;

            IncrementalValueProvider<(ImmutableArray<EndpointGroupDescriptor> Left, string? Right)> input = endpoints.Combine(rootNamespace);

            context.RegisterSourceOutput(input, (x, y) => GenerateCode(x, y.Left, y.Right));
            context.RegisterPostInitializationOutput(x => x.CollectEmbeddedSources(nameof(EndpointGenerator)));
        }

        private static bool MatchSyntaxNode(SyntaxNode node, CancellationToken cancellationToken)
        {
            if (node is not ClassDeclarationSyntax @class)
                return false;

            if (!@class.IsDefined("TestClass"))
                return false;

            foreach (MemberDeclarationSyntax member in @class.Members)
            {
                if (!member.IsKind(SyntaxKind.MethodDeclaration))
                    continue;

                if (!member.IsDefined("TestMethod"))
                    continue;

                if (!member.IsDefined("Endpoint"))
                    continue;

                return true;
            }

            return false;
        }

        private static EndpointGroupDescriptor? Collect(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            ClassDeclarationSyntax @class = (ClassDeclarationSyntax)context.Node;
            INamedTypeSymbol? classSymbol = context.SemanticModel.GetDeclaredSymbol(@class);

            if (classSymbol is null)
                return null;

            bool hasTestClassAttribute = classSymbol.GetAttributes().Any(x => x.AttributeClass?.ToDisplayString() == "Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute");
            if (!hasTestClassAttribute)
                return null;

            ImmutableArray<EndpointDescriptor>.Builder endpointsBuilder = ImmutableArray.CreateBuilder<EndpointDescriptor>();

            foreach (MemberDeclarationSyntax member in @class.Members)
            {
                if (member is not MethodDeclarationSyntax method)
                    continue;

                IMethodSymbol? methodSymbol = context.SemanticModel.GetDeclaredSymbol(method);
                if (methodSymbol is null)
                    continue;

                bool hasTestMethodAttribute = methodSymbol.GetAttributes().Any(x => x.AttributeClass?.ToDisplayString() == "Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute");
                if (!hasTestMethodAttribute)
                    continue;

                AttributeData? endpointAttribute = methodSymbol.GetAttributes().FirstOrDefault(x => x.AttributeClass?.ToDisplayString() == EndpointAttributeName);

                if (endpointAttribute is null)
                    continue;

                string? actionName = GetAttributeNamedArgumentValue<string>(endpointAttribute, nameof(EndpointAttribute.ActionName));
                bool? withAuthorization = GetAttributeNamedArgumentValue<bool?>(endpointAttribute, nameof(EndpointAttribute.WithAuthorization));
                bool? anonymous = GetAttributeNamedArgumentValue<bool?>(endpointAttribute, nameof(EndpointAttribute.Anonymous));

                endpointsBuilder.Add(new EndpointDescriptor
                (
                    MethodName: $"{methodSymbol.Name}_Endpoint",
                    ActionName: actionName,
                    Authorization: withAuthorization.Equals(true) ? [new EndpointAuthorizationDescriptor($"{methodSymbol.Name}_Authorization")] : [],
                    Anonymous: anonymous.Equals(true)
                ));
            }

            ImmutableArray<EndpointDescriptor> endpoints = endpointsBuilder.ToImmutable();
            string @namespace = classSymbol.ContainingNamespace?.ToDisplayString() ?? "Global";
            string className = classSymbol.Name;
            EndpointGroupDescriptor group = new EndpointGroupDescriptor(@namespace, className, endpoints);

            return group;
        }

        private static T? GetAttributeNamedArgumentValue<T>(AttributeData attribute, string name) => (T?)attribute.NamedArguments.FirstOrDefault(x => x.Key == name).Value.Value;

        private static void GenerateCode(SourceProductionContext context, ImmutableArray<EndpointGroupDescriptor> groups, string? rootNamespace)
        {
            if (groups.IsEmpty)
                return;

            string @namespace = rootNamespace ?? groups[0].Namespace;
            context.AddSource("TestHttpApiDescriptor.g.cs", GenerateApiDescriptor(groups, @namespace));
            context.AddSource("HttpClientExtensions.g.cs", GenerateHttpClientExtensions(groups, @namespace));

            foreach (EndpointGroupDescriptor group in groups)
            {
                context.AddSource($"{group.ClassName}.g.cs", GenerateSharedTestClass(group));
            }
        }

        private static string GenerateApiDescriptor(ImmutableArray<EndpointGroupDescriptor> groups, string @namespace)
        {
            return $$"""
                     {{SourceGeneratorUtility.GeneratedCodeHeader}}

                     namespace {{@namespace}}
                     {
                         internal sealed partial class TestHttpApiDescriptor : global::Dibix.Http.Server.HttpApiDescriptor
                         {
                             public TestHttpApiDescriptor()
                             {
                                 Metadata.ProductName = "Dibix";
                                 Metadata.AreaName = "Tests";
                             }
                             
                             public override void Configure(global::Dibix.Http.Server.IHttpApiDiscoveryContext context)
                             {{{GenerateControllerRegistrations(groups)}}
                             }
                         }
                     }
                     """;
        }

        private static string GenerateHttpClientExtensions(ImmutableArray<EndpointGroupDescriptor> groups, string @namespace)
        {
            return $$"""
                     {{SourceGeneratorUtility.GeneratedCodeHeader}}

                     namespace {{@namespace}}
                     {
                         internal static partial class HttpClientExtensions
                         {{{GenerateHttpClientExtensions(groups)}}
                         }
                     }
                     """;
        }
        private static string GenerateHttpClientExtensions(ImmutableArray<EndpointGroupDescriptor> groups)
        {
            StringBuilder sb = new StringBuilder();

            foreach ((EndpointGroupDescriptor group, EndpointDescriptor endpoint) in groups.SelectMany(x => x.Endpoints, (x, y) => (x, y)))
            {
                sb.AppendLine();

                string url = $"api/Tests/{group.ClassName}";
                if (endpoint.ActionName != null)
                    url = $"{url}/{endpoint.ActionName}";

                sb.Append($$"""
                                    public static global::System.Threading.Tasks.Task<global::System.Net.Http.HttpResponseMessage> {{group.ClassName}}_{{endpoint.MethodName}}(this global::System.Net.Http.HttpClient httpClient)
                                    {
                                        return httpClient.GetAsync("{{url}}");
                                    }
                            """);
            }

            string text = sb.ToString();
            return text;
        }

        private static string GenerateSharedTestClass(EndpointGroupDescriptor group)
        {
            return $$"""
                    {{SourceGeneratorUtility.GeneratedCodeHeader}}
                    
                    namespace {{group.Namespace}}
                    {
                        public sealed partial class {{group.ClassName}}
                        {{{GenerateEndpointHandlers(group.Endpoints)}}
                        }
                    }
                    """;
        }

        private static string GenerateControllerRegistrations(ImmutableArray<EndpointGroupDescriptor> groups)
        {
            StringBuilder sb = new StringBuilder();

            foreach (EndpointGroupDescriptor group in groups)
            {
                sb.AppendLine();

                string fullTypeName = $"global::{group.Namespace}.{group.ClassName}";
                sb.Append($$"""
                                       RegisterController(nameof({{fullTypeName}}), x =>
                                       {{{GenerateActionRegistrations(group, fullTypeName)}}
                                       });
                           """);
            }

            string text = sb.ToString();
            return text;
        }

        private static string GenerateActionRegistrations(EndpointGroupDescriptor group, string fullTypeName)
        {
            string CreateActionTarget(string methodName) => $"global::Dibix.Http.Server.LocalReflectionHttpActionTarget.Create(typeof({fullTypeName}), nameof({fullTypeName}.{methodName}))";

            StringBuilder sb = new StringBuilder();

            foreach (EndpointDescriptor endpoint in group.Endpoints)
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

        private sealed record EndpointGroupDescriptor(string Namespace, string ClassName, IReadOnlyCollection<EndpointDescriptor> Endpoints);

        private sealed record EndpointDescriptor(string MethodName, string? ActionName, IReadOnlyCollection<EndpointAuthorizationDescriptor> Authorization, bool Anonymous);

        private sealed record EndpointAuthorizationDescriptor(string MethodName);
    }
}