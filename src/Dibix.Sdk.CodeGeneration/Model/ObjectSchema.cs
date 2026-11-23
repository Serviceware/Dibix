using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    public class ObjectSchema : SchemaDefinition
    {
        public const string ETagPropertyName = "ETag";
        public const string FileNamePropertyName = "FileName";

        public string WcfNamespace { get; }
        public SourceLocation? IsJsonFileResult { get; }
        public SourceLocation? ContainsETag { get; }
        public IReadOnlyList<ObjectSchemaProperty> Properties { get; }

        public ObjectSchema(string absoluteNamespace, string relativeNamespace, string definitionName, SchemaDefinitionSource source, SourceLocation location, IList<ObjectSchemaProperty> properties, string wcfNamespace = null, SourceLocation? isJsonFileResult = null, SourceLocation? containsETag = null) : base(absoluteNamespace, relativeNamespace, definitionName, source, location)
        {
            WcfNamespace = wcfNamespace;
            IsJsonFileResult = isJsonFileResult;
            ContainsETag = containsETag;
            Properties = new ReadOnlyCollection<ObjectSchemaProperty>(properties);
        }
    }
}