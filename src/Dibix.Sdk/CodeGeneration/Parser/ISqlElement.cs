using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public interface ISqlElement
    {
        string Source { get; }
        int Line { get; }
        int Column { get; }
        IEnumerable<ISqlElementProperty> Properties { get; }

        bool TryGetPropertyValue(string propertyName, bool isDefault, out Token<string> value);
        Token<string> GetPropertyValue(string name);
    }
}