using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk
{
    public sealed class SqlAccessorGeneratorConfiguration
    {
        public ICollection<ISourceSelection> Sources { get; private set; }
        public IWriter Writer { get; set; }

        public SqlAccessorGeneratorConfiguration()
        {
            this.Sources = new Collection<ISourceSelection>();
        }
    }
}