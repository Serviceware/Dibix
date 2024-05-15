using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using Dibix.Http;
using Dibix.Sdk.Abstractions;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ApiDescriptionWriter : ArtifactWriterBase
    {
        #region Fields
        private readonly bool _assumeEmbeddedActionTargets;
        private readonly bool _includeReflectionTargets;
        private readonly bool _includeTargetsWithRefParameters;
        private readonly bool _includeTargetsWithDeepObjectQueryParameters;
        private readonly bool _includeTargetsWithBodyConverter;
        private readonly bool _generateActionDelegates;
        #endregion

        #region Properties
        public override string LayerName => CodeGeneration.LayerName.Business;
        public override string RegionName => "Endpoints";
        #endregion

        #region Constructor
        public ApiDescriptionWriter(bool assumeEmbeddedActionTargets, bool includeReflectionTargets, bool includeTargetsWithRefParameters, bool includeTargetsWithDeepObjectQueryParameters, bool includeTargetsWithBodyConverter, bool generateActionDelegates)
        {
            _assumeEmbeddedActionTargets = assumeEmbeddedActionTargets;
            _includeReflectionTargets = includeReflectionTargets;
            _includeTargetsWithRefParameters = includeTargetsWithRefParameters;
            _includeTargetsWithDeepObjectQueryParameters = includeTargetsWithDeepObjectQueryParameters;
            _includeTargetsWithBodyConverter = includeTargetsWithBodyConverter;
            _generateActionDelegates = generateActionDelegates;
        }
        #endregion

        #region Overrides
        public override bool HasContent(CodeGenerationModel model) => model.Controllers.Any();

        public override IEnumerable<CSharpAnnotation> GetGlobalAnnotations(CodeGenerationModel model)
        {
            string areaName = PathUtility.EnsureAreaName(model.AreaName);
            yield return new CSharpAnnotation("AreaRegistration", new CSharpStringValue(areaName));
        }

        public override void Write(CodeGenerationContext context)
        {
            context.AddUsing("Dibix.Http.Server");

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
                IList<ActionDefinition> actions = CollectActionsToInclude(context, controller).ToArray();
                if (!actions.Any())
                    continue;

                writer.WriteLine($"base.RegisterController(\"{controller.Name}\", controller =>")
                      .WriteLine("{")
                      .PushIndent();

                foreach (ActionDefinition action in actions)
                {
                    WriteAddAction(context, writer, action);
                }

                writer.PopIndent()
                      .Write("});");

                if (i + 1 < controllers.Count)
                    writer.WriteLine();
            }

            return writer.ToString();
        }

        private IEnumerable<ActionDefinition> CollectActionsToInclude(CodeGenerationContext context, ControllerDefinition controller)
        {
            static void LogNotSupportedInHttpHostWarning(string message, CodeGenerationContext context, SourceLocation sourceLocation)
            {
                context.Logger.LogWarning($"{message}, which is not supported in Dibix.Http.Host", sourceLocation);
            }

            foreach (ActionDefinition action in controller.Actions)
            {
                bool includeAction = true;
                if (!_includeReflectionTargets && action.Target is ReflectionActionTarget)
                {
                    LogNotSupportedInHttpHostWarning($"Action target method '{action.Target.OperationName}' is defined within an external assembly", context, action.Target.SourceLocation);
                    includeAction = false;
                }

                foreach (ActionParameter actionParameter in action.Parameters)
                {
                    if (!_includeTargetsWithRefParameters && actionParameter.IsOutput)
                    {
                        LogNotSupportedInHttpHostWarning($"Parameter '{actionParameter.InternalParameterName}' is an output parameter", context, actionParameter.SourceLocation);
                        includeAction = false;
                    }

                    if (!_includeTargetsWithDeepObjectQueryParameters
                     && actionParameter.ParameterLocation == ActionParameterLocation.Query
                     && actionParameter.Type.IsUserDefinedType(context.SchemaRegistry, out UserDefinedTypeSchema userDefinedTypeSchema)
                     && userDefinedTypeSchema.Properties.Count > 1)
                    {
                        LogNotSupportedInHttpHostWarning($"Parameter '{actionParameter.InternalParameterName}' is a deep object query parameter", context, actionParameter.SourceLocation);
                        includeAction = false;
                    }

                    if (!_includeTargetsWithBodyConverter && actionParameter.ParameterSource is ActionParameterBodySource { ConverterName: not null } bodySource)
                    {
                        LogNotSupportedInHttpHostWarning($"Parameter '{actionParameter.InternalParameterName}' uses a converter", context, bodySource.ConverterName.Location);
                        includeAction = false;
                    }
                }

                if (includeAction)
                    yield return action;
            }
        }

        private void WriteAddAction(CodeGenerationContext context, StringWriter writer, ActionDefinition action)
        {
            writer.Write("controller.AddAction(");
            WriteActionTarget(context, writer, action, "action", WriteActionConfiguration);
        }

        private void WriteActionTarget<T>(CodeGenerationContext context, StringWriter writer, T actionTargetDefinition, string variableName, Action<CodeGenerationContext, StringWriter, T, string> body) where T : ActionTargetDefinition
        {
            writer.WriteRaw("ReflectionHttpActionTarget.Create(");

            if (actionTargetDefinition.Parameters.Any(x => x.IsOutput))
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

            foreach (ActionParameter parameter in actionTargetDefinition.Parameters.Where(x => x.ParameterSource != null))
            {
                WriteParameter(context, writer, parameter.InternalParameterName, parameter.ParameterSource, variableName);
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
                writer.WriteLine($"{variableName}.BodyContract = typeof({context.ResolveTypeName(action.RequestBody.Contract)});");

            if (!String.IsNullOrEmpty(action.RequestBody?.Binder))
            {
                context.AddUsing<Type>();
                writer.WriteLine($"{variableName}.BodyBinder = Type.GetType(\"{action.RequestBody.Binder}\", true);");
            }

            foreach (SecuritySchemeRequirement securitySchemeRequirement in action.SecuritySchemes.Requirements)
            {
                writer.WriteLine($"{variableName}.SecuritySchemes.Add(\"{securitySchemeRequirement.Scheme.Name}\");");
            }

            if (action.FileResponse != null)
                writer.WriteLine($"{variableName}.FileResponse = new HttpFileResponseDefinition(cache: {ComputeConstantLiteral(context, action.FileResponse.Cache)});");

            foreach (int disabledAutoDetectionStatusCode in action.DisabledAutoDetectionStatusCodes)
            {
                writer.WriteLine($"{variableName}.DisableStatusCodeDetection({disabledAutoDetectionStatusCode});");
            }

            foreach (KeyValuePair<HttpStatusCode, ActionResponse> response in action.Responses)
            {
                int httpStatusCode = (int)response.Key;
                ActionResponse actionResponse = response.Value;
                ErrorDescription error = actionResponse.StatusCodeDetectionDetail;

                if (error == null)
                    continue;

                int errorCode = error.ErrorCode;
                string errorMessage = error.Description;
                writer.WriteLine($"{variableName}.SetStatusCodeDetectionResponse({httpStatusCode}, {errorCode}, {(errorMessage != null ? $"\"{errorMessage}\"" : "errorMessage: null")});");
            }

            if (action.Authorization != null)
            {
                writer.Write($"{variableName}.WithAuthorization(");
                WriteActionTarget(context, writer, action.Authorization, "authorization", WriteAuthorizationBehavior);
            }

            if (_generateActionDelegates)
                WriteDelegate(context, action, writer, variableName);
        }

        private static void WriteDelegate(CodeGenerationContext context, ActionDefinition action, StringWriter writer, string variableName)
        {
            context.AddUsing("Microsoft.AspNetCore.Http"); // HttpContext

            var distinctParameters = action.Parameters
                                           .Where(x => x.ParameterLocation is not ActionParameterLocation.NonUser and not ActionParameterLocation.Body)
                                           .DistinctBy(x => x.ApiParameterName)
                                           .Select(x => (
                                                 externalName: context.NormalizeApiParameterName(x.ApiParameterName)
                                               , internalName: x.InternalParameterName
                                               , typeName: ResolveDelegateParameterTypeName(context, x.Type)
                                               , location: x.ParameterLocation
                                               , hasExplicitMapping: x.ParameterSource != null
                                               , defaultValue: x.DefaultValue
                                               , udtSchema: x.Type.IsUserDefinedType(context.SchemaRegistry, out UserDefinedTypeSchema userDefinedTypeSchema) ? userDefinedTypeSchema : null
                                            ))
                                           .ToList();

            context.AddUsing<CancellationToken>();
            (string externalName, string internalName, string typeName, ActionParameterLocation location, bool hasExplicitMapping, ValueReference defaultValue, UserDefinedTypeSchema udtSchema) cancellationToken = (externalName: "cancellationToken", internalName: "cancellationToken", "CancellationToken", location: ActionParameterLocation.NonUser, hasExplicitMapping: false, defaultValue: null, udtSchema: null);
            distinctParameters.Add(cancellationToken);

            TypeReference bodyType = action.RequestBody?.Contract;
            if (bodyType != null)
                distinctParameters.Insert(0, (externalName: "body", internalName: HttpParameterName.Body, ResolveDelegateParameterTypeName(context, bodyType), location: ActionParameterLocation.Body, hasExplicitMapping: false, defaultValue: null, udtSchema: null));
            
            writer.Write($"{variableName}.RegisterDelegate((HttpContext httpContext, IHttpActionDelegator actionDelegator");

            foreach ((string externalName, _, string typeName, ActionParameterLocation location, _, ValueReference defaultValue, UserDefinedTypeSchema _) in distinctParameters.OrderBy(x => x.defaultValue != null))
            {
                string annotation = null;

                if (location == ActionParameterLocation.Header)
                {
                    context.AddUsing("Microsoft.AspNetCore.Mvc");
                    annotation = "[FromHeader] ";
                }

                string defaultValueSuffix = defaultValue != null ? $" = {context.BuildDefaultValueLiteral(defaultValue).AsString()}" : null;
                writer.WriteRaw($", {annotation}{typeName} {externalName}{defaultValueSuffix}");
            }

            context.AddUsing<Dictionary<string, object>>();
            writer.WriteRaw(") => actionDelegator.Delegate(httpContext, new Dictionary<string, object>");

            var arguments = distinctParameters.Where(x => x != cancellationToken || action.Target.IsAsync)
                                              .Select(x =>
                                              {
                                                  (string externalName, string internalName, _, _, bool hasExplicitMapping, _, UserDefinedTypeSchema udtSchema) = x;
                                                  string argumentName = hasExplicitMapping ? externalName : internalName;
                                                  string value = CollectDelegateParameterValue(context, externalName, udtSchema);
                                                  return (argumentName, value);
                                              })
                                              .ToList();

            if (!arguments.Any())
                writer.WriteRaw("()");
            else
            {
                writer.WriteLine()
                      .WriteLine("{")
                      .PushIndent();

                for (var i = 0; i < arguments.Count; i++)
                {
                    (string argumentName, string value) = arguments[i];
                    writer.Write($"{{ \"{argumentName}\", {value} }}");

                    if (i + 1 < arguments.Count)
                        writer.WriteRaw(",");

                    writer.WriteLine();
                }

                writer.PopIndent()
                      .Write("}");
            }

            writer.WriteLineRaw(", cancellationToken));");
        }

        private static string CollectDelegateParameterValue(CodeGenerationContext context, string externalName, UserDefinedTypeSchema userDefinedTypeSchema)
        {
            if (userDefinedTypeSchema == null)
                return externalName;

            string udtFactory = $"{userDefinedTypeSchema.FullName}.From({externalName}, (set, item) => set.Add(item))";
            return udtFactory;
        }

        private static string ResolveDelegateParameterTypeName(CodeGenerationContext context, TypeReference type)
        {
            if (!type.IsUserDefinedType(context.SchemaRegistry, out UserDefinedTypeSchema userDefinedTypeSchema)) 
                return context.ResolveTypeName(type);

            string arrayTypeName = context.ResolveTypeName(userDefinedTypeSchema.Properties[0].Type);
            return $"{arrayTypeName}[]";
        }

        private static void WriteAuthorizationBehavior(CodeGenerationContext context, StringWriter writer, AuthorizationBehavior authorization, string variableName) { }

        private static void WriteParameter(CodeGenerationContext context, StringWriter writer, string parameterName, ActionParameterSource value, string variableName)
        {
            switch (value)
            {
                case ActionParameterConstantSource constant:
                    WriteConstantParameter(context, writer, parameterName, variableName, constant.Value);
                    break;

                case ActionParameterBodySource body:
                    writer.WriteLine($"{variableName}.ResolveParameterFromBody(\"{parameterName}\", \"{body.ConverterName}\");");
                    break;

                case ActionParameterClaimSource claim:
                    writer.WriteLine($"{variableName}.ResolveParameterFromClaim(\"{parameterName}\", \"{claim.ClaimType}\");");
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
                            WriteParameter(context, writer, parameterSource.ParameterName, parameterSource.Source, itemSourceSelectorVariable);
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

        private static void WriteConstantParameter(CodeGenerationContext context, StringWriter writer, string parameterName, string variableName, ValueReference value)
        {
            switch (value)
            {
                case NullValueReference nullValueReference:
                    writer.WriteLine($"{variableName}.ResolveParameterFromNull<{context.ResolveTypeName(nullValueReference.Type)}>(\"{parameterName}\");");
                    break;
                
                case EnumMemberStringReference enumMemberStringReference:
                    WriteConstantParameter(writer, parameterName, variableName, $"{enumMemberStringReference.Type.Key}.{enumMemberStringReference.Value}");
                    break;
                
                case EnumMemberNumericReference enumMemberNumericReference:
                    WriteConstantParameter(context, writer, parameterName, variableName, enumMemberNumericReference.Value);
                    break;
                
                case PrimitiveValueReference primitiveValueReference:
                    WriteConstantParameter(context, writer, parameterName, variableName, primitiveValueReference.Value);
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(value));
            }
        }
        private static void WriteConstantParameter(CodeGenerationContext context, StringWriter writer, string parameterName, string variableName, object value)
        {
            string constantLiteral = ComputeConstantLiteral(context, value);
            WriteConstantParameter(writer, parameterName,variableName, constantLiteral);
        }
        private static void WriteConstantParameter(StringWriter writer, string parameterName, string variableName, string value)
        {
            writer.WriteLine($"{variableName}.ResolveParameterFromConstant(\"{parameterName}\", {value});");
        }
        
        private static string ComputeConstantLiteral(CodeGenerationContext context, object value)
        {
            switch (value)
            {
                case bool:
                    return $"{value}".ToLowerInvariant();

                case string:
                    return $"\"{value}\"";

                case DateTime dateTimeValue:
                    context.AddUsing<DateTime>();
                    return $"new {nameof(DateTime)}({dateTimeValue.Ticks}, {nameof(DateTimeKind)}.{dateTimeValue.Kind})";

                case DateTimeOffset dateTimeOffsetValue:
                    context.AddUsing<DateTimeOffset>();
                    return $"new {nameof(DateTimeOffset)}({dateTimeOffsetValue.Ticks}, {nameof(TimeSpan)}.{nameof(TimeSpan.FromTicks)}({dateTimeOffsetValue.Offset.Ticks}))";

                case Uri:
                    context.AddUsing<Uri>();
                    return $"new {nameof(Uri)}(\"{value}\")";

                case Guid:
                    context.AddUsing<Guid>();
                    return $"new {nameof(Guid)}(\"{value}\")";

                case byte:
                    return $"(byte){value}";

                case short:
                    return $"(short){value}";

                case long:
                    return $"{value}L";

                case float:
                    return $"{value}f";

                case decimal:
                    return $"{value}m";

                case int:
                case double:
                    return $"{value}";

                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }
        #endregion
    }
}