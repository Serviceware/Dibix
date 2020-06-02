using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Dibix.Http;
using Dibix.Sdk.CodeGeneration;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace Dibix.Sdk.OpenApi
{
    internal static class OpenApiGenerator
    {
        private static bool _useRelativeNamespaces = false;
        private static readonly IDictionary<PrimitiveDataType, Func<OpenApiSchema>> PrimitiveTypeMap = new Dictionary<PrimitiveDataType, Func<OpenApiSchema>>
        {
            [PrimitiveDataType.Boolean]        = () => new OpenApiSchema { Type = "boolean"                       }
          , [PrimitiveDataType.Byte]           = () => new OpenApiSchema { Type = "integer", Format = "int32"     }
          , [PrimitiveDataType.Int16]          = () => new OpenApiSchema { Type = "integer", Format = "int32"     }
          , [PrimitiveDataType.Int32]          = () => new OpenApiSchema { Type = "integer", Format = "int32"     }
          , [PrimitiveDataType.Int64]          = () => new OpenApiSchema { Type = "integer", Format = "int64"     }
          , [PrimitiveDataType.Float]          = () => new OpenApiSchema { Type = "number",  Format = "float"     }
          , [PrimitiveDataType.Double]         = () => new OpenApiSchema { Type = "number",  Format = "double"    }
          , [PrimitiveDataType.Decimal]        = () => new OpenApiSchema { Type = "number",  Format = "double"    }
          , [PrimitiveDataType.Binary]         = () => new OpenApiSchema { Type = "string",  Format = "byte"      }
          , [PrimitiveDataType.DateTime]       = () => new OpenApiSchema { Type = "string",  Format = "date-time" }
          , [PrimitiveDataType.DateTimeOffset] = () => new OpenApiSchema { Type = "string",  Format = "date-time" }
          , [PrimitiveDataType.String]         = () => new OpenApiSchema { Type = "string"                        }
          , [PrimitiveDataType.UUID]           = () => new OpenApiSchema { Type = "string",  Format = "uuid"      }
        };

        public static OpenApiDocument Generate
        (
            string projectVersion
          , string productName
          , string areaName
          , string rootNamespace
          , IEnumerable<ControllerDefinition> controllers
          , ISchemaRegistry schemaRegistry
        )
        {
            OpenApiDocument document = new OpenApiDocument
            {
                Info = new OpenApiInfo
                {
                    Title = $"{productName} {areaName}",
                    Version = !String.IsNullOrEmpty(projectVersion) ? projectVersion : "1.0.0"
                }
            };
            AppendPaths(document, areaName, controllers, rootNamespace, schemaRegistry);
            return document;
        }

        private static void AppendPaths(OpenApiDocument document, string areaName, IEnumerable<ControllerDefinition> controllers, string rootNamespace, ISchemaRegistry schemaRegistry)
        {
            foreach (ControllerDefinition controller in controllers)
            {
                IDictionary<ActionDefinition, string> operationIds = controller.Actions
                                                                               .GroupBy(x => x.Target.Name)
                                                                               .SelectMany(x => x.Select((y, i) => new
                                                                               {
                                                                                   Position = i + 1,
                                                                                   Name = x.Key,
                                                                                   Action = y,
                                                                                   IsAmbigous = x.Count() > 1
                                                                               }))
                                                                               .ToDictionary(x => x.Action, x => x.IsAmbigous ? $"{x.Name}{x.Position}" : x.Name);

                foreach (IGrouping<string, ActionDefinition> path in controller.Actions.GroupBy(x => $"/{RouteBuilder.BuildRoute(areaName, controller.Name, x.ChildRoute)}"))
                {
                    OpenApiPathItem value = new OpenApiPathItem();

                    foreach (ActionDefinition action in path)
                    {
                        OperationType operationType = (OperationType)Enum.Parse(typeof(OperationType), action.Method.ToString());

                        OpenApiOperation operation = new OpenApiOperation();
                        operation.Tags.Add(new OpenApiTag { Name = $"{areaName}/{controller.Name}" });
                        operation.Summary = action.Description ?? "Undocumented action";
                        operation.OperationId = operationIds[action];

                        AppendParameters(document, operation, action, rootNamespace, schemaRegistry);
                        AppendBody(document, operation, action, rootNamespace, schemaRegistry);
                        AppendResponses(document, operation, action, rootNamespace, schemaRegistry);

                        value.AddOperation(operationType, operation);
                    }

                    if (document.Paths == null)
                        document.Paths = new OpenApiPaths();

                    document.Paths.Add(path.Key, value);
                }
            }
        }

        private static void AppendParameters(OpenApiDocument document, OpenApiOperation operation, ActionDefinition action, string rootNamespace, ISchemaRegistry schemaRegistry)
        {
            ICollection<string> bodyProperties = GetBodyProperties(action.BodyContract, schemaRegistry);
            IDictionary<string, ActionParameter> actionParameters = GetActionParameters(action.Target);
            HashSet<string> visitedActionParameters = new HashSet<string>();
            if (!String.IsNullOrEmpty(action.ChildRoute))
            {
                IEnumerable<string> pathParameters = Regex.Matches(action.ChildRoute, @"\{(?<parameter>[^}]+)\}")
                                                          .Cast<Match>()
                                                          .Select(x => x.Groups["parameter"].Value);

                foreach (string pathParameter in pathParameters)
                {
                    if (!actionParameters.TryGetValue(pathParameter, out ActionParameter actionParameter)) 
                        continue;

                    AppendPathParameter(document, operation, actionParameter, pathParameter, rootNamespace, schemaRegistry);
                    visitedActionParameters.Add(actionParameter.Name);
                }
            }

            foreach (ActionParameter actionParameter in actionParameters.Values)
            {
                if (visitedActionParameters.Contains(actionParameter.Name)     // Path segment
                 || action.ParameterSources.ContainsKey(actionParameter.Name)  // Constant/Internal value
                 || bodyProperties.Contains(actionParameter.Name))             // Body property
                    continue;

                AppendParameter(document, operation, actionParameter, actionParameter.Type, rootNamespace, schemaRegistry);
            }
        }

        private static ICollection<string> GetBodyProperties(TypeReference bodyContract, ISchemaRegistry schemaRegistry)
        {
            HashSet<string> properties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (bodyContract is SchemaTypeReference schemaTypeReference)
            {
                SchemaDefinition schema = schemaRegistry.GetSchema(schemaTypeReference);
                if (schema is ObjectSchema objectSchema)
                    properties.AddRange(objectSchema.Properties.Select(x => x.Name));
            }
            return properties;
        }

        private static void AppendParameter(OpenApiDocument document, OpenApiOperation operation, ActionParameter actionParameter, TypeReference parameterType, string rootNamespace, ISchemaRegistry schemaRegistry)
        {
            switch (parameterType)
            {
                case PrimitiveTypeReference _:
                    AppendQueryParameter(document, operation, actionParameter, rootNamespace, schemaRegistry);
                    break;

                case SchemaTypeReference schemaTypeReference:
                    SchemaDefinition schema = schemaRegistry.GetSchema(schemaTypeReference);
                    AppendComplexParameter(document, operation, actionParameter, schema, rootNamespace, schemaRegistry);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(parameterType), parameterType, null);
            }
        }

        private static void AppendPathParameter(OpenApiDocument document, OpenApiOperation operation, ActionParameter actionParameter, string parameterName, string rootNamespace, ISchemaRegistry schemaRegistry)
        {
            AppendParameter(document, operation, actionParameter, parameterName, ParameterLocation.Path, true, rootNamespace, schemaRegistry);
        }

        private static void AppendQueryParameter(OpenApiDocument document, OpenApiOperation operation, ActionParameter actionParameter, string rootNamespace, ISchemaRegistry schemaRegistry) => AppendQueryParameter(document, operation, actionParameter, actionParameter.Type, actionParameter.Type.IsEnumerable, rootNamespace, schemaRegistry);
        private static OpenApiParameter AppendQueryParameter(OpenApiDocument document, OpenApiOperation operation, ActionParameter actionParameter, TypeReference parameterType, bool isEnumerable, string rootNamespace, ISchemaRegistry schemaRegistry)
        {
            bool isRequired = !actionParameter.HasDefaultValue && !actionParameter.Type.IsNullable;
            return AppendParameter(document, operation, actionParameter, actionParameter.Name, parameterType, isEnumerable, ParameterLocation.Query, isRequired, rootNamespace, schemaRegistry);
        }

        private static void AppendComplexParameter(OpenApiDocument document, OpenApiOperation operation, ActionParameter actionParameter, SchemaDefinition parameterSchema, string rootNamespace, ISchemaRegistry schemaRegistry)
        {
            switch (parameterSchema)
            {
                case EnumSchema _:
                    AppendQueryParameter(document, operation, actionParameter, rootNamespace, schemaRegistry);
                    break;

                case UserDefinedTypeSchema userDefinedTypeSchema:
                    AppendQueryArrayParameter(document, operation, actionParameter, userDefinedTypeSchema, rootNamespace, schemaRegistry);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(parameterSchema), parameterSchema, null);
            }
        }

        private static void AppendQueryArrayParameter(OpenApiDocument document, OpenApiOperation operation, ActionParameter actionParameter, UserDefinedTypeSchema parameterSchema, string rootNamespace, ISchemaRegistry schemaRegistry)
        {
            TypeReference parameterType = parameterSchema.Properties.Count > 1 ? actionParameter.Type : parameterSchema.Properties[0].Type;
            OpenApiParameter result = AppendQueryParameter(document, operation, actionParameter, parameterType, true, rootNamespace, schemaRegistry);
            result.Explode = true;
            result.Style = parameterSchema.Properties.Count > 1 ? ParameterStyle.DeepObject : ParameterStyle.Form;
        }

        private static void AppendParameter(OpenApiDocument document, OpenApiOperation operation, ActionParameter actionParameter, string parameterName, ParameterLocation parameterLocation, bool isRequired, string rootNamespace, ISchemaRegistry schemaRegistry) => AppendParameter(document, operation, actionParameter, parameterName, actionParameter.Type, actionParameter.Type.IsEnumerable, parameterLocation, isRequired, rootNamespace, schemaRegistry);
        private static OpenApiParameter AppendParameter(OpenApiDocument document, OpenApiOperation operation, ActionParameter actionParameter, string parameterName, TypeReference parameterType, bool isEnumerable, ParameterLocation parameterLocation, bool isRequired, string rootNamespace, ISchemaRegistry schemaRegistry)
        {
            OpenApiParameter apiParameter = new OpenApiParameter
            {
                In = parameterLocation,
                Required = isRequired,
                Name = parameterName,
                Schema = CreateSchema(document, parameterType, isEnumerable, actionParameter.HasDefaultValue, actionParameter.DefaultValue, rootNamespace, schemaRegistry)
            };
            operation.Parameters.Add(apiParameter);
            return apiParameter;
        }

        private static IDictionary<string, ActionParameter> GetActionParameters(ActionDefinitionTarget actionTarget)
        {
            switch (actionTarget)
            {
                case GeneratedAccessorMethodTarget generatedAccessorActionTarget: return generatedAccessorActionTarget.Parameters;

                //case ReflectionActionTarget reflectionActionTarget:
                
                default: return new Dictionary<string, ActionParameter>(); //throw new ArgumentOutOfRangeException(nameof(actionTarget), actionTarget, "Unsupported action target for Open API response");
            }
        }

        private static void AppendBody(OpenApiDocument document, OpenApiOperation operation, ActionDefinition action, string rootNamespace, ISchemaRegistry schemaRegistry)
        {
            if (action.BodyContract == null)
                return;

            OpenApiRequestBody body = new OpenApiRequestBody { Required = true };
            AppendContent(document, body.Content, action.BodyContract, rootNamespace, schemaRegistry);
            operation.RequestBody = body;
        }

        private static void AppendResponses(OpenApiDocument document, OpenApiOperation operation, ActionDefinition action, string rootNamespace, ISchemaRegistry schemaRegistry)
        {
            TypeReference resultType = GetResultType(action.Target);
            AppendDefaultResponse(document, operation, resultType, rootNamespace, schemaRegistry);
            AppendErrorResponses(document, operation, action, rootNamespace, schemaRegistry);
        }

        private static void AppendDefaultResponse(OpenApiDocument document, OpenApiOperation operation, TypeReference typeReference, string rootNamespace, ISchemaRegistry schemaRegistry)
        {
            HttpStatusCode statusCode = typeReference != null ? HttpStatusCode.OK : HttpStatusCode.NoContent;
            operation.Responses.Add(((int)statusCode).ToString(), CreateResponse(document, statusCode, typeReference, rootNamespace, schemaRegistry));
        }

        private static void AppendErrorResponses(OpenApiDocument document, OpenApiOperation operation, ActionDefinition action, string rootNamespace, ISchemaRegistry schemaRegistry)
        {
            if (!(action.Target is GeneratedAccessorMethodTarget generatedAccessorActionTarget))
                return;
            
            foreach (var errorResponseGroup in generatedAccessorActionTarget.ErrorResponses.GroupBy(x => new { x.StatusCode, x.IsClientError }))
            {
                AppendErrorResponse(document, operation, errorResponseGroup.Key.StatusCode, errorResponseGroup.Key.IsClientError, errorResponseGroup.ToArray(), rootNamespace, schemaRegistry);
            }
        }

        private static void AppendErrorResponse(OpenApiDocument document, OpenApiOperation operation, int statusCode, bool isClientError, ICollection<ErrorResponse> errorResponses, string rootNamespace, ISchemaRegistry schemaRegistry)
        {
            OpenApiResponse response = CreateResponse(document, (HttpStatusCode)statusCode, null, rootNamespace, schemaRegistry);

            if (isClientError)
            {
                response.Description = $@"{HttpErrorResponseParser.ClientErrorCodeHeaderName}|{HttpErrorResponseParser.ClientErrorDescriptionHeaderName}
-|-
{String.Join(Environment.NewLine, errorResponses.Select(x => $"{(x.ErrorCode != 0 ? x.ErrorCode.ToString() : "n/a")}|{x.ErrorDescription}"))}";

                if (errorResponses.Any(x => x.ErrorCode != 0))
                {
                    response.Headers.Add(HttpErrorResponseParser.ClientErrorCodeHeaderName, new OpenApiHeader
                    {
                        Description = "Additional error code to handle the error on the client",
                        Schema = PrimitiveTypeMap[PrimitiveDataType.Int16]()
                    });
                }

                if (errorResponses.Any(x => !String.IsNullOrEmpty(x.ErrorDescription)))
                {
                    response.Headers.Add(HttpErrorResponseParser.ClientErrorDescriptionHeaderName, new OpenApiHeader
                    {
                        Description = "A mesage describing the cause of the error",
                        Schema = PrimitiveTypeMap[PrimitiveDataType.String]()
                    });
                    const string mimeType = "text/plain";
                    response.Content.Add(mimeType, new OpenApiMediaType { Schema = PrimitiveTypeMap[PrimitiveDataType.String]() });
                }
            }

            operation.Responses.Add(statusCode.ToString(), response);
        }

        private static OpenApiResponse CreateResponse(OpenApiDocument document, HttpStatusCode statusCode, TypeReference typeReference, string rootNamespace, ISchemaRegistry schemaRegistry)
        {
            OpenApiResponse response = new OpenApiResponse { Description = statusCode.ToString() };
            if (typeReference != null)
                AppendContent(document, response.Content, typeReference, rootNamespace, schemaRegistry);

            return response;
        }

        private static void AppendContent(OpenApiDocument document, IDictionary<string, OpenApiMediaType> target, TypeReference typeReference, string rootNamespace, ISchemaRegistry schemaRegistry)
        {
            const string mediaType = "application/json";
            target.Add(mediaType, new OpenApiMediaType { Schema = CreateSchema(document, typeReference, rootNamespace, schemaRegistry) });
        }

        private static TypeReference GetResultType(ActionDefinitionTarget actionTarget)
        {
            switch (actionTarget)
            {
                case GeneratedAccessorMethodTarget generatedAccessorActionTarget: return generatedAccessorActionTarget.ResultType;

                //case ReflectionActionTarget reflectionActionTarget:

                default: return null; //throw new ArgumentOutOfRangeException(nameof(actionTarget), actionTarget, "Unsupported action target for Open API response");
            }
        }

        private static OpenApiSchema CreateSchema(OpenApiDocument document, TypeReference typeReference, string rootNamespace, ISchemaRegistry schemaRegistry) => CreateSchema(document, typeReference, typeReference.IsEnumerable, false, null, rootNamespace, schemaRegistry);
        private static OpenApiSchema CreateSchema(OpenApiDocument document, TypeReference typeReference, bool isEnumerable, bool hasDefaultValue, object defaultValue, string rootNamespace, ISchemaRegistry schemaRegistry)
        {
            OpenApiSchema schema = CreateSchemaCore(document, typeReference, rootNamespace, schemaRegistry);
            schema.Nullable = typeReference.IsNullable;

            if (hasDefaultValue)
                schema.Default = CreateDefaultValue(defaultValue);

            if (isEnumerable)
            {
                schema = new OpenApiSchema
                {
                    Type = "array",
                    Items = schema
                };
            }

            return schema;
        }

        private static OpenApiSchema CreateSchemaCore(OpenApiDocument document, TypeReference typeReference, string rootNamespace, ISchemaRegistry schemaRegistry)
        {
            switch (typeReference)
            {
                case PrimitiveTypeReference primitiveContractPropertyType: return CreatePrimitiveTypeSchema(primitiveContractPropertyType);
                case SchemaTypeReference contractPropertyTypeReference: return CreateReferenceSchema(document, contractPropertyTypeReference, rootNamespace, schemaRegistry);
                default: throw new ArgumentOutOfRangeException(nameof(typeReference), typeReference, $"Unexpected property type: {typeReference}");
            }
        }

        private static OpenApiSchema CreatePrimitiveTypeSchema(PrimitiveTypeReference typeReference)
        {
            if (!PrimitiveTypeMap.TryGetValue(typeReference.Type, out Func<OpenApiSchema> schemaFactory))
                throw new InvalidOperationException($"Unexpected primitive type: {typeReference.Type}");

            return schemaFactory();
        }

        private static OpenApiSchema CreateReferenceSchema(OpenApiDocument document, SchemaTypeReference typeReference, string rootNamespace, ISchemaRegistry schemaRegistry)
        {
            SchemaDefinition schema = schemaRegistry.GetSchema(typeReference);
            string typeName;

            if (_useRelativeNamespaces)
            {
                typeName = schema.DefinitionName;
                string relativeNamespace = NamespaceUtility.BuildRelativeNamespace(rootNamespace, LayerName.DomainModel, schema.Namespace);
                if (!String.IsNullOrEmpty(relativeNamespace))
                    typeName = $"{relativeNamespace}.{typeName}";
            }
            else
                typeName = schema.FullName;

            EnsureSchema(document, typeName, schema, rootNamespace, schemaRegistry);

            return new OpenApiSchema
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.Schema,
                    Id = typeName
                }
            };
        }

        private static void EnsureSchema(OpenApiDocument document, string schemaName, SchemaDefinition contract, string rootNamespace, ISchemaRegistry schemaRegistry)
        {
            if (document.Components == null)
                document.Components = new OpenApiComponents();

            if (!document.Components.Schemas.ContainsKey(schemaName))
                AppendContractSchema(document, schemaName, contract, rootNamespace, schemaRegistry);
        }

        private static void AppendContractSchema(OpenApiDocument document, string schemaName, SchemaDefinition contract, string rootNamespace, ISchemaRegistry schemaRegistry)
        {
            switch (contract)
            {
                case ObjectSchema objectContract:
                    AppendObjectSchema(document, schemaName, objectContract, rootNamespace, schemaRegistry);
                    break;

                case EnumSchema enumContract:
                    AppendEnumSchema(document, schemaName, enumContract);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(contract), contract, "Unexpected contract definition");
            }
        }

        private static void AppendObjectSchema(OpenApiDocument document, string schemaName, ObjectSchema objectContract, string rootNamespace, ISchemaRegistry schemaRegistry)
        {
            OpenApiSchema schema = new OpenApiSchema
            {
                Type = "object",
                AdditionalPropertiesAllowed = false
            };

            document.Components.Schemas.Add(schemaName, schema); // Register schema before traversing properties to avoid endless recursions for self referencing properties

            foreach (ObjectSchemaProperty property in objectContract.Properties)
            {
                if (property.SerializationBehavior == SerializationBehavior.Never)
                    continue;

                OpenApiSchema propertySchema = CreateSchema(document, property.Type, rootNamespace, schemaRegistry);
                schema.Properties.Add(property.Name, propertySchema);
            }
        }

        private static void AppendEnumSchema(OpenApiDocument document, string schemaName, EnumSchema enumContract)
        {
            OpenApiSchema schema = PrimitiveTypeMap[PrimitiveDataType.Int32]();
            OpenApiArray enumNames = new OpenApiArray();
            schema.Extensions.Add("x-enum-varnames", enumNames);
            foreach (EnumSchemaMember member in enumContract.Members)
            {
                schema.Enum.Add(new OpenApiInteger(member.ActualValue));
                enumNames.Add(new OpenApiString(member.Name));
            }

            document.Components.Schemas.Add(schemaName, schema);
        }

        private static IOpenApiAny CreateDefaultValue(object defaultValue)
        {
            switch (defaultValue)
            {
                case bool boolValue: return new OpenApiBoolean(boolValue);
                case int intValue: return new OpenApiInteger(intValue);
                case string stringValue: return new OpenApiString(stringValue);
                case null: return new OpenApiNull();
                default: throw new ArgumentOutOfRangeException(nameof(defaultValue), defaultValue, null);
            }
        }
    }
}