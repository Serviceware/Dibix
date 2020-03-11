using System.Collections.Generic;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class UserDefinedTypeProvider : IUserDefinedTypeProvider
    {
        #region Fields
        private readonly string _productName;
        private readonly string _areaName;
        private readonly ITypeResolverFacade _typeResolver;
        private readonly IErrorReporter _errorReporter;
        private readonly IDictionary<string, UserDefinedTypeSchema> _schemas;
        #endregion

        #region Properties
        public IEnumerable<UserDefinedTypeSchema> Types => this._schemas.Values;
        IEnumerable<SchemaDefinition> ISchemaProvider.Schemas => this.Types;
        #endregion

        #region Constructor
        public UserDefinedTypeProvider(string productName, string areaName, IEnumerable<string> inputs, ITypeResolverFacade typeResolver, IErrorReporter errorReporter)
        {
            this._productName = productName;
            this._areaName = areaName;
            this._typeResolver = typeResolver;
            this._errorReporter = errorReporter;
            this._schemas = new Dictionary<string, UserDefinedTypeSchema>();
            this.Collect(inputs);
        }
        #endregion

        #region ISchemaProvider Members
        public bool TryGetSchema(string name, out SchemaDefinition schema)
        {
            if (this._schemas.TryGetValue(name, out UserDefinedTypeSchema userDefinedTypeSchema))
            {
                schema = userDefinedTypeSchema;
                return true;
            }
            schema = null;
            return false;
        }
        #endregion

        #region Private Methods
        private void Collect(IEnumerable<string> inputs)
        {
            SqlUserDefinedTypeParser parser = new SqlUserDefinedTypeParser(this._productName, this._areaName, this._typeResolver, this._errorReporter);
            this._schemas.AddRange(inputs.Select(x => parser.Parse(x))
                                         .Where(x => x != null)
                                         .Select(x => new KeyValuePair<string, UserDefinedTypeSchema>(x.FullName, x)));
        }
        #endregion
    }
}