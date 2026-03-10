using System;
using Dibix.Testing;
using Dibix.Testing.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Tests
{
    [TestClass]
    public sealed partial class TestConfigurationLoaderTests
    {
        [TestMethod]
        public void Load_Lazy_AccessNotInitializedProperty_ThrowsDuringPropertyAccess()
        {
            TestConfiguration configuration = TestConfigurationLoader.Load<TestConfiguration>(TestContext);
            Assert.IsNotNull(configuration);
            InvalidOperationException exception = Assert.ThrowsExactly<InvalidOperationException>(() => configuration.Nested.NotInitializedValue);
            Assert.AreEqual("Property not configured: Nested:NotInitializedValue", exception.Message);
        }

        [TestMethod]
        public void Load_Lazy_AccessInitializedProperty_DoesNotThrow()
        {
            TestConfiguration configuration = TestConfigurationLoader.Load<TestConfiguration>(TestContext);
            Assert.IsNotNull(configuration);
            Assert.AreEqual(1, configuration.Nested.InitializedValue);
        }

        [TestMethod]
        public void Load_DataAnnotations_IncludesNotInitializedProperty_ThrowsDuringLoad()
        {
            OptionsValidationException exception = Assert.ThrowsExactly<OptionsValidationException>(() => TestConfigurationLoader.Load<TestConfiguration>(TestContext, TestConfigurationValidationBehavior.DataAnnotations));
            Assert.AreEqual("DataAnnotation validation failed for 'TestConfiguration.Nested' members: 'NotInitializedValue' with the error: 'The NotInitializedValue field is required.'.", exception.Message);
        }
    }
}