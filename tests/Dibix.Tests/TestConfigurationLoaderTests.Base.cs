using System.ComponentModel.DataAnnotations;
using Dibix.Testing;
using Microsoft.Extensions.Options;

namespace Dibix.Tests
{
    public sealed partial class TestConfigurationLoaderTests : TestBase
    {
    }

    public class TestConfiguration
    {
        [ValidateObjectMembers]
        public virtual NestedTestConfiguration Nested { get; } = new NestedTestConfiguration();
    }

    public class NestedTestConfiguration
    {
        public virtual int? InitializedValue { get; set; }
        [Required]
        public virtual int? NotInitializedValue { get; set; }
    }
}