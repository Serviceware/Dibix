using Dibix.Http;
using Dibix.Sdk.CodeGeneration;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dibix.Sdk.OpenApi
{
    internal static class OpenApiGenerator
    {
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
          , string areaName
          , string rootNamespace
          , IEnumerable<ControllerDefinition> controllers
          , IEnumerable<SchemaDefinition> contracts
        )
        {
            OpenApiDocument document = new OpenApiDocument
            {
                Info = new OpenApiInfo
                {
                    Title = title,
                    Version = "1.0.0"
                }
            };
            AppendPaths(document, areaName, controllers);
            AppendSchemas(document, contracts, rootNamespace);
            return document;
        }

        private static void AppendPaths(OpenApiDocument document, string areaName, IEnumerable<ControllerDefinition> controllers)
        {
            foreach (ControllerDefinition controller in controllers)
            {
                foreach (IGrouping<string, ActionDefinition> path in controller.Actions.GroupBy(x => $"/{RouteBuilder.BuildRoute(areaName, controller.Name, x.ChildRoute)}"))
                {
                    OpenApiPathItem value = new OpenApiPathItem();

                    foreach (ActionDefinition action in path)
                    {
                        OperationType operationType = (OperationType)Enum.Parse(typeof(OperationType), action.Method.ToString());
                        string operationName = action.Target.Name;

                        OpenApiOperation operation = new OpenApiOperation();
                        operation.Tags.Add(new OpenApiTag { Name = $"{areaName}/{controller.Name}" });
                        operation.Summary = action.Description ?? "Undocumented action";
                        operation.OperationId = $"{areaName}_{controller}_{operationName}";
                        if (action.BodyContract != null)
                        {
                            //operation.RequestBody = new OpenApiRequestBody
                            //{
                            //    Required = true,
                            //    Content =
                            //    {
                            //        {
                            //            HttpParameterResolverUtility.BodyKey, new OpenApiMediaType
                            //            {
                            //                Schema = new OpenApiSchema
                            //                {

                            //                }
                            //            }
                            //        }
                            //    }
                            //};
                        }
                        //operation.Responses.Add(((int)HttpStatusCode.OK).ToString(), new OpenApiResponse
                        //{

                        //});

                        value.AddOperation(operationType, operation);
                    }

                    if (document.Paths == null)
                        document.Paths = new OpenApiPaths();

                    document.Paths.Add(path.Key, value);
                }
            }
        }

        private static void AppendSchemas(OpenApiDocument document, IEnumerable<SchemaDefinition> contracts, string rootNamespace)
        {
            foreach (SchemaDefinition contract in contracts)
            {
                if (document.Components == null)
                    document.Components = new OpenApiComponents();

                string schemaName = contract.DefinitionName;
                string relativeNamespace = NamespaceUtility.BuildRelativeNamespace(rootNamespace, LayerName.DomainModel, contract.Namespace);
                if (!String.IsNullOrEmpty(relativeNamespace))
                    schemaName = $"{relativeNamespace}.{schemaName}";

                OpenApiSchema schema = CreateSchema(contract, relativeNamespace);

                document.Components.Schemas.Add(schemaName, schema);
            }
        }

        private static OpenApiSchema CreateSchema(SchemaDefinition contract, string relativeNamespace)
        {
            OpenApiSchema schema;

            switch (contract)
            {
                case ObjectSchema objectContract:
                    schema = CreateObjectSchema(objectContract, relativeNamespace);
                    break;

                case EnumSchema enumContract:
                    schema = CreateEnumSchema(enumContract);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(contract), contract, "Unexpected contract definition");
            }

            return schema;
        }

        private static OpenApiSchema CreateObjectSchema(ObjectSchema objectContract, string relativeNamespace)
        {
            OpenApiSchema schema = new OpenApiSchema
            {
                Type = "object",
                AdditionalPropertiesAllowed = false
            };

            foreach (ObjectSchemaProperty property in objectContract.Properties)
            {
                if (property.SerializationBehavior == SerializationBehavior.Never)
                    continue;

                OpenApiSchema propertySchema = CreateObjectPropertySchema(property, relativeNamespace);
                schema.Properties.Add(property.Name, propertySchema);
            }

            return schema;
        }

        private static OpenApiSchema CreateObjectPropertySchema(ObjectSchemaProperty property, string @namespace)
        {
            OpenApiSchema schema = CreateObjectPropertySchema(property.Type, @namespace);
            schema.Nullable = property.Type.IsNullable;
            if (property.Type.IsEnumerable)
            {
                schema = new OpenApiSchema
                {
                    Type = "array",
                    Items = schema
                };
            }
            return schema;
        }

        private static OpenApiSchema CreateObjectPropertySchema(TypeReference propertyType, string @namespace)
        {
            switch (propertyType)
            {
                case PrimitiveTypeReference primitiveContractPropertyType: return CreatePrimitiveTypeSchema(primitiveContractPropertyType);
                case SchemaTypeReference contractPropertyTypeReference: return CreateReferenceSchema(contractPropertyTypeReference, @namespace);
                default: throw new ArgumentOutOfRangeException(nameof(propertyType), propertyType, $"Unexpected property type: {propertyType}");
            }
        }

        private static OpenApiSchema CreatePrimitiveTypeSchema(PrimitiveTypeReference type)
        {
            if (!PrimitiveTypeMap.TryGetValue(type.Type, out Func<OpenApiSchema> schemaFactory))
                throw new InvalidOperationException($"Unexpected primitive type: {type.Type}");

            return schemaFactory();
        }

        private static OpenApiSchema CreateReferenceSchema(SchemaTypeReference type, string @namespace)
        {
            string typeName = type.Key;
            if (!String.IsNullOrEmpty(@namespace))
                typeName = $"{@namespace}.{typeName}";

            return new OpenApiSchema
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.Schema,
                    Id = typeName
                }
            };
        }

        private static OpenApiSchema CreateEnumSchema(EnumSchema enumContract)
        {
            bool isFlagged = enumContract.Members.Any(x => x.Value != null);
            OpenApiSchema schema = isFlagged ? PrimitiveTypeMap[PrimitiveDataType.Int32]() : new OpenApiSchema { Type = "string" };
            
            foreach (EnumSchemaMember member in enumContract.Members)
            {
                if (member.Value != null)
                {
                    if (!Int32.TryParse(member.Value, out int value) && !TryComputeEnumValueReference(enumContract, member.Value, out value))
                        throw new InvalidOperationException($"Unexpected enum value for '{enumContract.DefinitionName}.{member.Name}': {member.Value}");

                    schema.Enum.Add(new OpenApiInteger(value));
                }
                else
                    schema.Enum.Add(new OpenApiString(member.Name));
            }

            return schema;
        }

        private static bool TryComputeEnumValueReference(EnumSchema enumContract, string memberValue, out int value)
        {
            value = 0;

            Queue<char> tokens = new Queue<char>(memberValue);
            char currentToken = default;
            char? bitwiseOperator = null;
            StringBuilder memberNameSb = new StringBuilder();
            while (tokens.Any())
            {
                while (tokens.Any() && (currentToken = tokens.Dequeue()) != default && currentToken != '|' && currentToken != '&')
                {
                    if (currentToken == ' ')
                        continue;

                    memberNameSb.Append(currentToken);
                }

                string memberNameReference = memberNameSb.ToString();
                EnumSchemaMember currentMember = enumContract.Members.SingleOrDefault(x => x.Name == memberNameReference);
                if (currentMember == null)
                    throw new InvalidOperationException($"Unexpected enum member name value reference: {memberNameReference}");

                int currentMemberValue = Int32.Parse(currentMember.Value);
                memberNameSb = new StringBuilder();

                if (bitwiseOperator.HasValue)
                {
                    if (bitwiseOperator.Value == '|')
                        value |= currentMemberValue;
                    else if (bitwiseOperator.Value == '&')
                        value &= currentMemberValue;

                    bitwiseOperator = null;
                }
                else
                    value = currentMemberValue;

                if (currentToken == '|' || currentToken == '&')
                {
                    bitwiseOperator = currentToken;
                }
            }
            return true;
        }
    }
}