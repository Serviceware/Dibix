using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class InputConfiguration
    {
        public ICollection<SourceConfiguration> Sources { get; }

        public InputConfiguration()
        {
            this.Sources = new Collection<SourceConfiguration>();
        }
    }
}