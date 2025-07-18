﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using Dibix.Http;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ApiDescriptionWriter : ArtifactWriterBase
    {
        #region Fields
        private readonly ActionCompatibilityLevel _compatibilityLevel;
        private readonly IList<ControllerEntry> _controllers;
        #endregion

        #region Properties
        public override string LayerName => CodeGeneration.LayerName.Business;
        public override string RegionName => "Endpoints";
        #endregion

        #region Constructor
        public ApiDescriptionWriter(CodeGenerationModel model, ActionCompatibilityLevel compatibilityLevel)
        {
            _compatibilityLevel = compatibilityLevel;
            _controllers = model.Controllers
                                .SelectMany(x => x.Actions, (x, y) => new
                                {
                                    Controller = x,
                                    Action = y
                                })
                                .Where(x => x.Action.CompatibilityLevel == compatibilityLevel)
                                .GroupBy(x => x.Controller)
                                .Select(x => new ControllerEntry(x.Key, x.Select(y => y.Action).ToArray()))
                                .ToArray();
        }
        #endregion

        #region Overrides
        public override bool HasContent(CodeGenerationModel model) => _controllers.Any();

        public override IEnumerable<CSharpAnnotation> GetGlobalAnnotations(CodeGenerationModel model)
        {
            if (_compatibilityLevel != ActionCompatibilityLevel.Reflection)
                yield break;

            string areaName = PathUtility.EnsureAreaName(model.AreaName);
            yield return new CSharpAnnotation("AreaRegistration", new CSharpStringValue(areaName));
        }

        public override void Write(CodeGenerationContext context)
        {
            context.AddUsing("Dibix.Http.Server");

            string body = this.WriteBody(context);

            context.CreateOutputScope()
                   .AddClass("ApiConfiguration", CSharpModifiers.Public | CSharpModifiers.Sealed)
                   .Inherits("HttpApiDescriptor")
                   .AddMethod("Configure", "void", body, modifiers: CSharpModifiers.Public | CSharpModifiers.Override)
                   .AddParameter("context", "IHttpApiDiscoveryContext");
        }
        #endregion

        #region Private Methods
        private string WriteBody(CodeGenerationContext context)
        {
            StringWriter writer = new StringWriter();
            for (int i = 0; i < _controllers.Count; i++)
            {
                ControllerEntry controllerEntry = _controllers[i];
                ControllerDefinition controller = controllerEntry.Controller;
                IList<ActionDefinition> actions = controllerEntry.Actions;
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

                if (i + 1 < _controllers.Count)
                    writer.WriteLine();
            }

            return writer.ToString();
        }

        private void WriteAddAction(CodeGenerationContext context, StringWriter writer, ActionDefinition action)
        {
            if (_compatibilityLevel == ActionCompatibilityLevel.Native)
                context.AddUsing("Dibix.Http.Server.AspNetCore");
            else if (_compatibilityLevel == ActionCompatibilityLevel.Reflection)
                context.AddUsing("Dibix.Http.Server.AspNet");

            writer.Write("controller.AddAction(");
            WriteActionTarget(context, writer, action, "action", WriteActionConfiguration);
        }

        private void WriteActionTarget<T>(CodeGenerationContext context, StringWriter writer, T actionTargetDefinition, string variableName, Action<CodeGenerationContext, StringWriter, T, string> body) where T : ActionTargetDefinition
        {
            writer.WriteRaw(actionTargetDefinition.Target is ReflectionActionTarget ? "External" : "Local");
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

                // Native endpoints have all dependencies embedded, whereas legacy endpoints reference them via external assembly
                if (_compatibilityLevel != ActionCompatibilityLevel.Native && actionTargetDefinition.Target is LocalActionTarget localActionTarget)
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
            writer.WriteLine($"{variableName}.ActionName = \"{action.OperationId}\";");

            if (!String.IsNullOrEmpty(action.Target.RelativeNamespace))
                writer.WriteLine($"{variableName}.RelativeNamespace = \"{action.Target.RelativeNamespace}\";");

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
                writer.WriteLine($"{variableName}.SecuritySchemes.Add(\"{securitySchemeRequirement.Scheme.SchemeName}\");");
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

            foreach (AuthorizationBehavior authorizationBehavior in action.Authorization)
            {
                writer.Write($"{variableName}.AddAuthorizationBehavior(");
                WriteActionTarget(context, writer, authorizationBehavior, "authorization", WriteAuthorizationBehavior);
            }

            if (_compatibilityLevel == ActionCompatibilityLevel.Native)
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
                    writer.Write($"{variableName}.ResolveParameterFromSource(\"{parameterName}\", \"{property.Definition.Name}\", \"{property.PropertyPath}\"");

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

                case EnumMemberReference enumMemberReference:
                    WriteConstantParameter(context, writer, parameterName, variableName, CollectEnumConstantValue(enumMemberReference, enumMemberReference.Kind));
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

        private static object CollectEnumConstantValue(EnumMemberReference enumMemberReference, EnumMemberReferenceKind kind) => kind switch
        {
            EnumMemberReferenceKind.Value => enumMemberReference.Member.ActualValue,
            EnumMemberReferenceKind.Name => enumMemberReference,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

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

                case EnumMemberReference enumMemberReference:
                    return $"{enumMemberReference.Type.Key}.{enumMemberReference.Member.Name}";

                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }

        private static IEnumerable<KeyValuePair<ControllerDefinition, ActionDefinition>> CollectActionsToInclude(CodeGenerationModel model)
        {
            foreach (ControllerDefinition controller in model.Controllers)
            {
                foreach (ActionDefinition action in controller.Actions)
                {
                    if (action.CompatibilityLevel != ActionCompatibilityLevel.Native)
                        continue;

                    yield return new KeyValuePair<ControllerDefinition, ActionDefinition>(controller, action);
                }
            }
        }
        #endregion

        #region Nested Types
        private readonly struct ControllerEntry
        {
            public ControllerDefinition Controller { get; }
            public IList<ActionDefinition> Actions { get; }

            public ControllerEntry(ControllerDefinition controller, IList<ActionDefinition> actions)
            {
                Controller = controller;
                Actions = actions;
            }
        }
        #endregion
    }
}