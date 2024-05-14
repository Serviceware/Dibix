using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.Abstractions;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ConstantParameterSourceReader(ISchemaRegistry schemaRegistry, ILogger logger) : IParameterSourceReader
    {
        ActionParameterSourceBuilder IParameterSourceReader.Read(JToken value, JTokenType type, ActionRequestBody requestBody, IReadOnlyDictionary<string, PathParameter> pathParameters, ActionParameterPropertySourceBuilder rootParameterSourceBuilder)
        {
            switch (type)
            {
                case JTokenType.Boolean:
                case JTokenType.Integer:
                case JTokenType.Null:
                    return new ActionParameterConstantSourceBuilder((JValue)value, schemaRegistry, logger);

                case JTokenType.String:
                    string stringValue = (string)value;
                    if (stringValue != null && stringValue.Contains('.')) // Property path
                        return null;

                    return new ActionParameterConstantSourceBuilder((JValue)value, schemaRegistry, logger);

                default:
                    return null;
            }
        }
    }
}