using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ApiDescriptionWriter : ArtifactWriterBase
    {
        #region Properties
        public override string LayerName => CodeGeneration.LayerName.Business;
        public override string RegionName => "Endpoints";
        #endregion

        #region Overrides
        public override bool HasContent(CodeGenerationModel model) => model.Controllers.Any();

        public override IEnumerable<CSharpAnnotation> GetGlobalAnnotations(CodeGenerationModel model)
        {
            string areaName = NamespaceUtility.EnsureAreaName(model.AreaName);
            yield return new CSharpAnnotation("AreaRegistration", new CSharpStringValue(areaName));
        }

        public override void Write(CodeGenerationContext context)
        {
            context.AddDibixHttpServerReference();

            if (context.Model.Controllers.Any(x => x.ControllerImports.Any()))
                context.AddUsing<Type>();

            string body = WriteBody(context, context.Model.Controllers);

            context.Output
                   .AddClass("ApiConfiguration", CSharpModifiers.Public | CSharpModifiers.Sealed)
                   .Inherits("HttpApiDescriptor")
                   .AddMethod("Configure", "void", body, modifiers: CSharpModifiers.Public | CSharpModifiers.Override)
                   .AddParameter("context", "IHttpApiDiscoveryContext");
        }
        #endregion

        #region Private Methods
        private static string WriteBody(CodeGenerationContext context, IList<ControllerDefinition> controllers)
        {
            StringWriter writer = new StringWriter();
            for (int i = 0; i < controllers.Count; i++)
            {
                ControllerDefinition controller = controllers[i];
                writer.WriteLine($"base.RegisterController(\"{controller.Name}\", x => ")
                      .WriteLine("{")
                      .PushIndent();

                foreach (ActionDefinition action in controller.Actions)
                {
                    writer.Write("x.AddAction(ReflectionHttpActionTarget.Create(");

                    if (action.Target.HasRefParameters) 
                        writer.WriteRaw("context, ");

                    if (action.Target is ReflectionActionTarget reflectionActionTarget)
                    {
                        writer.WriteRaw($"\"{reflectionActionTarget.AccessorFullName}.{reflectionActionTarget.OperationName},{reflectionActionTarget.AssemblyName}\"");
                    }
                    else
                    {
                        writer.WriteRaw($"typeof({action.Target.AccessorFullName}), nameof({action.Target.AccessorFullName}.")
                              .WriteRaw(action.Target.OperationName);
                        
                        if (action.Target.IsAsync)
                            writer.WriteRaw("Async");

                        writer.WriteRaw(')');
                    }

                    writer.WriteLineRaw("), y =>")
                          .WriteLine("{")
                          .PushIndent();

                    writer.WriteLine($"y.Method = HttpApiMethod.{action.Method};");

                    if (!String.IsNullOrEmpty(action.Description))
                        writer.WriteLine($"y.Description = \"{action.Description}\";");

                    if (!String.IsNullOrEmpty(action.ChildRoute))
                        writer.WriteLine($"y.ChildRoute = \"{action.ChildRoute}\";");

                    // TODO: Involves a breaking change
                    //if (action.RequestBody != null)
                    //{
                    //    string @null = ComputeConstantLiteral(null);
                    //    string binder = action.RequestBody.Binder != null ? $"Type.GetType(\"{action.RequestBody.Binder}\", true);" : @null;
                    //    writer.WriteLine($"y.Body = new HttpRequestBody(contract: typeof({context.ResolveTypeName(action.RequestBody.Contract)}), binder: {binder});");
                    //}

                    if (action.RequestBody?.Contract != null) 
                        writer.WriteLine($"y.BodyContract = typeof({context.ResolveTypeName(action.RequestBody.Contract, context)});");

                    if (!String.IsNullOrEmpty(action.RequestBody?.Binder))
                    {
                        context.AddUsing<Type>();
                        writer.WriteLine($"y.BodyBinder = Type.GetType(\"{action.RequestBody.Binder}\", true);");
                    }

                    foreach (ActionParameter parameter in action.Parameters.Where(x => x.Source != null))
                    {
                        WriteParameter(writer, parameter.InternalParameterName, parameter.Source);
                    }

                    if (action.SecuritySchemes.Any(x => x.Contains(SecuritySchemes.Anonymous.Name)))
                        writer.WriteLine("y.IsAnonymous = true;");

                    if (action.FileResponse != null)
                        writer.WriteLine($"y.FileResponse = new HttpFileResponseDefinition(cache: {ComputeConstantLiteral(action.FileResponse.Cache)});");

                    writer.PopIndent()
                          .WriteLine("});");
                }

                foreach (string controllerImport in controller.ControllerImports)
                    writer.WriteLine($"x.ControllerImports.Add(\"{controllerImport}\");");

                writer.PopIndent()
                      .Write("});");

                if (i + 1 < controllers.Count)
                    writer.WriteLine();
            }

            return writer.ToString();
        }

        private static void WriteParameter(StringWriter writer, string parameterName, ActionParameterSource value, char? parentSourceSelectorVariable = null)
        {
            char sourceSelectorVariable = parentSourceSelectorVariable ?? 'y';
            switch (value)
            {
                case ActionParameterConstantSource constant when constant.Value != null:
                    string constantLiteral = ComputeConstantLiteral(constant.Value);
                    writer.WriteLine($"{sourceSelectorVariable}.ResolveParameterFromConstant(\"{parameterName}\", {constantLiteral});");
                    break;

                case ActionParameterConstantSource constant when constant.Value == null:
                    writer.WriteLine($"{sourceSelectorVariable}.ResolveParameterFromNull(\"{parameterName}\");");
                    break;

                case ActionParameterBodySource body:
                    writer.WriteLine($"{sourceSelectorVariable}.ResolveParameterFromBody(\"{parameterName}\", \"{body.ConverterName}\");");
                    break;

                case ActionParameterPropertySource property:
                    writer.Write($"{sourceSelectorVariable}.ResolveParameterFromSource(\"{parameterName}\", \"{property.Definition.Name}\", \"{property.PropertyName}\"");

                    if (property.ItemSources.Any())
                    {
                        if (parentSourceSelectorVariable != null)
                            throw new InvalidOperationException("Nested item sources are not supported");

                        char itemSourceSelectorVariable = 'z';
                        writer.WriteRaw($", {itemSourceSelectorVariable} => ")
                              .WriteLine()
                              .WriteLine("{")
                              .PushIndent();

                        foreach (KeyValuePair<string, ActionParameterSource> parameterSource in property.ItemSources)
                        {
                            WriteParameter(writer, parameterSource.Key, parameterSource.Value, itemSourceSelectorVariable);
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
                case null: return "null";
                default: return value.ToString();
            }
        }
        #endregion
    }
}