using System.ComponentModel.DataAnnotations;
using Dibix.Testing;
using Dibix.Testing.Generators;
using Microsoft.Extensions.Options;

namespace Dibix.Tests
{
    public sealed partial class TestConfigurationLoaderTests : TestBase
    {
    }

    public class TestConfigurationUsingProxy
    {
        [ValidateObjectMembers]
        public virtual NestedTestConfigurationUsingProxy Nested { get; } = new NestedTestConfigurationUsingProxy();
        [Required]
        public virtual int? NotInitializedValue { get; set; }
    }

    public class NestedTestConfigurationUsingProxy
    {
        public virtual int? InitializedValue { get; set; }
        [Required]
        public virtual int? NotInitializedValue { get; set; }
    }

    public sealed partial class TestConfigurationUsingSourceGeneration
    {
        [LazyValidation]
        private NestedTestConfigurationUsingSourceGeneration _nested;

        [LazyValidation]
        private int? _notInitializedValue;
    }

    public sealed partial class NestedTestConfigurationUsingSourceGeneration
    {
        [LazyValidation]
        private int? _initializedValue;

        [LazyValidation]
        private int? _notInitializedValue;
    }
}