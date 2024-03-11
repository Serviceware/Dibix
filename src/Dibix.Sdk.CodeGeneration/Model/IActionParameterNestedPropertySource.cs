using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    internal interface IActionParameterNestedPropertySource
    {
        IReadOnlyCollection<ActionParameterItemSource> ItemSources { get; }
    }
}