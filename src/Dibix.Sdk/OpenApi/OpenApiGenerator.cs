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
        private static readonly Dictionary<ContractPropertyDataType, Func<OpenApiSchema>> PrimitiveTypeMap = new Dictionary<ContractPropertyDataType, Func<OpenApiSchema>>
        {
            [ContractPropertyDataType.Boolean]        = () => new OpenApiSchema { Type = "boolean"                       }
          , [ContractPropertyDataType.Byte]           = () => new OpenApiSchema { Type = "integer", Format = "int32"     }
          , [ContractPropertyDataType.Int16]          = () => new OpenApiSchema { Type = "integer", Format = "int32"     }
          , [ContractPropertyDataType.Int32]          = () => new OpenApiSchema { Type = "integer", Format = "int32"     }
          , [ContractPropertyDataType.Int64]          = () => new OpenApiSchema { Type = "integer", Format = "int64"     }
          , [ContractPropertyDataType.Float]          = () => new OpenApiSchema { Type = "number",  Format = "float"     }
          , [ContractPropertyDataType.Double]         = () => new OpenApiSchema { Type = "number",  Format = "double"    }
          , [ContractPropertyDataType.Decimal]        = () => new OpenApiSchema { Type = "number",  Format = "double"    }
          , [ContractPropertyDataType.Binary]         = () => new OpenApiSchema { Type = "string",  Format = "byte"      }
          , [ContractPropertyDataType.DateTime]       = () => new OpenApiSchema { Type = "string",  Format = "date-time" }
          , [ContractPropertyDataType.DateTimeOffset] = () => new OpenApiSchema { Type = "string",  Format = "date-time" }
          , [ContractPropertyDataType.String]         = () => new OpenApiSchema { Type = "string"                        }
          , [ContractPropertyDataType.UUID]           = () => new OpenApiSchema { Type = "string",  Format = "uuid"      }
        };

        public static OpenApiDocument Generate
        (
            string title
          , string areaName
          , IEnumerable<ControllerDefinition> controllers
          , IEnumerable<ContractDefinition> contracts
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
            AppendSchemas(document, contracts);
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
                        string operationName = action.Target.Target.Split(',').First().Split('.').Last();

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

        private static void AppendSchemas(OpenApiDocument document, IEnumerable<ContractDefinition> contracts)
        {
            foreach (ContractDefinition contract in contracts)
            {
                if (document.Components == null)
                    document.Components = new OpenApiComponents();

                string schemaName = contract.DefinitionName;
                if (!String.IsNullOrEmpty(contract.Namespace.RelativeNamespace))
                    schemaName = $"{contract.Namespace.RelativeNamespace}.{schemaName}";

                OpenApiSchema schema = CreateSchema(contract);

                document.Components.Schemas.Add(schemaName, schema);
            }
        }

        private static OpenApiSchema CreateSchema(ContractDefinition contract)
        {
            OpenApiSchema schema;

            switch (contract)
            {
                case ObjectContract objectContract:
                    schema = CreateObjectSchema(objectContract);
                    break;

                case EnumContract enumContract:
                    schema = CreateEnumSchema(enumContract);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(contract), contract, "Unexpected contract definition");
            }

            return schema;
        }

        private static OpenApiSchema CreateObjectSchema(ObjectContract objectContract)
        {
            OpenApiSchema schema = new OpenApiSchema
            {
                Type = "object",
                AdditionalPropertiesAllowed = false
            };

            foreach (ObjectContractProperty property in objectContract.Properties)
            {
                if (property.SerializationBehavior == SerializationBehavior.Never)
                    continue;

                OpenApiSchema propertySchema = CreateObjectPropertySchema(property, objectContract.Namespace.RelativeNamespace);
                schema.Properties.Add(property.Name, propertySchema);
            }

            return schema;
        }

        private static OpenApiSchema CreateObjectPropertySchema(ObjectContractProperty property, string @namespace)
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

        private static OpenApiSchema CreateObjectPropertySchema(ContractPropertyType propertyType, string @namespace)
        {
            switch (propertyType)
            {
                case PrimitiveContractPropertyType primitiveContractPropertyType: return CreatePrimitiveTypeSchema(primitiveContractPropertyType);
                case ContractPropertyTypeReference contractPropertyTypeReference: return CreateReferenceSchema(contractPropertyTypeReference, @namespace);
                default: throw new ArgumentOutOfRangeException(nameof(propertyType), propertyType, $"Unexpected property type: {propertyType}");
            }
        }

        private static OpenApiSchema CreatePrimitiveTypeSchema(PrimitiveContractPropertyType type)
        {
            if (!PrimitiveTypeMap.TryGetValue(type.Type, out Func<OpenApiSchema> schemaFactory))
                throw new InvalidOperationException($"Unexpected primitive type: {type.Type}");

            return schemaFactory();
        }

        private static OpenApiSchema CreateReferenceSchema(ContractPropertyTypeReference type, string @namespace)
        {
            string typeName = type.TypeName;
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

        private static OpenApiSchema CreateEnumSchema(EnumContract enumContract)
        {
            bool isFlagged = enumContract.Members.Any(x => x.Value != null);
            OpenApiSchema schema = isFlagged ? PrimitiveTypeMap[ContractPropertyDataType.Int32]() : new OpenApiSchema { Type = "string" };
            
            foreach (EnumContractMember member in enumContract.Members)
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

        private static bool TryComputeEnumValueReference(EnumContract enumContract, string memberValue, out int value)
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
                EnumContractMember currentMember = enumContract.Members.SingleOrDefault(x => x.Name == memberNameReference);
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