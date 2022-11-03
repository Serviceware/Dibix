using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ApiDescriptionWriter : ArtifactWriterBase
    {
        #region Fields
        private readonly bool _assumeEmbeddedActionTargets;
        #endregion

        #region Properties
        public override string LayerName => CodeGeneration.LayerName.Business;
        public override string RegionName => "Endpoints";
        #endregion

        #region Constructor
        public ApiDescriptionWriter(bool assumeEmbeddedActionTargets)
        {
            this._assumeEmbeddedActionTargets = assumeEmbeddedActionTargets;
        }
        #endregion

        #region Overrides
        public override bool HasContent(CodeGenerationModel model) => model.Controllers.Any();

        public override IEnumerable<CSharpAnnotation> GetGlobalAnnotations(CodeGenerationModel model)
        {
            string areaName = PathUtility.EnsureAreaName(model.ArtifactGenerationConfiguration.AreaName);
            yield return new CSharpAnnotation("AreaRegistration", new CSharpStringValue(areaName));
        }

        public override void Write(CodeGenerationContext context)
        {
            context.AddDibixHttpServerReference();

            if (context.Model.Controllers.Any(x => x.ControllerImports.Any()))
                context.AddUsing<Type>();

            string body = this.WriteBody(context, context.Model.Controllers);

            context.CreateOutputScope()
                   .AddClass("ApiConfiguration", CSharpModifiers.Public | CSharpModifiers.Sealed)
                   .Inherits("HttpApiDescriptor")
                   .AddMethod("Configure", "void", body, modifiers: CSharpModifiers.Public | CSharpModifiers.Override)
                   .AddParameter("context", "IHttpApiDiscoveryContext");
        }
        #endregion

        #region Private Methods
        private string WriteBody(CodeGenerationContext context, IList<ControllerDefinition> controllers)
        {
            StringWriter writer = new StringWriter();
            for (int i = 0; i < controllers.Count; i++)
            {
                ControllerDefinition controller = controllers[i];
                writer.WriteLine($"base.RegisterController(\"{controller.Name}\", controller =>")
                      .WriteLine("{")
                      .PushIndent();

                foreach (ActionDefinition action in controller.Actions)
                {
                    WriteAddAction(context, writer, action);
                }

                foreach (string controllerImport in controller.ControllerImports)
                    writer.WriteLine($"controller.Import(\"{controllerImport}\");");

                writer.PopIndent()
                      .Write("});");

                if (i + 1 < controllers.Count)
                    writer.WriteLine();
            }

            return writer.ToString();
        }

        private void WriteAddAction(CodeGenerationContext context, StringWriter writer, ActionDefinition action)
        {
            writer.Write("controller.AddAction(");
            WriteActionTarget(context, writer, action, "action", WriteActionConfiguration);
        }

        private void WriteActionTarget<T>(CodeGenerationContext context, StringWriter writer, T actionTargetDefinition, string variableName, Action<CodeGenerationContext, StringWriter, T, string> body) where T : ActionTargetDefinition
        {
            writer.WriteRaw("ReflectionHttpActionTarget.Create(");

            if (actionTargetDefinition.Target.HasRefParameters)
                writer.WriteRaw("context, ");

            if (actionTargetDefinition.Target is ReflectionActionTarget reflectionActionTarget)
            {
                writer.WriteRaw($"\"{reflectionActionTarget.AccessorFullName}.{reflectionActionTarget.OperationName},{reflectionActionTarget.AssemblyName}\"");
            }
            else
            {
                string accessorFullName = actionTargetDefinition.Target.AccessorFullName;

                if (!this._assumeEmbeddedActionTargets && actionTargetDefinition.Target is LocalActionTarget localActionTarget)
                    accessorFullName = localActionTarget.ExternalAccessorFullName;

                writer.WriteRaw($"typeof({accessorFullName}), nameof({accessorFullName}.")
                      .WriteRaw(actionTargetDefinition.Target.OperationName);

                if (actionTargetDefinition.Target.IsAsync)
                    writer.WriteRaw("Async");

                writer.WriteRaw(')');
            }

            writer.WriteLineRaw($"), {variableName} =>")
                  .WriteLine("{")
                  .PushIndent();

            body(context, writer, actionTargetDefinition, variableName);

            foreach (ActionParameter parameter in actionTargetDefinition.Parameters.Where(x => x.Source != null))
            {
                WriteParameter(writer, parameter.InternalParameterName, parameter.Source, variableName);
            }

            writer.PopIndent()
                  .WriteLine("});");
        }

        private void WriteActionConfiguration(CodeGenerationContext context, StringWriter writer, ActionDefinition action, string variableName)
        {
            writer.WriteLine($"{variableName}.Method = HttpApiMethod.{action.Method};");

            if (!String.IsNullOrEmpty(action.Description))
                writer.WriteLine($"{variableName}.Description = \"{action.Description}\";");

            if (action.ChildRoute != null)
                writer.WriteLine($"{variableName}.ChildRoute = \"{action.ChildRoute.Value}\";");

            // TODO: Involves a breaking change
            //if (action.RequestBody != null)
            //{
            //    string @null = ComputeConstantLiteral(null);
            //    string binder = action.RequestBody.Binder != null ? $"Type.GetType(\"{action.RequestBody.Binder}\", true);" : @null;
            //    writer.WriteLine($"{variableName}.Body = new HttpRequestBody(contract: typeof({context.ResolveTypeName(action.RequestBody.Contract)}), binder: {binder});");
            //}

            if (action.RequestBody?.Contract != null)
                writer.WriteLine($"{variableName}.BodyContract = typeof({context.ResolveTypeName(action.RequestBody.Contract, context)});");

            if (!String.IsNullOrEmpty(action.RequestBody?.Binder))
            {
                context.AddUsing<Type>();
                writer.WriteLine($"{variableName}.BodyBinder = Type.GetType(\"{action.RequestBody.Binder}\", true);");
            }

            if (action.SecuritySchemes.Any(x => x.Contains(SecuritySchemes.Anonymous.Name)))
                writer.WriteLine($"{variableName}.IsAnonymous = true;");

            if (action.FileResponse != null)
                writer.WriteLine($"{variableName}.FileResponse = new HttpFileResponseDefinition(cache: {ComputeConstantLiteral(action.FileResponse.Cache)});");

            if (action.Authorization != null)
            {
                writer.Write($"{variableName}.WithAuthorization(");
                WriteActionTarget(context, writer, action.Authorization, "authorization", WriteAuthorizationBehavior);
            }
        }

        private static void WriteAuthorizationBehavior(CodeGenerationContext context, StringWriter writer, AuthorizationBehavior authorization, string variableName) { }

        private static void WriteParameter(StringWriter writer, string parameterName, ActionParameterSource value, string variableName)
        {
            switch (value)
            {
                case ActionParameterConstantSource constant when constant.Value != null:
                    string constantLiteral = ComputeConstantLiteral(constant.Value);
                    writer.WriteLine($"{variableName}.ResolveParameterFromConstant(\"{parameterName}\", {constantLiteral});");
                    break;

                case ActionParameterConstantSource constant when constant.Value == null:
                    writer.WriteLine($"{variableName}.ResolveParameterFromNull(\"{parameterName}\");");
                    break;

                case ActionParameterBodySource body:
                    writer.WriteLine($"{variableName}.ResolveParameterFromBody(\"{parameterName}\", \"{body.ConverterName}\");");
                    break;

                case ActionParameterPropertySource property:
                    writer.Write($"{variableName}.ResolveParameterFromSource(\"{parameterName}\", \"{property.Definition.Name}\", \"{property.PropertyName}\"");

                    if (property.ItemSources.Any())
                    {
                        const string itemSourceSelectorVariable = "items";
                        writer.WriteRaw($", {itemSourceSelectorVariable} =>")
                              .WriteLine()
                              .WriteLine("{")
                              .PushIndent();

                        foreach (ActionParameterItemSource parameterSource in property.ItemSources)
                        {
                            WriteParameter(writer, parameterSource.ParameterName, parameterSource.Source, itemSourceSelectorVariable);
                        }

                        writer.PopIndent()
                              .Write("}");
                    }
                    else if (!String.IsNullOrEmpty(property.Converter))
                    {
                        writer.WriteRaw($", \"{property.Converter}\"");
                    }

                    writer.WriteLineRaw(");");

                    break;

                default:
                    throw new InvalidOperationException($"Unsupported parameter source for {parameterName}: {value.GetType()}");
            }
        }

        private static string ComputeConstantLiteral(object value)
        {
            switch (value)
            {
                case bool boolValue: return boolValue.ToString().ToLowerInvariant();
                case string stringValue: return $"\"{stringValue}\"";
                case null: return "null";
                default: return value.ToString();
            }
        }
        #endregion
    }
}