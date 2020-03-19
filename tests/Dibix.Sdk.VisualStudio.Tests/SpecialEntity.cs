using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.VisualStudio.Tests
{
    internal sealed class SpecialEntity : Entity
    {
        public int Age { get; set; }
        public ICollection<Direction> Directions { get; }

        public SpecialEntity()
        {
            this.Directions = new Collection<Direction>();
        }
    }
}