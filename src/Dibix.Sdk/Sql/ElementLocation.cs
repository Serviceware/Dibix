using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.Sql
{
    public sealed class ElementLocation
    {
        private readonly Func<TSqlModel, TSqlObject> _elementAccessor;

        public int Offset { get; }
        public IList<string> Identifiers { get; }

        public ElementLocation(int offset, IEnumerable<string> identifiers, Func<TSqlModel, TSqlObject> elementAccessor)
        {
            this._elementAccessor = elementAccessor;
            this.Offset = offset;
            this.Identifiers = new Collection<string>();
            this.Identifiers.AddRange(identifiers);
        }

        public TSqlObject GetModelElement(TSqlModel model) => this._elementAccessor(model);
    }
}