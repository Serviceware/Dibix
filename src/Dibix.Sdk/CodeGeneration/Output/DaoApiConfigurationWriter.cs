using System;
using System.Collections.Generic;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class DaoApiConfigurationWriter : IDaoWriter
    {
        #region Properties
        public string RegionName => "Endpoints";
        #endregion

        #region IDaoWriter Members
        public bool HasContent(OutputConfiguration configuration, SourceArtifacts artifacts) => artifacts.Controllers.Any();

        public IEnumerable<string> GetGlobalAnnotations(OutputConfiguration configuration)
        {
            yield return $"ApiRegistration(\"{configuration.Namespace.Split('.')[1]}\")";
        }

        public void Write(DaoWriterContext context)
        {
            context.Output.AddUsing("Dibix.Http");

            if (context.Artifacts.Controllers.Any(x => x.ControllerImports.Any()))
                context.Output.AddUsing("System");

            string body = WriteBody(context.Configuration, context.Artifacts.Controllers);

            context.Output
                   .BeginScope(LayerName.Business)
                   .AddClass("ApiConfiguration", CSharpModifiers.Public | CSharpModifiers.Sealed)
                   .Inherits("HttpApiDescriptor")
                   .AddMethod("Configure", "void", body, modifiers: CSharpModifiers.Public | CSharpModifiers.Override);
        }
        #endregion

        #region Private Methods
        private static string WriteBody(OutputConfiguration configuration, IList<ControllerDefinition> controllers)
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
                        string @namespace = "Data";
                        int statementNameIndex = action.Target.Target.LastIndexOf('.');
                        if (statementNameIndex >= 0)
                            @namespace = action.Target.Target.Substring(0, statementNameIndex);

                        string typeName = $"{@namespace}.{configuration.ClassName}";
                        writer.WriteRaw($"typeof({typeName}), nameof({typeName}.")
                              .WriteRaw(action.Target.Target.Substring(statementNameIndex + 1))
                              .WriteRaw(')');
                    }

                    writer.WriteLineRaw("), y =>")
                          .WriteLine("{")
                          .PushIndent();

                    writer.WriteLine($"y.Method = HttpApiMethod.{action.Method};");

                    if (!String.IsNullOrEmpty(action.ChildRoute))
                        writer.WriteLine($"y.ChildRoute = \"{action.ChildRoute}\";");

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

                case ActionParameterComplexSource complex:
                    writer.WriteRaw($"typeof({complex.ContractName})");
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