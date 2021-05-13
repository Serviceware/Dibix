﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Dibix.Http.Server;
using Dibix.Sdk.CodeGeneration;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using PrimitiveType = Dibix.Sdk.CodeGeneration.PrimitiveType;

namespace Dibix.Sdk.OpenApi
{
    internal static class OpenApiGenerator
    {
        private static readonly bool UseRelativeNamespaces = true;
        private static readonly IDictionary<PrimitiveType, Func<OpenApiSchema>> PrimitiveTypeMap = new Dictionary<PrimitiveType, Func<OpenApiSchema>>
        {
            [PrimitiveType.Boolean]        = () => new OpenApiSchema { Type = "boolean"                       }
          , [PrimitiveType.Byte]           = () => new OpenApiSchema { Type = "integer", Format = "int32"     }
          , [PrimitiveType.Int16]          = () => new OpenApiSchema { Type = "integer", Format = "int32"     }
          , [PrimitiveType.Int32]          = () => new OpenApiSchema { Type = "integer", Format = "int32"     }
          , [PrimitiveType.Int64]          = () => new OpenApiSchema { Type = "integer", Format = "int64"     }
          , [PrimitiveType.Float]          = () => new OpenApiSchema { Type = "number",  Format = "float"     }
          , [PrimitiveType.Double]         = () => new OpenApiSchema { Type = "number",  Format = "double"    }
          , [PrimitiveType.Decimal]        = () => new OpenApiSchema { Type = "number",  Format = "double"    }
          , [PrimitiveType.Binary]         = () => new OpenApiSchema { Type = "string",  Format = "byte"      }
          , [PrimitiveType.Stream]         = () => new OpenApiSchema { Type = "string",  Format = "binary"    }
          , [PrimitiveType.DateTime]       = () => new OpenApiSchema { Type = "string",  Format = "date-time" }
          , [PrimitiveType.DateTimeOffset] = () => new OpenApiSchema { Type = "string",  Format = "date-time" }
          , [PrimitiveType.String]         = () => new OpenApiSchema { Type = "string"                        }
          , [PrimitiveType.UUID]           = () => new OpenApiSchema { Type = "string",  Format = "uuid"      }
        };
        private static readonly string[] ReservedOpenApiHeaders = { "Accept", "Authorization", "Content-Type" };
        private static readonly OpenApiSchema NullSchema = new OpenApiSchema { Type = "null" };

        public static OpenApiDocument Generate(CodeArtifactsGenerationModel model, ISchemaRegistry schemaRegistry)
        {
            OpenApiDocument document = new OpenApiDocument
            {
                Info = new OpenApiInfo
                {
                    Title = model.Title,
                    Version = !String.IsNullOrEmpty(model.Version) ? model.Version : "1.0.0",
                    Description = model.Description
                }
            };
            
            if (!String.IsNullOrEmpty(model.BaseUrl))
                document.Servers.Add(new OpenApiServer { Url = model.BaseUrl });

            AppendSecuritySchemes(document, model.SecuritySchemes);
            AppendPaths(document, model.AreaName, model.Controllers, model.RootNamespace, schemaRegistry);
            return document;
        }

        private static void AppendPaths(OpenApiDocument document, string areaName, ICollection<ControllerDefinition> controllers, string rootNamespace, ISchemaRegistry schemaRegistry)
        {
            IDictionary<ActionDefinition, string> operationIds = controllers.SelectMany(x => x.Actions)
                                                                            .GroupBy(x => x.Target.OperationName)
                                                                            .SelectMany(x => x.Select((y, i) => new
                                                                            {
                                                                                Position = i + 1,
                                                                                Name = x.Key,
                                                                                Action = y,
                                                                                IsAmbiguous = x.Count() > 1
                                                                            }))
                                                                            .ToDictionary(x => x.Action, x => x.IsAmbiguous ? $"{x.Name}{x.Position}" : x.Name);

            foreach (ControllerDefinition controller in controllers)
            {
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
                        AppendSecuritySchemes(document, action, operation);

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
                if (parameter.Location != ActionParameterLocation.Query
                 && parameter.Location != ActionParameterLocation.Path
                 && parameter.Location != ActionParameterLocation.Header) 
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
                
                case ActionParameterLocation.Header:
                    AppendHeaderParameter(document, operation, parameter, rootNamespace, schemaRegistry);
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
            AppendParameter(document, operation, parameter, ParameterLocation.Path, rootNamespace, schemaRegistry);
        }

        private static void AppendHeaderParameter(OpenApiDocument document, OpenApiOperation operation, ActionParameter parameter, string rootNamespace, ISchemaRegistry schemaRegistry)
        {
            // Header parameters named Accept, Content-Type and Authorization are not allowed. To describe these headers, use the corresponding OpenAPI keywords
            // See: https://swagger.io/docs/specification/describing-parameters/#header-parameters
            if (ReservedOpenApiHeaders.Contains(parameter.ApiParameterName))
            {
                return;
            }

            AppendParameter(document, operation, parameter, ParameterLocation.Header, rootNamespace, schemaRegistry);
        }

        private static void AppendQueryParameter(OpenApiDocument document, OpenApiOperation operation, ActionParameter parameter, string rootNamespace, ISchemaRegistry schemaRegistry) => AppendQueryParameter(document, operation, parameter, parameter.Type, parameter.Type.IsEnumerable, rootNamespace, schemaRegistry);
        private static void AppendQueryParameter(OpenApiDocument document, OpenApiOperation operation, ActionParameter parameter, TypeReference parameterType, bool isEnumerable, string rootNamespace, ISchemaRegistry schemaRegistry) => AppendParameter(document, operation, parameter, parameterType, isEnumerable, ParameterLocation.Query, rootNamespace, schemaRegistry);

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
            AppendQueryParameter(document, operation, parameter, parameterType, true, rootNamespace, schemaRegistry);
        }

        private static void AppendParameter(OpenApiDocument document, OpenApiOperation operation, ActionParameter parameter, ParameterLocation parameterLocation, string rootNamespace, ISchemaRegistry schemaRegistry) => AppendParameter(document, operation, parameter, parameter.Type, parameter.Type.IsEnumerable, parameterLocation, rootNamespace, schemaRegistry);
        private static OpenApiParameter AppendParameter(OpenApiDocument document, OpenApiOperation operation, ActionParameter actionParameter, TypeReference parameterType, bool isEnumerable, ParameterLocation parameterLocation, string rootNamespace, ISchemaRegistry schemaRegistry)
        {
            OpenApiParameter apiParameter = new OpenApiParameter
            {
                In = parameterLocation,
                Required = actionParameter.IsRequired,
                Name = actionParameter.ApiParameterName,
                Schema = CreateSchema(document, parameterType, isEnumerable, actionParameter.DefaultValue, rootNamespace, schemaRegistry)
            };
            operation.Parameters.Add(apiParameter);
            return apiParameter;
        }

        private static void AppendBody(OpenApiDocument document, OpenApiOperation operation, ActionDefinition action, string rootNamespace, ISchemaRegistry schemaRegistry)
        {
            if (action.RequestBody == null)
                return;

            OpenApiRequestBody body = new OpenApiRequestBody { Required = true };
            AppendContent(document, body.Content, action.RequestBody.MediaType, action.RequestBody.Contract, rootNamespace, schemaRegistry);
            operation.RequestBody = body;
        }

        private static void AppendResponses(OpenApiDocument document, OpenApiOperation operation, ActionDefinition action, string rootNamespace, ISchemaRegistry schemaRegistry)
        {
            foreach (ActionResponse actionResponse in action.Responses.Values.OrderBy(x => x.StatusCode))
            {
                OpenApiResponse apiResponse = new OpenApiResponse();

                if (actionResponse.ResultType != null)
                    AppendContent(document, apiResponse.Content, actionResponse.MediaType, actionResponse.ResultType, rootNamespace, schemaRegistry);

                StringBuilder sb = new StringBuilder(actionResponse.Description);
                if (actionResponse.Errors.Any())
                {
                    if (sb.Length > 0)
                        sb.AppendLine();

                    sb.Append($@"{HttpErrorResponseParser.ClientErrorCodeHeaderName}|{HttpErrorResponseParser.ClientErrorDescriptionHeaderName}
-|-
{String.Join(Environment.NewLine, actionResponse.Errors.Select(x => $"{(x.ErrorCode != 0 ? x.ErrorCode.ToString() : "n/a")}|{x.Description}"))}");

                    if (actionResponse.Errors.Any(x => x.ErrorCode != 0))
                    {
                        apiResponse.Headers.Add(HttpErrorResponseParser.ClientErrorCodeHeaderName, new OpenApiHeader
                        {
                            Description = "Additional error code to handle the error on the client",
                            Schema = PrimitiveTypeMap[PrimitiveType.Int16]()
                        });
                    }

                    if (actionResponse.Errors.Any(x => !String.IsNullOrEmpty(x.Description)))
                    {
                        apiResponse.Headers.Add(HttpErrorResponseParser.ClientErrorDescriptionHeaderName, new OpenApiHeader
                        {
                            Description = "A mesage describing the cause of the error",
                            Schema = PrimitiveTypeMap[PrimitiveType.String]()
                        });
                        const string mimeType = "text/plain";
                        apiResponse.Content.Add(mimeType, new OpenApiMediaType { Schema = PrimitiveTypeMap[PrimitiveType.String]() });
                    }
                }

                apiResponse.Description = sb.Length > 0 ? sb.ToString() : actionResponse.StatusCode.ToString();
                operation.Responses.Add(((int)actionResponse.StatusCode).ToString(), apiResponse);
            }
        }

        private static void AppendContent(OpenApiDocument document, IDictionary<string, OpenApiMediaType> target, string mediaType, TypeReference typeReference, string rootNamespace, ISchemaRegistry schemaRegistry)
        {
            OpenApiMediaType content = new OpenApiMediaType { Schema = CreateSchema(document, typeReference, rootNamespace, schemaRegistry) };
            target.Add(mediaType, content);
        }

        private static void AppendSecuritySchemes(OpenApiDocument document, ActionDefinition action, OpenApiOperation operation)
        {
            if (action.SecuritySchemes.SelectMany(x => x).All(x => x == SecuritySchemes.Anonymous.Name))
                return;

            foreach (ICollection<string> securitySchemeGroup in action.SecuritySchemes)
            {
                OpenApiSecurityRequirement requirement = new OpenApiSecurityRequirement();
                foreach (string securitySchemeName in securitySchemeGroup)
                {
                    if (securitySchemeName == SecuritySchemes.Anonymous.Name)
                        continue;

                    OpenApiSecurityScheme scheme = document.Components.SecuritySchemes[securitySchemeName];
                    requirement.Add(scheme, new Collection<string>());
                }
                operation.Security.Add(requirement);
            }
        }

        private static void AppendSecuritySchemes(OpenApiDocument document, IEnumerable<SecurityScheme> securitySchemes)
        {
            foreach (SecurityScheme modelSecurityScheme in securitySchemes)
            {
                string name = modelSecurityScheme.Name;
                OpenApiSecurityScheme openApiSecurityScheme = CreateSecurityScheme(name, modelSecurityScheme.Kind);

                if (openApiSecurityScheme == null)
                    continue;

                if (EnsureComponents(document).SecuritySchemes.ContainsKey(name)) 
                    continue;

                document.Components.SecuritySchemes.Add(name, openApiSecurityScheme);
            }
        }

        private static OpenApiSecurityScheme CreateSecurityScheme(string name, SecuritySchemeKind kind)
        {
            switch (kind)
            {
                case SecuritySchemeKind.None: 
                    return null;

                case SecuritySchemeKind.Bearer:
                    return new OpenApiSecurityScheme
                    {
                        Name = name,
                        Type = SecuritySchemeType.Http,
                        In = ParameterLocation.Header,
                        Reference = new OpenApiReference
                        {
                            Id = name,
                            Type = ReferenceType.SecurityScheme
                        },
                        Scheme = "bearer",
                        BearerFormat = "JWT"
                    };

                case SecuritySchemeKind.ApiKey:
                    return new OpenApiSecurityScheme
                    {
                        Name = name,
                        Type = SecuritySchemeType.ApiKey,
                        In = ParameterLocation.Header,
                        Reference = new OpenApiReference
                        {
                            Id = name,
                            Type = ReferenceType.SecurityScheme
                        }
                    };

                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        private static OpenApiSchema CreateSchema(OpenApiDocument document, TypeReference typeReference, string rootNamespace, ISchemaRegistry schemaRegistry) => CreateSchema(document, typeReference, typeReference.IsEnumerable, defaultValue: null, rootNamespace, schemaRegistry);
        private static OpenApiSchema CreateSchema(OpenApiDocument document, TypeReference typeReference, bool isEnumerable, DefaultValue defaultValue, string rootNamespace, ISchemaRegistry schemaRegistry)
        {
            OpenApiSchema schema = CreateSchemaCore(document, typeReference, defaultValue, rootNamespace, schemaRegistry);

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

        private static OpenApiSchema CreateSchemaCore(OpenApiDocument document, TypeReference typeReference, DefaultValue defaultValue, string rootNamespace, ISchemaRegistry schemaRegistry)
        {
            switch (typeReference)
            {
                case PrimitiveTypeReference primitiveContractPropertyType: return CreatePrimitiveTypeSchema(primitiveContractPropertyType, defaultValue);
                case SchemaTypeReference contractPropertyTypeReference: return CreateReferenceSchema(document, contractPropertyTypeReference, rootNamespace, schemaRegistry);
                default: throw new ArgumentOutOfRangeException(nameof(typeReference), typeReference, $"Unexpected property type: {typeReference}");
            }
        }

        private static OpenApiSchema CreatePrimitiveTypeSchema(PrimitiveTypeReference typeReference, DefaultValue defaultValue)
        {
            if (!PrimitiveTypeMap.TryGetValue(typeReference.Type, out Func<OpenApiSchema> schemaFactory))
                throw new InvalidOperationException($"Unexpected primitive type: {typeReference.Type}");

            OpenApiSchema schema = schemaFactory();
            schema.Nullable = typeReference.IsNullable;

            if (defaultValue != null)
                schema.Default = CreateDefaultValue(defaultValue.Value);

            return schema;
        }

        private static OpenApiSchema CreateReferenceSchema(OpenApiDocument document, SchemaTypeReference typeReference, string rootNamespace, ISchemaRegistry schemaRegistry)
        {
            SchemaDefinition schemaDefinition = schemaRegistry.GetSchema(typeReference);
            string typeName;

            if (UseRelativeNamespaces)
            {
                typeName = schemaDefinition.DefinitionName;
                string relativeNamespace = NamespaceUtility.BuildRelativeNamespace(rootNamespace, LayerName.DomainModel, schemaDefinition.Namespace);
                if (!String.IsNullOrEmpty(relativeNamespace))
                    typeName = $"{relativeNamespace}.{typeName}";
            }
            else
                typeName = schemaDefinition.FullName;

            EnsureSchema(document, typeName, schemaDefinition, rootNamespace, schemaRegistry);

            OpenApiSchema openApiSchema = new OpenApiSchema
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.Schema,
                    Id = typeName
                }
            };

            // OpenAPI 3.1
            /*
            if (typeReference.IsNullable)
            {
                openApiSchema = new OpenApiSchema
                {
                    OneOf =
                    {
                        NullSchema,
                        openApiSchema
                    }
                };

            }
            */

            return openApiSchema;
        }

        private static void EnsureSchema(OpenApiDocument document, string schemaName, SchemaDefinition contract, string rootNamespace, ISchemaRegistry schemaRegistry)
        {
            if (!EnsureComponents(document).Schemas.ContainsKey(schemaName))
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

                if (property.SerializationBehavior == SerializationBehavior.Always && !property.IsOptional)
                    schema.Required.Add(property.Name);

                if (property.DefaultValue != null)
                {
                    object defaultValue = property.DefaultValue.Value;
                    if (property.DefaultValue.EnumMember != null)
                        defaultValue = property.DefaultValue.EnumMember.ActualValue;

                    propertySchema.Default = CreateDefaultValue(defaultValue);
                }
            }
        }

        private static void AppendEnumSchema(OpenApiDocument document, string schemaName, EnumSchema enumContract)
        {
            OpenApiSchema schema = PrimitiveTypeMap[PrimitiveType.Int32]();
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

        private static OpenApiComponents EnsureComponents(OpenApiDocument document) => document.Components ?? (document.Components = new OpenApiComponents());

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