using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dibix.Sdk.Abstractions;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class PropertyPathParameterSourceReader(ISchemaRegistry schemaRegistry, ILogger logger, IActionParameterSourceRegistry actionParameterSourceRegistry) : IParameterSourceReader
    {
        ActionParameterSourceBuilder IParameterSourceReader.Read(JToken value, JTokenType type, ActionRequestBody requestBody, IReadOnlyDictionary<string, PathParameter> pathParameters, ActionParameterPropertySourceBuilder rootParameterSourceBuilder)
        {
            if (type != JTokenType.String)
                return null;

            string stringValue = (string)value;
            if (stringValue != null && stringValue.Contains('.'))
            {
                SourceLocation valueLocation = value.GetSourceInfo();
                return CollectPropertyParameterSource(stringValue, valueLocation, requestBody, rootParameterSourceBuilder);
            }

            return null;
        }

        private ActionParameterSourceBuilder CollectPropertyParameterSource(string value, SourceLocation valueLocation, ActionRequestBody requestBody, ActionParameterPropertySourceBuilder rootParameterSourceBuilder)
        {
            string[] parts = value.Split(['.'], 2);
            string sourceName = parts[0];
            string propertyName = parts[1];

            if (!actionParameterSourceRegistry.TryGetDefinition(sourceName, out ActionParameterSourceDefinition definition))
            {
                logger.LogError($"Unknown property source '{sourceName}'", valueLocation.Source, valueLocation.Line, valueLocation.Column);
            }

            if (definition is ClaimParameterSource claimParameterSource)
                return new StaticActionParameterSourceBuilder(new ActionParameterClaimSource(claimParameterSource, propertyName, valueLocation));

            ActionParameterPropertySourceBuilder propertySourceBuilder = new ActionParameterPropertySourceBuilder(definition, propertyName, valueLocation);
            CollectPropertySourceNodes(propertySourceBuilder, propertySourceBuilder.Definition, requestBody, rootParameterSourceBuilder);
            return propertySourceBuilder;
        }

        private void CollectPropertySourceNodes(ActionParameterPropertySourceBuilder propertySourceBuilder, ActionParameterSourceDefinition definition, ActionRequestBody requestBody, ActionParameterPropertySourceBuilder rootPropertySourceBuilder)
        {
            switch (definition)
            {
                case BodyParameterSource:
                    CollectBodyPropertySourceNodes(propertySourceBuilder, requestBody);
                    break;

                case ItemParameterSource:
                    CollectItemPropertySourceNodes(propertySourceBuilder, rootPropertySourceBuilder);
                    break;

                case DynamicParameterSource:
                case HeaderParameterSource:
                case QueryParameterSource:
                case PathParameterSource:
                    break;

                case IActionParameterFixedPropertySourceDefinition dynamicPropertyParameterSource:
                    CollectNonUserPropertyNode(dynamicPropertyParameterSource, propertySourceBuilder);
                    break;

                // Validation errors already logged previously for unknown source
                case null:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(definition), definition, null);
            }
        }

        private void CollectBodyPropertySourceNodes(ActionParameterPropertySourceBuilder propertySourceBuilder, ActionRequestBody requestBody)
        {
            if (requestBody == null)
            {
                logger.LogError("Must specify a body contract on the endpoint action when using BODY property source", propertySourceBuilder.Location.Source, propertySourceBuilder.Location.Line, propertySourceBuilder.Location.Column);
                return;
            }

            // Only traverse, if the body is an object contract
            TypeReference type = requestBody.Contract;
            if (type is not SchemaTypeReference bodySchemaTypeReference)
                return;

            if (schemaRegistry.GetSchema(bodySchemaTypeReference) is not ObjectSchema objectSchema)
                return;

            IList<string> segments = propertySourceBuilder.PropertyName.Split('.');
            CollectPropertySourceNodes(propertySourceBuilder, segments, type, objectSchema);
        }

        private void CollectItemPropertySourceNodes(ActionParameterPropertySourceBuilder propertySource, ActionParameterPropertySourceBuilder rootPropertySourceBuilder)
        {
            if (!rootPropertySourceBuilder.Nodes.Any())
            {
                // Oops, a previous error should have been logged in this case
                return;
            }

            ActionParameterPropertySourceNode lastNode = rootPropertySourceBuilder.Nodes.LastOrDefault();
            if (lastNode == null)
                throw new InvalidOperationException($"Missing resolved source property node for item property mapping ({rootPropertySourceBuilder.PropertyName})");

            ActionParameterPropertySourceNode nestedEnumerableParent = rootPropertySourceBuilder.Nodes
                                                                                                .Reverse()
                                                                                                .Skip(1)
                                                                                                .FirstOrDefault(x => x.Property.Type.IsEnumerable);

            bool isObjectSchema = TryCollectNodeSchema(lastNode, out SchemaTypeReference propertySchemaTypeReference, out ObjectSchema objectSchema, logger, rootPropertySourceBuilder, schemaRegistry);

            IList<string> segments = new Collection<string>();

            foreach (string propertyName in propertySource.PropertyName.Split('.'))
            {
                if (propertyName is ItemParameterSource.IndexPropertyName or nameof(NestedEnumerablePair<object, object>.ParentIndex) or nameof(NestedEnumerablePair<object, object>.ChildIndex))
                    break;

                if (nestedEnumerableParent != null)
                {
                    if (propertyName == nameof(NestedEnumerablePair<object, object>.Parent))
                    {
                        if (!(isObjectSchema = TryCollectNodeSchema(nestedEnumerableParent, out propertySchemaTypeReference, out objectSchema, logger, rootPropertySourceBuilder, schemaRegistry)))
                            return;

                        continue;
                    }

                    if (propertyName == nameof(NestedEnumerablePair<object, object>.Child))
                    {
                        continue;
                    }
                }

                segments.Add(propertyName);
            }

            if (!isObjectSchema)
                return;

            CollectPropertySourceNodes(propertySource, segments, propertySchemaTypeReference, objectSchema);
        }

        private static bool TryCollectNodeSchema(ActionParameterPropertySourceNode node, out SchemaTypeReference propertySchemaTypeReference, out ObjectSchema objectSchema, ILogger logger, ActionParameterPropertySourceBuilder rootPropertySourceBuilder, ISchemaRegistry schemaRegistry)
        {
            TypeReference type = node.Property.Type;
            if (type is not SchemaTypeReference typeReference)
            {
                propertySchemaTypeReference = null;
                objectSchema = null;
                return false;
            }

            SchemaDefinition propertySchema = schemaRegistry.GetSchema(typeReference);
            if (propertySchema is not ObjectSchema schema)
            {
                propertySchemaTypeReference = null;
                objectSchema = null;
                return false;
            }

            propertySchemaTypeReference = typeReference;
            objectSchema = schema;
            return true;
        }

        private void CollectPropertySourceNodes(ActionParameterPropertySourceBuilder propertySourceBuilder, IEnumerable<string> segments, TypeReference typeReference, ObjectSchema schema)
        {
            TypeReference type = typeReference;
            ObjectSchema objectSchema = schema;
            string currentPath = null;
            int columnOffset = 0;
            foreach (string propertyName in segments)
            {
                currentPath = currentPath == null ? propertyName : $"{currentPath}.{propertyName}";

                if (!CollectPropertyNode(type, objectSchema, propertyName, propertySourceBuilder, logger, columnOffset, out type))
                    return;

                columnOffset += propertyName.Length + 1; // Skip property name + dot
                objectSchema = type is SchemaTypeReference schemaTypeReference ? schemaRegistry.GetSchema(schemaTypeReference) as ObjectSchema : null;

                if (objectSchema == null)
                    break;
            }
        }

        private static bool CollectPropertyNode(TypeReference type, ObjectSchema objectSchema, string propertyName, ActionParameterPropertySourceBuilder propertySourceBuilder, ILogger logger, int columnOffset, out TypeReference propertyType)
        {
            ObjectSchemaProperty property = objectSchema.Properties.SingleOrDefault(x => x.Name.Value == propertyName);
            if (property != null)
            {
                propertySourceBuilder.Nodes.Add(new ActionParameterPropertySourceNode(objectSchema, property));
                propertyType = property.Type;
                return true;
            }

            int definitionNameOffset = propertySourceBuilder.Definition.Name.Length + 1; // Skip source name + dot
            int column = propertySourceBuilder.Location.Column + definitionNameOffset + columnOffset;
            logger.LogError($"Property '{propertyName}' not found on contract '{type.DisplayName}'", propertySourceBuilder.Location.Source, propertySourceBuilder.Location.Line, column);
            propertyType = null;
            return false;
        }

        private static void CollectNonUserPropertyNode(IActionParameterFixedPropertySourceDefinition source, ActionParameterPropertySourceBuilder propertySourceBuilder)
        {
            string propertyName = propertySourceBuilder.PropertyName;
            PropertyParameterSourceDescriptor property = source.Properties.SingleOrDefault(x => x.Name == propertyName);
            if (property == null)
            {
                // Validation errors already logged previously for unknown property
                return;
            }
            propertySourceBuilder.Nodes.Add(new ActionParameterPropertySourceNode(schema: null, property));
        }
    }
}