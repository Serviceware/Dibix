using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public interface ISqlMarkupDeclaration
    {
        bool HasElements { get; }
        ICollection<string> ElementNames { get; }

        bool TryGetSingleElement(string name, string source, ILogger logger, out ISqlElement element);
        bool TryGetSingleElementValue(string name, string source, ILogger logger, out string value);
        bool TryGetSingleElementValue(string name, string source, ILogger logger, out Token<string> value);
        bool HasSingleElement(string name, string source, ILogger logger);
        IEnumerable<ISqlElement> GetElements(string name);
    }
}