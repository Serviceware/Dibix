﻿using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class BuiltInSchemaProvider : ISchemaProvider
    {
        private const string Source = "<internal>";
        private const string Namespace = "Dibix";
        private static readonly SourceLocation SourceLocation = new SourceLocation(Source, line: default, column: default);

        public static SchemaDefinition FileEntitySchema { get; } = CollectFileEntitySchema();
        public static SchemaDefinition ProblemDetailsSchema { get; } = CollectProblemDetailsSchema();

        public IEnumerable<SchemaDefinition> Collect()
        {
            yield return FileEntitySchema;
            yield return ProblemDetailsSchema;
        }

        private static SchemaDefinition CollectFileEntitySchema()
        {
            ObjectSchema schema = new ObjectSchema
            (
                absoluteNamespace: Namespace
              , relativeNamespace: null
              , definitionName: "FileEntity"
              , SchemaDefinitionSource.Internal
              , SourceLocation
              , [
                    new ObjectSchemaProperty(name: new Token<string>("Type", SourceLocation), new PrimitiveTypeReference(PrimitiveType.String, isNullable: false, isEnumerable: false))
                  , new ObjectSchemaProperty(name: new Token<string>("Data", SourceLocation), new PrimitiveTypeReference(PrimitiveType.Binary, isNullable: false, isEnumerable: false))
                  , new ObjectSchemaProperty(name: new Token<string>("FileName", SourceLocation), new PrimitiveTypeReference(PrimitiveType.String, isNullable: false, isEnumerable: false))
                ]
            );
            return schema;
        }

        private static ObjectSchema CollectProblemDetailsSchema()
        {
            // See
            // https://api.swaggerhub.com/domains/smartbear-public/ProblemDetails/1.0.0#/components/responses/BadRequest
            // https://swagger.io/blog/problem-details-rfc9457-api-error-handling/
            IList<ObjectSchemaProperty> properties = 
            [
                new ObjectSchemaProperty(name: new Token<string>("Type", SourceLocation), new PrimitiveTypeReference(PrimitiveType.Uri, isNullable: false, isEnumerable: false), isOptional: false/*, maxLength: 1024, description: "A URI reference that identifies the problem type."*/)
              , new ObjectSchemaProperty(name: new Token<string>("Status", SourceLocation), new PrimitiveTypeReference(PrimitiveType.Int32, isNullable: false, isEnumerable: false), isOptional: true/*, minimum: 100, maximum: 599, description: "The HTTP status code generated by the origin server for this occurrence of the problem."*/)
              , new ObjectSchemaProperty(name: new Token<string>("Title", SourceLocation), new PrimitiveTypeReference(PrimitiveType.String, isNullable: false, isEnumerable: false), isOptional: false/*, maxLength: 1024, description: "A short, human-readable summary of the problem type. It should not change from occurrence to occurrence of the problem, except for purposes of localization."*/)
              , new ObjectSchemaProperty(name: new Token<string>("Detail", SourceLocation), new PrimitiveTypeReference(PrimitiveType.String, isNullable: false, isEnumerable: false), isOptional: true/*, maxLength: 4096, description: "A human-readable explanation specific to this occurrence of the problem."*/)
              , new ObjectSchemaProperty(name: new Token<string>("Instance", SourceLocation), new PrimitiveTypeReference(PrimitiveType.String, isNullable: false, isEnumerable: false), isOptional: true/*, maxLength: 4096, description: "A URI reference that identifies the specific occurrence of the problem. It may or may not yield further information if dereferenced."*/)
              , new ObjectSchemaProperty(name: new Token<string>("Code", SourceLocation), new PrimitiveTypeReference(PrimitiveType.Int32, isNullable: false, isEnumerable: false), isOptional: false)
            ];
            ObjectSchema schema = new ObjectSchema(absoluteNamespace: Namespace, relativeNamespace: null, definitionName: "ProblemDetails", SchemaDefinitionSource.Internal, SourceLocation, properties);
            return schema;
        }
    }
}