using System.Collections.Generic;
using Dibix.Sdk.Abstractions;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class BodyConverterParameterSourceReader : IParameterSourceReader
    {
        ActionParameterSourceBuilder IParameterSourceReader.Read(JToken value, JTokenType type, ActionRequestBody requestBody, IReadOnlyDictionary<string, PathParameter> pathParameters, ActionParameterPropertySourceBuilder rootParameterSourceBuilder)
        {
            if (type != JTokenType.Object)
                return null;

            JObject @object = (JObject)value;
            JProperty bodyConverterProperty = @object.Property("convertFromBody");
            if (bodyConverterProperty != null)
                return CollectBodyConverterParameterSource(bodyConverterProperty);

            return null;
        }

        private static ActionParameterSourceBuilder CollectBodyConverterParameterSource(JProperty bodyConverterProperty)
        {
            JValue bodyConverterValue = (JValue)bodyConverterProperty.Value;
            SourceLocation sourceLocation = bodyConverterValue.GetSourceInfo();
            string bodyConverterTypeName = (string)bodyConverterValue.Value;
            return new StaticActionParameterSourceBuilder(new ActionParameterBodySource(new Token<string>(bodyConverterTypeName, sourceLocation)));
        }
    }
}