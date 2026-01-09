using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using Dibix.Http;
using Dibix.Sdk.Abstractions;
using Microsoft.OpenApi;

namespace Dibix.Sdk.CodeGeneration.OpenApi
{
    internal static class OpenApiGenerator
    {
        private static readonly bool UseRelativeNamespaces = true;
        private static readonly string[] ReservedOpenApiHeaders = ["Accept", "Authorization", "Content-Type"];
        private static readonly string[] SupportedEnumExtensions = ["x-enum-varnames", "x-enumNames"];
        private static readonly OpenApiSchema NullSchema = new OpenApiSchema { Type = JsonSchemaType.Null };

        public static OpenApiDocument Generate(CodeGenerationModel model, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            OpenApiDocument document = new OpenApiDocument
            {
                Info = new OpenApiInfo
                {
                    Title = model.Title,
                    Version = !String.IsNullOrEmpty(model.OpenApiVersion) ? model.OpenApiVersion : "1.0.0",
                    Description = model.OpenApiDescription
                },
                Servers = { new OpenApiServer { Url = model.BaseUrl } }
            };

            AppendSecuritySchemes(document, model.SecuritySchemes);
            AppendPaths(document, model.AreaName, model.Controllers, model.RootNamespace, schemaRegistry, logger);
            return document;
        }

        private static void AppendPaths(OpenApiDocument document, string areaName, IEnumerable<ControllerDefinition> controllers, string rootNamespace, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            foreach (ControllerDefinition controller in controllers)
            {
                foreach (IGrouping<string, ActionDefinition> path in controller.Actions.GroupBy(x => $"/{RouteBuilder.BuildRoute(areaName, controller.Name, x.ChildRoute)}"))
                {
                    OpenApiPathItem value = new OpenApiPathItem();

                    foreach (ActionDefinition action in path)
                    {
                        HttpMethod httpMethod = ToHttpMethod(action.Method);

                        OpenApiOperation operation = new OpenApiOperation
                        {
                            Tags = new HashSet<OpenApiTagReference>([new OpenApiTagReference(controller.Name)]),
                            Summary = action.Description ?? action.OperationId.Value,
                            OperationId = action.OperationId.Value
                        };

                        AppendParameters(document, operation, action, rootNamespace, schemaRegistry, logger);
                        AppendBody(document, operation, action, rootNamespace, schemaRegistry, logger);
                        AppendResponses(document, operation, action, rootNamespace, schemaRegistry, logger);
                        AppendSecuritySchemes(action, operation, document);

                        value.AddOperation(httpMethod, operation);
                    }

                    document.Paths.Add(path.Key, value);
                }
            }
        }

        private static HttpMethod ToHttpMethod(HttpApiMethod method) => method switch
        {
            HttpApiMethod.Get => HttpMethod.Get,
            HttpApiMethod.Post => HttpMethod.Post,
            HttpApiMethod.Patch => new HttpMethod("PATCH"),
            HttpApiMethod.Put => HttpMethod.Put,
            HttpApiMethod.Delete => HttpMethod.Delete,
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
        };

        private static void AppendParameters(OpenApiDocument document, OpenApiOperation operation, ActionDefinition action, string rootNamespace, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            foreach (ApiParameter parameter in action.ApiParameters)
            {
                if (parameter.ParameterLocation != ActionParameterLocation.Query
                 && parameter.ParameterLocation != ActionParameterLocation.Path
                 && parameter.ParameterLocation != ActionParameterLocation.Header)
                    continue;

                // We don't support out parameters in REST APIs, but this accessor could still be used directly within the backend
                // Therefore we discard this parameter
                if (parameter.IsOutput)
                    continue;

                AppendUserParameter(document, operation, action, parameter, parameter.ParameterLocation, rootNamespace, schemaRegistry, logger);
            }
        }

        private static void AppendUserParameter(OpenApiDocument document, OpenApiOperation operation, ActionDefinition action, ApiParameter parameter, ActionParameterLocation location, string rootNamespace, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            switch (location)
            {
                case ActionParameterLocation.Query:
                    AppendQueryParameter(document, operation, parameter, parameter.Type, rootNamespace, schemaRegistry, logger);
                    break;

                case ActionParameterLocation.Path:
                    AppendPathParameter(document, operation, parameter, rootNamespace, schemaRegistry, logger);
                    break;

                case ActionParameterLocation.Header:
                    AppendHeaderParameter(document, operation, action, parameter, rootNamespace, schemaRegistry, logger);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(location), location, null);
            }
        }

        private static void AppendQueryParameter(OpenApiDocument document, OpenApiOperation operation, ApiParameter parameter, TypeReference parameterType, string rootNamespace, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            switch (parameterType)
            {
                case PrimitiveTypeReference:
                    AppendQueryParameter(document, operation, parameter, rootNamespace, schemaRegistry, logger);
                    break;

                case SchemaTypeReference schemaTypeReference:
                    SchemaDefinition schema = schemaRegistry.GetSchema(schemaTypeReference);
                    AppendComplexQueryParameter(document, operation, parameter, schema, rootNamespace, schemaRegistry, logger);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(parameterType), parameterType, null);
            }
        }

        private static void AppendPathParameter(OpenApiDocument document, OpenApiOperation operation, ApiParameter parameter, string rootNamespace, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            AppendParameter(document, operation, parameter, ParameterLocation.Path, rootNamespace, schemaRegistry, logger);
        }

        private static void AppendHeaderParameter(OpenApiDocument document, OpenApiOperation operation, ActionDefinition action, ApiParameter parameter, string rootNamespace, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            // Header parameters named Accept, Content-Type and Authorization are not allowed. To describe these headers, use the corresponding OpenAPI keywords
            // See: https://swagger.io/docs/specification/describing-parameters/#header-parameters
            if (ReservedOpenApiHeaders.Contains(parameter.ParameterName) || action.SecuritySchemes.Requirements.Any(x => x.Scheme.SchemeName == parameter.ParameterName))
            {
                return;
            }

            AppendParameter(document, operation, parameter, ParameterLocation.Header, rootNamespace, schemaRegistry, logger);
        }

        private static void AppendQueryParameter(OpenApiDocument document, OpenApiOperation operation, ApiParameter parameter, string rootNamespace, ISchemaRegistry schemaRegistry, ILogger logger) => AppendQueryParameter(document, operation, parameter, parameter.Type, parameter.Type.IsEnumerable, rootNamespace, schemaRegistry, logger);
        private static void AppendQueryParameter(OpenApiDocument document, OpenApiOperation operation, ApiParameter parameter, TypeReference parameterType, bool isEnumerable, string rootNamespace, ISchemaRegistry schemaRegistry, ILogger logger) => AppendParameter(document, operation, parameter, parameterType, isEnumerable, ParameterLocation.Query, rootNamespace, schemaRegistry, logger);

        private static void AppendComplexQueryParameter(OpenApiDocument document, OpenApiOperation operation, ApiParameter parameter, SchemaDefinition parameterSchema, string rootNamespace, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            switch (parameterSchema)
            {
                case EnumSchema:
                    AppendQueryParameter(document, operation, parameter, rootNamespace, schemaRegistry, logger);
                    break;

                case UserDefinedTypeSchema userDefinedTypeSchema:
                    AppendQueryArrayParameter(document, operation, parameter, userDefinedTypeSchema, rootNamespace, schemaRegistry, logger);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(parameterSchema), parameterSchema, null);
            }
        }

        private static void AppendQueryArrayParameter(OpenApiDocument document, OpenApiOperation operation, ApiParameter parameter, ObjectSchema parameterSchema, string rootNamespace, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            TypeReference parameterType = parameterSchema.Properties.Count > 1 ? parameter.Type : parameterSchema.Properties[0].Type;
            AppendQueryParameter(document, operation, parameter, parameterType, true, rootNamespace, schemaRegistry, logger);
        }

        private static void AppendParameter(OpenApiDocument document, OpenApiOperation operation, ApiParameter parameter, ParameterLocation parameterLocation, string rootNamespace, ISchemaRegistry schemaRegistry, ILogger logger) => AppendParameter(document, operation, parameter, parameter.Type, parameter.Type.IsEnumerable, parameterLocation, rootNamespace, schemaRegistry, logger);
        private static void AppendParameter(OpenApiDocument document, OpenApiOperation operation, ApiParameter actionParameter, TypeReference parameterType, bool isEnumerable, ParameterLocation parameterLocation, string rootNamespace, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            OpenApiParameter apiParameter = new OpenApiParameter
            {
                In = parameterLocation,
                Required = actionParameter.IsRequired,
                Name = actionParameter.ParameterName,
                Schema = CreateSchema(document, parameterType, isEnumerable, actionParameter.DefaultValue, rootNamespace, treatAsFile: false, schemaRegistry, logger)
            };

            operation.Parameters ??= new List<IOpenApiParameter>();
            operation.Parameters.Add(apiParameter);
        }

        private static void AppendBody(OpenApiDocument document, OpenApiOperation operation, ActionDefinition action, string rootNamespace, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            ActionRequestBody requestBody = action.RequestBody;
            if (requestBody == null)
                return;

            OpenApiRequestBody body = new OpenApiRequestBody
            {
                Required = true,
                Content = new Dictionary<string, IOpenApiMediaType>()
            };

            AppendContent(document, body.Content, requestBody.MediaType, requestBody.Contract, rootNamespace, requestBody.TreatAsFile != null, schemaRegistry, logger);
            operation.RequestBody = body;
        }

        private static void AppendResponses(OpenApiDocument document, OpenApiOperation operation, ActionDefinition action, string rootNamespace, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            foreach (ActionResponse actionResponse in action.Responses.Values.OrderBy(x => x.StatusCode))
            {
                OpenApiResponse apiResponse = new OpenApiResponse();

                if (actionResponse.ResultType != null)
                {
                    apiResponse.Content = new Dictionary<string, IOpenApiMediaType>();
                    AppendContent(document, apiResponse.Content, actionResponse.MediaType, actionResponse.ResultType, rootNamespace, treatAsFile: false, schemaRegistry, logger);
                }

                StringBuilder sb = new StringBuilder(actionResponse.Description);
                if (actionResponse.Errors.Any())
                {
                    if (sb.Length > 0)
                        sb.AppendLine();

                    sb.Append($"""
                               Code|Description
                               -|-
                               {String.Join(Environment.NewLine, actionResponse.Errors.Select(x => $"{x.ErrorCode}|{x.Description}"))}
                               """);
                }

                apiResponse.Description = sb.Length > 0 ? sb.ToString() : actionResponse.StatusCode.ToString();

                operation.Responses ??= new OpenApiResponses();
                operation.Responses.Add(((int)actionResponse.StatusCode).ToString(), apiResponse);
            }
        }

        private static void AppendContent(OpenApiDocument document, IDictionary<string, IOpenApiMediaType> target, string mediaType, TypeReference typeReference, string rootNamespace, bool treatAsFile, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            IOpenApiSchema schema = CreateSchema(document, typeReference, rootNamespace, treatAsFile, schemaRegistry, logger);
            OpenApiMediaType content = new OpenApiMediaType { Schema = schema };
            target.Add(mediaType, content);
        }

        private static void AppendSecuritySchemes(ActionDefinition action, OpenApiOperation operation, OpenApiDocument document)
        {
            if (!action.SecuritySchemes.HasEffectiveRequirements)
                return;

            OpenApiSecurityRequirement requirement = null;
            foreach (SecuritySchemeRequirement securitySchemeRequirement in action.SecuritySchemes.Requirements)
            {
                if (action.SecuritySchemes.Operator != SecuritySchemeOperator.And || requirement == null)
                {
                    requirement = new OpenApiSecurityRequirement();

                    operation.Security ??= new List<OpenApiSecurityRequirement>();
                    operation.Security.Add(requirement);
                }

                if (securitySchemeRequirement.Scheme == SecuritySchemes.Anonymous)
                    continue;

                requirement.Add(new OpenApiSecuritySchemeReference(securitySchemeRequirement.Scheme.SchemeName, document), []);
            }
        }

        private static void AppendSecuritySchemes(OpenApiDocument document, IEnumerable<SecurityScheme> securitySchemes)
        {
            foreach (SecurityScheme modelSecurityScheme in securitySchemes.Where(x => x.SchemeName != SecuritySchemeNames.Anonymous).OrderBy(x => x.SchemeName))
            {
                string name = modelSecurityScheme.SchemeName;
                OpenApiSecurityScheme openApiSecurityScheme = CreateSecurityScheme(modelSecurityScheme.Value);

                if (openApiSecurityScheme == null)
                    continue;

                document.AddComponent(name, openApiSecurityScheme);
            }
        }

        private static OpenApiSecurityScheme CreateSecurityScheme(SecuritySchemeValue value) => value switch
        {
            BearerSecuritySchemeValue => new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                In = ParameterLocation.Header,
                Scheme = "bearer",
                BearerFormat = "JWT"
            },
            HeaderSecuritySchemeValue headerSecuritySchemeValue => new OpenApiSecurityScheme
            {
                Name = headerSecuritySchemeValue.HeaderName,
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header
            },
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

        private static IOpenApiSchema CreateSchema(OpenApiDocument document, TypeReference typeReference, string rootNamespace, bool treatAsFile, ISchemaRegistry schemaRegistry, ILogger logger) => CreateSchema(document, typeReference, typeReference.IsEnumerable, defaultValue: null, rootNamespace, treatAsFile, schemaRegistry, logger);
        private static IOpenApiSchema CreateSchema(OpenApiDocument document, TypeReference typeReference, bool isEnumerable, ValueReference defaultValue, string rootNamespace, bool treatAsFile, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            IOpenApiSchema schema = CreateSchemaCore(document, typeReference, defaultValue, rootNamespace, treatAsFile, schemaRegistry, logger);

            if (isEnumerable)
            {
                schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.Array,
                    Items = schema
                };
            }

            return schema;
        }

        private static IOpenApiSchema CreateSchemaCore(OpenApiDocument document, TypeReference typeReference, ValueReference defaultValue, string rootNamespace, bool treatAsFile, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            if (treatAsFile)
            {
                return CreatePrimitiveTypeSchema(typeReference, PrimitiveType.Stream, defaultValue);
            }

            IOpenApiSchema schema = typeReference switch
            {
                PrimitiveTypeReference primitiveContractPropertyType => CreatePrimitiveTypeSchema(primitiveContractPropertyType, defaultValue),
                SchemaTypeReference contractPropertyTypeReference => CreateReferenceSchema(document, contractPropertyTypeReference, rootNamespace, schemaRegistry, logger),
                _ => throw new ArgumentOutOfRangeException(nameof(typeReference), typeReference, $"Unexpected property type: {typeReference}")
            };
            return schema;
        }

        private static OpenApiSchema CreatePrimitiveTypeSchema(PrimitiveTypeReference typeReference, ValueReference defaultValue) => CreatePrimitiveTypeSchema(typeReference, typeReference.Type, defaultValue);
        private static OpenApiSchema CreatePrimitiveTypeSchema(TypeReference typeReference, PrimitiveType type, ValueReference defaultValue)
        {
            if (!PrimitiveTypeMap.TryGetOpenApiFactory(type, out Func<OpenApiSchema> schemaFactory))
                throw new InvalidOperationException($"Unexpected primitive type: {type}");

            OpenApiSchema schema = schemaFactory();
            if (typeReference.IsNullable)
                schema.Type |= JsonSchemaType.Null;

            if (defaultValue != null)
                schema.Default = ParseDefaultValue(defaultValue);

            return schema;
        }

        private static IOpenApiSchema CreateReferenceSchema(OpenApiDocument document, SchemaTypeReference typeReference, string rootNamespace, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            SchemaDefinition schemaDefinition = schemaRegistry.GetSchema(typeReference);
            string typeName;

            if (UseRelativeNamespaces)
            {
                typeName = schemaDefinition.DefinitionName;
                if (!String.IsNullOrEmpty(schemaDefinition.RelativeNamespace))
                    typeName = $"{schemaDefinition.RelativeNamespace}.{typeName}";
            }
            else
            {
                typeName = schemaDefinition.FullName;
            }

            EnsureSchema(document, typeName, schemaDefinition, rootNamespace, schemaRegistry, logger);

            IOpenApiSchema openApiSchema = new OpenApiSchemaReference(typeName);

            if (typeReference.IsNullable)
            {
                openApiSchema = new OpenApiSchema
                {
                    OneOf =
                    [
                        NullSchema,
                        openApiSchema
                    ]
                };
            }

            return openApiSchema;
        }

        private static void EnsureSchema(OpenApiDocument document, string schemaName, SchemaDefinition contract, string rootNamespace, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            document.Components ??= new OpenApiComponents();
            document.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>();

            if (!document.Components.Schemas.ContainsKey(schemaName))
                AppendContractSchema(document, schemaName, contract, rootNamespace, schemaRegistry, logger);
        }

        private static void AppendContractSchema(OpenApiDocument document, string schemaName, SchemaDefinition contract, string rootNamespace, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            switch (contract)
            {
                case ObjectSchema objectContract:
                    AppendObjectSchema(document, schemaName, objectContract, rootNamespace, schemaRegistry, logger);
                    break;

                case EnumSchema enumContract:
                    AppendEnumSchema(document, schemaName, enumContract);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(contract), contract, "Unexpected contract definition");
            }
        }

        private static void AppendObjectSchema(OpenApiDocument document, string schemaName, ObjectSchema objectContract, string rootNamespace, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            OpenApiSchema schema = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                AdditionalPropertiesAllowed = false
            };

            document.Components ??= new OpenApiComponents();
            document.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>();
            document.Components.Schemas.Add(schemaName, schema); // Register schema before traversing properties to avoid endless recursions for self referencing properties

            foreach (ObjectSchemaProperty property in objectContract.Properties)
            {
                if (property.SerializationBehavior == SerializationBehavior.Never)
                    continue;

                string propertyName = StringExtensions.ToCamelCase(property.Name);
                IOpenApiSchema propertySchema = CreateSchema(document, property.Type, rootNamespace, treatAsFile: false, schemaRegistry, logger);

                schema.Properties ??= new Dictionary<string, IOpenApiSchema>();
                schema.Properties.Add(propertyName, propertySchema);

                if (property.SerializationBehavior == SerializationBehavior.Always && !property.IsOptional)
                {
                    schema.Required ??= new HashSet<string>();
                    schema.Required.Add(propertyName);
                }

                if (property.DefaultValue == null)
                    continue;

                if (propertySchema is OpenApiSchema openApiSchema)
                    openApiSchema.Default = ParseDefaultValue(property.DefaultValue);
            }
        }

        private static void AppendEnumSchema(OpenApiDocument document, string schemaName, EnumSchema enumContract)
        {
            OpenApiSchema schema = PrimitiveTypeMap.GetOpenApiFactory(PrimitiveType.Int32).Invoke();
            JsonArray enumNames = new JsonArray();

            schema.Description = String.Join("<br/>", enumContract.Members.Select(x => $"{x.ActualValue} = {x.Name}"));

            schema.Extensions = new Dictionary<string, IOpenApiExtension>();

            foreach (string extensionName in SupportedEnumExtensions)
            {
                schema.Extensions.Add(extensionName, new JsonNodeExtension(enumNames));
            }

            schema.Enum = new List<JsonNode>();

            foreach (EnumSchemaMember member in enumContract.Members)
            {
                schema.Enum.Add(JsonValue.Create(member.ActualValue));
                enumNames.Add(JsonValue.Create(member.Name));
            }

            document.Components ??= new OpenApiComponents();
            document.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>();
            document.Components.Schemas.Add(schemaName, schema);
        }

        private static JsonNode ParseDefaultValue(ValueReference defaultValue) => defaultValue switch
        {
            NullValueReference => JsonNullSentinel.JsonNull,
            PrimitiveValueReference primitiveValueReference => ParseDefaultValue(primitiveValueReference.Type, primitiveValueReference.Value),
            EnumMemberReference enumMemberReference => JsonValue.Create(enumMemberReference.Member.ActualValue),
            _ => throw new ArgumentOutOfRangeException(nameof(defaultValue), defaultValue, $"Unexpected default value reference: {defaultValue?.GetType()}")
        };
        private static JsonNode ParseDefaultValue(TypeReference typeReference, object value) => typeReference switch
        {
            PrimitiveTypeReference primitiveValueReference => ParseDefaultValue(primitiveValueReference.Type, value),
            _ => throw new ArgumentOutOfRangeException(nameof(typeReference), typeReference, $"Unexpected type reference: {typeReference?.GetType()}")
        };

        private static JsonNode ParseDefaultValue(PrimitiveType type, object value) => type switch
        {
            PrimitiveType.Boolean => JsonValue.Create((bool)value),
            PrimitiveType.Byte => JsonValue.Create((byte)value),
            PrimitiveType.Int16 => JsonValue.Create((short)value),
            PrimitiveType.Int32 => JsonValue.Create((int)value),
            PrimitiveType.Int64 => JsonValue.Create((long)value),
            PrimitiveType.Float => JsonValue.Create((float)value),
            PrimitiveType.Double => JsonValue.Create((double)value),
            PrimitiveType.Date => JsonValue.Create((DateTime)value),
            PrimitiveType.Time => JsonValue.Create((DateTime)value),
            PrimitiveType.DateTime => JsonValue.Create((DateTime)value),
            PrimitiveType.DateTimeOffset => JsonValue.Create((DateTimeOffset)value),
            PrimitiveType.String => JsonValue.Create((string)value),
            PrimitiveType.UUID => JsonValue.Create(value.ToString()),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}