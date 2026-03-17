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
        public void Load_LazyUsingProxy_AccessNotInitializedRootProperty_ThrowsDuringPropertyAccess()
        {
            TestConfigurationUsingProxy configuration = TestConfigurationLoader.Load<TestConfigurationUsingProxy>(TestContext, TestConfigurationValidationBehavior.LazyUsingProxy);
            Assert.IsNotNull(configuration);
            InvalidOperationException exception = Assert.ThrowsExactly<InvalidOperationException>(() => configuration.NotInitializedValue);
            Assert.AreEqual("Property not configured: NotInitializedValue", exception.Message);
        }

        [TestMethod]
        public void Load_LazyUsingProxy_AccessNotInitializedNestedProperty_ThrowsDuringPropertyAccess()
        {
            TestConfigurationUsingProxy configuration = TestConfigurationLoader.Load<TestConfigurationUsingProxy>(TestContext, TestConfigurationValidationBehavior.LazyUsingProxy);
            Assert.IsNotNull(configuration);
            InvalidOperationException exception = Assert.ThrowsExactly<InvalidOperationException>(() => configuration.Nested.NotInitializedValue);
            Assert.AreEqual("Property not configured: Nested:NotInitializedValue", exception.Message);
        }

        [TestMethod]
        public void Load_LazyUsingProxy_AccessInitializedProperty_DoesNotThrow()
        {
            TestConfigurationUsingProxy configuration = TestConfigurationLoader.Load<TestConfigurationUsingProxy>(TestContext, TestConfigurationValidationBehavior.LazyUsingProxy);
            Assert.IsNotNull(configuration);
            Assert.AreEqual(1, configuration.Nested.InitializedValue);
        }

        [TestMethod]
        public void Load_LazyUsingProxy_AccessNotInitializedProperty_DuringInitialization_DoesNotThrow()
        {
            int? notInitializedValue = -1;
            TestConfigurationUsingProxy configuration = TestConfigurationLoader.Load<TestConfigurationUsingProxy>(TestContext, TestConfigurationValidationBehavior.LazyUsingProxy, x => notInitializedValue = x.NotInitializedValue);
            Assert.IsNotNull(configuration);
            Assert.IsNull(notInitializedValue);
        }

        [TestMethod]
        public void Load_LazyUsingSourceGeneration_AccessNotInitializedRootProperty_ThrowsDuringPropertyAccess()
        {
            TestConfigurationUsingSourceGeneration configuration = TestConfigurationLoader.Load<TestConfigurationUsingSourceGeneration>(TestContext, TestConfigurationValidationBehavior.LazyUsingSourceGeneration);
            Assert.IsNotNull(configuration);
            InvalidOperationException exception = Assert.ThrowsExactly<InvalidOperationException>(() => configuration.NotInitializedValue);
            Assert.AreEqual("Property not configured: NotInitializedValue", exception.Message);
        }

        [TestMethod]
        public void Load_LazyUsingSourceGeneration_AccessNotInitializedNestedProperty_ThrowsDuringPropertyAccess()
        {
            TestConfigurationUsingSourceGeneration configuration = TestConfigurationLoader.Load<TestConfigurationUsingSourceGeneration>(TestContext, TestConfigurationValidationBehavior.LazyUsingSourceGeneration);
            Assert.IsNotNull(configuration);
            InvalidOperationException exception = Assert.ThrowsExactly<InvalidOperationException>(() => configuration.Nested.NotInitializedValue);
            Assert.AreEqual("Property not configured: Nested:NotInitializedValue", exception.Message);
        }

        [TestMethod]
        public void Load_LazyUsingSourceGeneration_AccessInitializedProperty_DoesNotThrow()
        {
            TestConfigurationUsingSourceGeneration configuration = TestConfigurationLoader.Load<TestConfigurationUsingSourceGeneration>(TestContext, TestConfigurationValidationBehavior.LazyUsingSourceGeneration);
            Assert.IsNotNull(configuration);
            Assert.AreEqual(1, configuration.Nested.InitializedValue);
        }

        [TestMethod]
        public void Load_LazyUsingUsingSourceGeneration_AccessNotInitializedProperty_DuringInitialization_DoesNotThrow()
        {
            int? notInitializedValue = -1;
            TestConfigurationUsingSourceGeneration configuration = TestConfigurationLoader.Load<TestConfigurationUsingSourceGeneration>(TestContext, TestConfigurationValidationBehavior.LazyUsingSourceGeneration, x => notInitializedValue = x.NotInitializedValue);
            Assert.IsNotNull(configuration);
            Assert.IsNull(notInitializedValue);
        }

        [TestMethod]
        public void Load_DataAnnotations_IncludesNotInitializedProperties_ThrowsDuringLoad()
        {
            OptionsValidationException exception = Assert.ThrowsExactly<OptionsValidationException>(() => TestConfigurationLoader.Load<TestConfigurationUsingProxy>(TestContext, TestConfigurationValidationBehavior.DataAnnotations));
            Assert.AreEqual("DataAnnotation validation failed for 'TestConfigurationUsingProxy' members: 'NotInitializedValue' with the error: 'The NotInitializedValue field is required.'.; DataAnnotation validation failed for 'TestConfigurationUsingProxy.Nested' members: 'NotInitializedValue' with the error: 'The NotInitializedValue field is required.'.", exception.Message);
        }
    }
}