using System.Collections.Generic;

namespace Dibix
{
    internal interface IActionParameterFixedPropertySourceDefinition
    {
        ICollection<string> Properties { get; }
    }
}