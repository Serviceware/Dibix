using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class UserDefinedTypeSchemaProvider : ISchemaProvider
    {
        #region Fields
        private readonly string _productName;
        private readonly string _areaName;
        private readonly ITypeResolverFacade _typeResolver;
        private readonly ILogger _logger;
        private readonly ICollection<UserDefinedTypeSchema> _schemas;
        #endregion

        #region Constructor
        public UserDefinedTypeSchemaProvider(string productName, string areaName, IEnumerable<TaskItem> source, ITypeResolverFacade typeResolver, ILogger logger)
        {
            _productName = productName;
            _areaName = areaName;
            _typeResolver = typeResolver;
            _logger = logger;
            _schemas = new Collection<UserDefinedTypeSchema>();
            Collect(source.Select(x => x.GetFullPath()));
        }
        #endregion

        #region ISchemaProvider Members
        public IEnumerable<SchemaDefinition> Collect() => _schemas;
        #endregion

        #region Private Methods
        private void Collect(IEnumerable<string> inputs)
        {
            SqlUserDefinedTypeParser parser = new SqlUserDefinedTypeParser(_productName, _areaName, _typeResolver, _logger);
            _schemas.AddRange(inputs.Select(parser.Parse).Where(x => x != null));
        }
        #endregion
    }
}