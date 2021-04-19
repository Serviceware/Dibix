using Dibix.Http.Server;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ApiClientImplementationWriter : ApiClientWriter
    {
        #region Properties
        public override string RegionName => "Implementation";
        #endregion

        #region Overrides

        protected override void WriteController(CodeGenerationContext context, ControllerDefinition controller, string serviceName)
        {
            context.AddHttpClientReference();

            string className = $"{controller.Name}Service";
            string interfaceName = $"I{className}";
            CSharpAnnotation interfaceDescriptor = new CSharpAnnotation("HttpService", new CSharpValue($"typeof({interfaceName})"));
            CSharpClass @class = context.Output
                                        .AddClass(className, CSharpModifiers.Public | CSharpModifiers.Sealed, interfaceDescriptor)
                                        .Implements(interfaceName)
                                        .AddField("BaseAddress", "Uri", new CSharpValue($"new Uri(\"{context.Model.BaseUrl.TrimEnd('/')}/\")"), CSharpModifiers.Private | CSharpModifiers.Static | CSharpModifiers.ReadOnly)
                                        .AddField("_httpClientFactory", "IHttpClientFactory", modifiers: CSharpModifiers.Private | CSharpModifiers.ReadOnly)
                                        .AddField("_authorizationProvider", "IHttpAuthorizationProvider", modifiers: CSharpModifiers.Private | CSharpModifiers.ReadOnly);

            @class.AddSeparator();

            @class.AddConstructor()
                  .AddParameter("authorizationProvider", "IHttpAuthorizationProvider")
                  .CallThis()
                  .AddParameter(new CSharpValue("new DefaultHttpClientFactory()"))
                  .AddParameter(new CSharpValue("authorizationProvider"));

            @class.AddConstructor(@"this._httpClientFactory = httpClientFactory;
this._authorizationProvider = authorizationProvider;")
                  .AddParameter("httpClientFactory", "IHttpClientFactory")
                  .AddParameter("authorizationProvider", "IHttpAuthorizationProvider");

            @class.AddSeparator();

            foreach (ActionDefinition action in controller.Actions)
            {
                // TODO: Remove this shit!
                if (context.Model.AreaName != "Tests" && controller.Name != "UserConfiguration")
                    continue;

                string body = GeneratedMethodBody(controller, action, context);
                base.AddMethod(action, context, (methodName, returnType) => @class.AddMethod(methodName, returnType, body, modifiers: CSharpModifiers.Public | CSharpModifiers.Async));
            }
        }
        #endregion

        #region Private Methods
        private static string GeneratedMethodBody(ControllerDefinition controller, ActionDefinition action, CodeGenerationContext context)
        {
            StringWriter writer = new StringWriter();
            writer.WriteLine("using (HttpClient client = this._httpClientFactory.CreateClient(BaseAddress))")
                  .WriteLine("{")
                  .PushIndent();

            string uri = RouteBuilder.BuildRoute(context.Model.AreaName, controller.Name, action.ChildRoute);
            writer.WriteLine($"HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.{action.Method}, \"{uri}\");");

            foreach (SecurityScheme securityScheme in action.SecuritySchemes)
            {
                writer.WriteLine($"requestMessage.Headers.Add(\"{securityScheme.Name}\", this._authorizationProvider.GetValue(\"{securityScheme.Name}\"));");
            }

            writer.WriteLine("HttpResponseMessage responseMessage = await client.SendAsync(requestMessage).ConfigureAwait(false);");

            string responseContentType = null;
            if (action.DefaultResponseType != null)
                responseContentType = context.ResolveTypeName(action.DefaultResponseType);

            if (responseContentType != null)
            {
                context.AddHttpFormattingReference();
                writer.WriteLine($"{responseContentType} responseContent = await responseMessage.Content.ReadAsAsync<{responseContentType}>().ConfigureAwait(false);");
            }

            writer.Write("return new HttpResponse");

            if (responseContentType != null)
                writer.WriteRaw($"<{responseContentType}>");

            writer.WriteRaw("(responseMessage");

            if (responseContentType != null)
                writer.WriteRaw(", responseContent");

            writer.WriteRaw(");")
                  .WriteLine();

            writer.PopIndent()
                  .Write('}');

            string body = writer.ToString();
            return body;
        }
        #endregion
    }
}