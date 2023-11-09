using System;
using System.Collections.Generic;
using Dibix.Sdk.Abstractions;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class SqlDataTypeExtensions
    {
        public static TypeReference ToTypeReference
        (
            this DataTypeReference dataTypeReference
          , bool isNullable
          , string name
          , string relativeNamespace
          , string source
          , ISqlMarkupDeclaration markup
          , ITypeResolverFacade typeResolver
          , ILogger logger
          , out string udtName
        )
        {
            if (markup.TryGetSingleElementValue(SqlMarkupKey.ClrType, source, logger, out Token<string> typeImplementationName) && !(dataTypeReference is SqlDataTypeReference))
            {
                udtName = null;
                logger.LogError($@"The @ClrType hint is only supported for primitive types
Name: {name}
DataType: {dataTypeReference.GetType()}", source, dataTypeReference.StartLine, dataTypeReference.StartColumn);
                return null;
            }

            switch (dataTypeReference)
            {
                case SqlDataTypeReference sqlDataTypeReference:
                    {
                        udtName = null;

                        // Explicit override of generated type name
                        // Examples:
                        // /* @ClrType stream */ [data] VARBINARY(MAX) -> System.IO.Stream data
                        // /* @ClrType RequestTypeEnum */ [requesttype] TINYINT -> RequestTypeEnum requesttype
                        if (typeImplementationName != null)
                        {
                            TypeReference typeReference = typeResolver.ResolveType(typeImplementationName.Value, relativeNamespace, typeImplementationName.Location, isEnumerable: false);
                            if (typeReference != null)
                                typeReference.IsNullable = isNullable;

                            return typeReference;
                        }

                        if (PrimitiveTypeMap.TryGetPrimitiveType(sqlDataTypeReference.SqlDataTypeOption, out PrimitiveType dataType))
                        {
                            int? size = CollectSize(dataType, sqlDataTypeReference.Parameters);
                            return new PrimitiveTypeReference(dataType, isNullable, isEnumerable: false, size, new SourceLocation(source, dataTypeReference.StartLine, dataTypeReference.StartColumn));
                        }

                        logger.LogError($@"Unsupported sql data type
Name: {name}
DataType: {sqlDataTypeReference.SqlDataTypeOption}", source, dataTypeReference.StartLine, dataTypeReference.StartColumn);
                        return null;
                    }

                case UserDataTypeReference userDataTypeReference:
                    {
                        if (string.Equals(userDataTypeReference.Name.BaseIdentifier.Value, "SYSNAME", StringComparison.OrdinalIgnoreCase))
                        {
                            udtName = null;
                            return new PrimitiveTypeReference(PrimitiveType.String, isNullable, isEnumerable: false, size: 128, new SourceLocation(source, dataTypeReference.StartLine, dataTypeReference.StartColumn)){};
                        }

                        udtName = userDataTypeReference.Name.ToFullName();
                        TypeReference typeReference = typeResolver.ResolveType(TypeResolutionScope.UserDefinedType, udtName, relativeNamespace, new SourceLocation(source, dataTypeReference.StartLine, dataTypeReference.StartColumn), false);
                        return typeReference;
                    }

                case XmlDataTypeReference _:
                    udtName = null;
                    return new PrimitiveTypeReference(PrimitiveType.Xml, isNullable: false, isEnumerable: false, size: null, new SourceLocation(source, dataTypeReference.StartLine, dataTypeReference.StartColumn));

                default:
                    throw new InvalidOperationException($@"Unexpected data type reference
Name: {name}
DataType: {dataTypeReference.GetType()}");
            }
        }

        private static int? CollectSize(PrimitiveType type, IList<Literal> parameters) => type switch
        {
            PrimitiveType.String when parameters.Count == 1 && parameters[0] is IntegerLiteral sizeLiteral && Int32.TryParse(sizeLiteral.Value, out int size) => size,
            _ => null
        };
    }
}