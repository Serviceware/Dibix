using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ApiControllerClassWriter : ArtifactWriterBase
    {
        #region Fields
        private readonly IDictionary<string, IList<ReflectionActionDescriptor>> _map;
        #endregion

        #region Properties
        public override string LayerName => CodeGeneration.LayerName.Business;
        public override string RegionName => "Controller Abstractions";
        #endregion

        #region Constructor
        public ApiControllerClassWriter(CodeGenerationModel model)
        {
            _map = CollectReflectionTargetActions(model).GroupBy(x => x.ClassName)
                                                        .ToDictionary(x => x.Key, x => (IList<ReflectionActionDescriptor>)x.ToArray());
        }
        #endregion

        #region Overrides
        public override bool HasContent(CodeGenerationModel model) => _map.Any();

        public override void Write(CodeGenerationContext context)
        {
            context.AddUsing("Dibix");

            CSharpStatementScope scope = context.CreateOutputScope();
            foreach (KeyValuePair<string, IList<ReflectionActionDescriptor>> entry in _map)
            {
                string className = $"{entry.Key}Base";
                CSharpClass @class = scope.AddClass(className, CSharpModifiers.Public | CSharpModifiers.Abstract);
                AddMethods(@class, context, entry.Value, isImplementation: true);
                @class.AddSeparator();
                AddMethods(@class, context, entry.Value, isImplementation: false);
            }
        }
        #endregion

        #region Private Methods
        private static void AddMethods(CSharpClass @class, CodeGenerationContext context, IList<ReflectionActionDescriptor> descriptors, bool isImplementation)
        {
            for (int i = 0; i < descriptors.Count; i++)
            {
                ReflectionActionDescriptor descriptor = descriptors[i];
                ActionDefinition action = descriptor.Action;
                ICollection<ActionParameter> parameters = action.Parameters
                                                                .OrderBy(x => x.DefaultValue != null)
                                                                .ToArray();

                string methodName = descriptor.MethodName;
                string returnTypeName = DetermineReturnTypeName(action, context);
                string methodImplementationName = $"{methodName}Implementation";

                CSharpMethod method;
                if (isImplementation)
                {
                    IList<string> parameterNames = parameters.Select(x => x.InternalParameterName).ToList();
                    parameterNames.Add("cancellationToken");
                    string body = $"return {methodImplementationName}({String.Join(", ", parameterNames)});";
                    method = @class.AddMethod(name: methodName, returnType: returnTypeName, body);
                }
                else
                {
                    method = @class.AddMethod(name: methodImplementationName, returnType: returnTypeName, CSharpModifiers.Protected | CSharpModifiers.Abstract);
                }

                foreach (ActionParameter parameter in parameters)
                {
                    CSharpValue defaultValue = isImplementation && parameter.DefaultValue != null ? context.BuildDefaultValueLiteral(parameter.DefaultValue) : null;
                    method.AddParameter(parameter.InternalParameterName, context.ResolveTypeName(parameter.Type), defaultValue);
                }

                context.AddUsing<CancellationToken>();
                method.AddParameter("cancellationToken", nameof(CancellationToken), isImplementation ? new CSharpValue("default") : null);

                if (i + 1 < descriptors.Count)
                    @class.AddSeparator();
            }
        }

        private static string DetermineReturnTypeName(ActionDefinition definition, CodeGenerationContext context)
        {
            context.AddUsing<Task<object>>();

            StringBuilder sb = new StringBuilder("Task");

            if (definition.DefaultResponseType != null)
            {
                string returnTypeName = context.ResolveTypeName(definition.DefaultResponseType);
                sb.Append($"<{returnTypeName}>");
            }

            return sb.ToString();
        }

        private static IEnumerable<ReflectionActionDescriptor> CollectReflectionTargetActions(CodeGenerationModel model)
        {
            foreach (ControllerDefinition controller in model.Controllers)
            {
                foreach (ActionDefinition action in controller.Actions)
                {
                    if (action.Target is not ReflectionActionTarget reflectionActionTarget) 
                        continue;

                    string[] parts = reflectionActionTarget.AccessorFullName.Split('.');
                    string className = parts[parts.Length - 1];
                    string methodName = reflectionActionTarget.OperationName;
                    ReflectionActionDescriptor descriptor = new ReflectionActionDescriptor(action, className, methodName);
                    yield return descriptor;
                }
            }
        }
        #endregion

        #region Nested Types
        private readonly struct ReflectionActionDescriptor
        {
            public ActionDefinition Action { get; }
            public string ClassName { get; }
            public string MethodName { get; }

            public ReflectionActionDescriptor(ActionDefinition action, string className, string methodName)
            {
                Action = action;
                ClassName = className;
                MethodName = methodName;
            }
        }
        #endregion
    }
}