using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.Tests.CodeGeneration
{
    public class SpecialEntity : Entity
    {
        public int Age { get; set; }
        public ICollection<Direction> Directions { get; }

        public SpecialEntity()
        {
            this.Directions = new Collection<Direction>();
        }
    }
}