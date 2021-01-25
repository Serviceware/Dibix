using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Dibix.Http;
using Dibix.Sdk.CodeGeneration;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace Dibix.Sdk.OpenApi
{
    internal static class OpenApiGenerator
    {
        private static bool _useRelativeNamespaces = true;
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
            string title
          , string version
          , string description
          , string baseUrl
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
                    Title = title,
                    Version = !String.IsNullOrEmpty(version) ? version : "1.0.0",
                    Description = description
                }
            };
            
            if (!String.IsNullOrEmpty(baseUrl))
                document.Servers.Add(new OpenApiServer { Url = baseUrl });

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
                        string operationId = operationIds[action];

                        OpenApiOperation operation = new OpenApiOperation();
                        operation.Tags.Add(new OpenApiTag { Name = controller.Name });
                        operation.Summary = action.Description ?? operationId;
                        operation.OperationId = operationId;

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
            foreach (ActionParameter parameter in action.Parameters.DistinctBy(x => x.ApiParameterName))
            {
                if (parameter.Location != ActionParameterLocation.Query && parameter.Location != ActionParameterLocation.Path) 
                    continue;

                AppendUserParameter(document, operation, parameter, parameter.Location, rootNamespace, schemaRegistry);
            }
        }

        private static void AppendUserParameter(OpenApiDocument document, OpenApiOperation operation, ActionParameter parameter, ActionParameterLocation location, string rootNamespace, ISchemaRegistry schemaRegistry)
        {
            switch (location)
            {
                case ActionParameterLocation.Query: 
                    AppendQueryParameter(document, operation, parameter, parameter.Type, rootNamespace, schemaRegistry);
                    break;
                
                case ActionParameterLocation.Path: 
                    AppendPathParameter(document, operation, parameter, rootNamespace, schemaRegistry);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(location), location, null);
            }
        }

        private static void AppendQueryParameter(OpenApiDocument document, OpenApiOperation operation, ActionParameter parameter, TypeReference parameterType, string rootNamespace, ISchemaRegistry schemaRegistry)
        {
            switch (parameterType)
            {
                case PrimitiveTypeReference _:
                    AppendQueryParameter(document, operation, parameter, rootNamespace, schemaRegistry);
                    break;

                case SchemaTypeReference schemaTypeReference:
                    SchemaDefinition schema = schemaRegistry.GetSchema(schemaTypeReference);
                    AppendComplexQueryParameter(document, operation, parameter, schema, rootNamespace, schemaRegistry);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(parameterType), parameterType, null);
            }
        }

        private static void AppendPathParameter(OpenApiDocument document, OpenApiOperation operation, ActionParameter parameter, string rootNamespace, ISchemaRegistry schemaRegistry)
        {
            AppendParameter(document, operation, parameter, ParameterLocation.Path, true, rootNamespace, schemaRegistry);
        }

        private static void AppendQueryParameter(OpenApiDocument document, OpenApiOperation operation, ActionParameter parameter, string rootNamespace, ISchemaRegistry schemaRegistry) => AppendQueryParameter(document, operation, parameter, parameter.Type, parameter.Type.IsEnumerable, rootNamespace, schemaRegistry);
        private static OpenApiParameter AppendQueryParameter(OpenApiDocument document, OpenApiOperation operation, ActionParameter parameter, TypeReference parameterType, bool isEnumerable, string rootNamespace, ISchemaRegistry schemaRegistry)
        {
            bool isRequired = !parameter.HasDefaultValue;
            return AppendParameter(document, operation, parameter, parameterType, isEnumerable, ParameterLocation.Query, isRequired, rootNamespace, schemaRegistry);
        }

        private static void AppendComplexQueryParameter(OpenApiDocument document, OpenApiOperation operation, ActionParameter parameter, SchemaDefinition parameterSchema, string rootNamespace, ISchemaRegistry schemaRegistry)
        {
            switch (parameterSchema)
            {
                case EnumSchema _:
                    AppendQueryParameter(document, operation, parameter, rootNamespace, schemaRegistry);
                    break;

                case UserDefinedTypeSchema userDefinedTypeSchema:
                    AppendQueryArrayParameter(document, operation, parameter, userDefinedTypeSchema, rootNamespace, schemaRegistry);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(parameterSchema), parameterSchema, null);
            }
        }

        private static void AppendQueryArrayParameter(OpenApiDocument document, OpenApiOperation operation, ActionParameter parameter, ObjectSchema parameterSchema, string rootNamespace, ISchemaRegistry schemaRegistry)
        {
            TypeReference parameterType = parameterSchema.Properties.Count > 1 ? parameter.Type : parameterSchema.Properties[0].Type;
            OpenApiParameter result = AppendQueryParameter(document, operation, parameter, parameterType, true, rootNamespace, schemaRegistry);
            result.Explode = true;
            result.Style = parameterSchema.Properties.Count > 1 ? ParameterStyle.DeepObject : ParameterStyle.Form;
        }

        private static void AppendParameter(OpenApiDocument document, OpenApiOperation operation, ActionParameter parameter, ParameterLocation parameterLocation, bool isRequired, string rootNamespace, ISchemaRegistry schemaRegistry) => AppendParameter(document, operation, parameter, parameter.Type, parameter.Type.IsEnumerable, parameterLocation, isRequired, rootNamespace, schemaRegistry);
        private static OpenApiParameter AppendParameter(OpenApiDocument document, OpenApiOperation operation, ActionParameter actionParameter, TypeReference parameterType, bool isEnumerable, ParameterLocation parameterLocation, bool isRequired, string rootNamespace, ISchemaRegistry schemaRegistry)
        {
            OpenApiParameter apiParameter = new OpenApiParameter
            {
                In = parameterLocation,
                Required = isRequired,
                Name = actionParameter.ApiParameterName,
                Schema = CreateSchema(document, parameterType, isEnumerable, actionParameter.HasDefaultValue, actionParameter.DefaultValue, rootNamespace, schemaRegistry)
            };
            operation.Parameters.Add(apiParameter);
            return apiParameter;
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

                if (property.SerializationBehavior == SerializationBehavior.Always)
                    schema.Required.Add(property.Name);
            }
        }

        private static void AppendEnumSchema(OpenApiDocument document, string schemaName, EnumSchema enumContract)
        {
            OpenApiSchema schema = PrimitiveTypeMap[PrimitiveDataType.Int32]();
            OpenApiArray enumNames = new OpenApiArray();

            schema.Description = String.Join("<br/>", enumContract.Members.Select(x => $"{x.ActualValue} = {x.Name}"));
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