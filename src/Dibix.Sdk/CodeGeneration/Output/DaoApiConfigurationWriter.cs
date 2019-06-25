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

        public void Write(DaoWriterContext context)
        {
            if (context.Artifacts.Controllers.Any(x => x.ControllerImports.Any()))
                context.Output.AddUsing("System");

            string body = WriteBody(context.Artifacts.Controllers);

            context.Output
                   .BeginScope(LayerName.Business)
                   .AddClass("ApiConfiguration", CSharpModifiers.Public | CSharpModifiers.Sealed)
                   .AddMethod("Configure", "void", body, modifiers: CSharpModifiers.Protected | CSharpModifiers.Override);
        }
        #endregion

        #region Private Methods
        private static string WriteBody(IEnumerable<ControllerDefinition> controllers)
        {
            StringWriter writer = new StringWriter();
            foreach (ControllerDefinition controller in controllers)
            {
                writer.WriteLine($"base.RegisterController(\"{controller.Name}\", x => ")
                      .WriteLine("{")
                      .PushIndent();

                foreach (ActionDefinition action in controller.Actions)
                {
                    writer.Write("x.AddAction(ReflectionHttpActionTarget.Create(");

                    if (action.Target.IsExternal)
                        writer.WriteRaw('"');

                    writer.WriteRaw(action.Target.Target);

                    if (action.Target.IsExternal)
                        writer.WriteRaw('"');

                    writer.WriteLineRaw("), y =>")
                          .WriteLine("{")
                          .PushIndent();

                    writer.WriteLine($"y.Method = HttpApiMethod.{action.Method};");

                    if (!String.IsNullOrEmpty(action.ChildRoute))
                        writer.WriteLine($"y.ChildRoute = \"{action.ChildRoute}\";");

                    if (action.OmitResult)
                        writer.WriteLine("y.OmitResult = true;");

                    foreach (ActionParameterMapping parameter in action.DynamicParameters)
                    {
                        writer.WriteLine($"y.ResolveParameter(\"{parameter.TargetParameterName}\", \"{parameter.SourceName}\", \"{parameter.SourcePropertyName}\");");
                    }

                    writer.PopIndent()
                          .WriteLine("});");
                }

                writer.PopIndent()
                      .Write('}');
            }

            return writer.ToString();
        }
        #endregion
    }
}