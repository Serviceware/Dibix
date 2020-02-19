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

                    if (action.Target.IsExternal)
                    {
                        writer.WriteRaw($"\"{action.Target.Target}\"");
                    }
                    else
                    {
                        writer.WriteRaw($"typeof({action.Target.TypeName}), nameof({action.Target.TypeName}.")
                              .WriteRaw(action.Target.MethodName)
                              .WriteRaw(')');
                    }

                    writer.WriteLineRaw("), y =>")
                          .WriteLine("{")
                          .PushIndent();

                    writer.WriteLine($"y.Method = HttpApiMethod.{action.Method};");

                    if (!String.IsNullOrEmpty(action.Description))
                        writer.WriteLine($"y.Description = \"{action.Description}\";");

                    if (!String.IsNullOrEmpty(action.ChildRoute))
                        writer.WriteLine($"y.ChildRoute = \"{action.ChildRoute}\";");

                    if (!String.IsNullOrEmpty(action.BodyContract))
                    {
                        string bodyContractName = action.BodyContract;
                        if (!bodyContractName.StartsWith(context.Model.RootNamespace, StringComparison.Ordinal))
                            bodyContractName = $"{context.Model.RootNamespace}.{CodeGeneration.LayerName.DomainModel}.{bodyContractName}";

                        writer.WriteLine($"y.BodyContract = typeof({bodyContractName});");
                    }

                    if (!String.IsNullOrEmpty(action.BodyBinder))
                    {
                        context.AddUsing(typeof(Type).Namespace);
                        writer.WriteLine($"y.BodyBinder = Type.GetType(\"{action.BodyBinder}\", true);");
                    }

                    foreach (KeyValuePair<string, ActionParameterSource> parameter in action.DynamicParameters)
                    {
                        WriteParameter(writer, parameter.Key, parameter.Value);
                    }

                    if (action.OmitResult)
                        writer.WriteLine("y.OmitResult = true;");

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

        private static void WriteParameter(StringWriter writer, string parameterName, ActionParameterSource value)
        {
            writer.Write($"y.ResolveParameter(\"{parameterName}\", ");
            switch (value)
            {
                case ActionParameterConstantSource constant:
                    string constantValue = constant.Value is bool boolValue ? boolValue.ToString().ToLowerInvariant() : constant.Value.ToString();
                    writer.WriteRaw(constantValue);
                    break;

                case ActionParameterBodySource body:
                    writer.WriteRaw($"\"{body.ConverterName}\"");
                    break;

                case ActionParameterPropertySource property:
                    writer.WriteRaw($"\"{property.SourceName}\", \"{property.PropertyName}\"");
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported parameter source for {parameterName}: {value.GetType()}");
            }
            writer.WriteLineRaw(");");
        }
        #endregion
    }
}