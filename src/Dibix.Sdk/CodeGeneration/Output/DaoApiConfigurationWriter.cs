﻿using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class DaoApiConfigurationWriter : DaoWriter
    {
        #region Properties
        public override string LayerName => CodeGeneration.LayerName.Business;
        public override string RegionName => "Endpoints";
        #endregion

        #region Overrides
        public override bool HasContent(CodeGenerationModel model) => model.Controllers.Any();

        public override IEnumerable<string> GetGlobalAnnotations(CodeGenerationModel model)
        {
            string areaName = NamespaceUtility.EnsureAreaName(model.AreaName);
            yield return $"ApiRegistration(\"{areaName}\")";
        }

        public override void Write(DaoCodeGenerationContext context)
        {
            context.AddUsing("Dibix.Http");

            if (context.Model.Controllers.Any(x => x.ControllerImports.Any()))
                context.AddUsing(typeof(Type).Namespace);

            string body = WriteBody(context, context.Model.Controllers);

            context.Output
                   .AddClass("ApiConfiguration", CSharpModifiers.Public | CSharpModifiers.Sealed)
                   .Inherits("HttpApiDescriptor")
                   .AddMethod("Configure", "void", body, modifiers: CSharpModifiers.Public | CSharpModifiers.Override);
        }
        #endregion

        #region Private Methods
        private static string WriteBody(DaoCodeGenerationContext context, IList<ControllerDefinition> controllers)
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

                    if (action.Target is GeneratedAccessorMethodTarget referencedActionTarget)
                    {
                        writer.WriteRaw($"typeof({referencedActionTarget.AccessorFullName}), nameof({referencedActionTarget.AccessorFullName}.")
                              .WriteRaw(referencedActionTarget.Name)
                              .WriteRaw(')');
                    }
                    else if (action.Target is ReflectionActionTarget reflectionActionTarget)
                    {
                        writer.WriteRaw($"\"{reflectionActionTarget.AssemblyAndTypeQualifiedMethodName}\"");
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unexpected action target: {action.Target?.GetType()}");
                    }

                    writer.WriteLineRaw("), y =>")
                          .WriteLine("{")
                          .PushIndent();

                    writer.WriteLine($"y.Method = HttpApiMethod.{action.Method};");

                    if (!String.IsNullOrEmpty(action.Description))
                        writer.WriteLine($"y.Description = \"{action.Description}\";");

                    if (!String.IsNullOrEmpty(action.ChildRoute))
                        writer.WriteLine($"y.ChildRoute = \"{action.ChildRoute}\";");

                    if (action.BodyContract != null)
                    {
                        if (!(action.BodyContract is SchemaTypeReference bodySchemaReference))
                            throw new InvalidOperationException($"Unexpected body type: {action.BodyContract?.GetType()}");

                        writer.WriteLine($"y.BodyContract = typeof({bodySchemaReference.Key});");
                    }

                    if (!String.IsNullOrEmpty(action.BodyBinder))
                    {
                        context.AddUsing(typeof(Type).Namespace);
                        writer.WriteLine($"y.BodyBinder = Type.GetType(\"{action.BodyBinder}\", true);");
                    }

                    foreach (KeyValuePair<string, ActionParameterSource> parameter in action.ParameterSources)
                    {
                        WriteParameter(writer, parameter.Key, parameter.Value);
                    }

                    if (action.IsAnonymous)
                        writer.WriteLine("y.IsAnonymous = true;");

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
                case ActionParameterConstantSource constant:
                    string constantValue = constant.Value is bool boolValue ? boolValue.ToString().ToLowerInvariant() : constant.Value.ToString();
                    writer.WriteLine($"{sourceSelectorVariable}.ResolveParameterFromConstant(\"{parameterName}\", {constantValue});");
                    break;

                case ActionParameterBodySource body:
                    writer.WriteLine($"{sourceSelectorVariable}.ResolveParameterFromBody(\"{parameterName}\", \"{body.ConverterName}\");");
                    break;

                case ActionParameterPropertySource property:
                    writer.Write($"{sourceSelectorVariable}.ResolveParameterFromSource(\"{parameterName}\", \"{property.SourceName}\", \"{property.PropertyName}\"");

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

                    writer.WriteLineRaw(");");

                    break;

                default:
                    throw new InvalidOperationException($"Unsupported parameter source for {parameterName}: {value.GetType()}");
            }
        }
        #endregion
    }
}