using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class UserDefinedTypeProvider : IUserDefinedTypeProvider
    {
        #region Fields
        private readonly string _productName;
        private readonly string _areaName;
        private readonly ITypeResolverFacade _typeResolver;
        private readonly ILogger _logger;
        private readonly IDictionary<string, UserDefinedTypeSchema> _schemas;
        #endregion

        #region Properties
        public IEnumerable<UserDefinedTypeSchema> Types => _schemas.Values;
        IEnumerable<SchemaDefinition> ISchemaProvider.Schemas => Types;
        #endregion

        #region Constructor
        public UserDefinedTypeProvider(string productName, string areaName, IEnumerable<TaskItem> source, ITypeResolverFacade typeResolver, ILogger logger)
        {
            _productName = productName;
            _areaName = areaName;
            _typeResolver = typeResolver;
            _logger = logger;
            _schemas = new Dictionary<string, UserDefinedTypeSchema>();
            Collect(source.Select(x => x.GetFullPath()));
        }
        #endregion

        #region Private Methods
        private void Collect(IEnumerable<string> inputs)
        {
            SqlUserDefinedTypeParser parser = new SqlUserDefinedTypeParser(_productName, _areaName, _typeResolver, _logger);
            _schemas.AddRange(inputs.Select(x => parser.Parse(x))
                                         .Where(x => x != null)
                                         .Select(x => new KeyValuePair<string, UserDefinedTypeSchema>(x.FullName, x)));
        }
        #endregion
    }
}