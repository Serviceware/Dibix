using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal abstract class ParameterSourceReaderBase(IEnumerable<IParameterSourceReader> readers)
    {
        public virtual ActionParameterSourceBuilder Read(JProperty property, ActionRequestBody requestBody, IReadOnlyDictionary<string, PathParameter> pathParameters, ActionParameterPropertySourceBuilder rootParameterSourceBuilder)
        {
            foreach (IParameterSourceReader reader in readers)
            {
                ActionParameterSourceBuilder source = reader.Read(property.Value, property.Value.Type, requestBody, pathParameters, rootParameterSourceBuilder);
                if (source != null)
                    return source;
            }
            return null;
        }
    }
}