using System;
using System.Linq;
using Microsoft.OpenApi.Validations;

namespace Dibix.Sdk.CodeGeneration.OpenApi
{
    internal static class ValidationRules
    {
        public static ValidationRuleSet All { get; } = BuildValidationRuleSet();

        private static ValidationRuleSet BuildValidationRuleSet()
        {
            ValidationRuleSet set = ValidationRuleSet.GetDefaultRuleSet();
            typeof(OpenApiArtifactsGenerationUnit).Assembly
                                                  .GetTypes()
                                                  .Where(x => typeof(ValidationRuleDescriptor).IsAssignableFrom(x) && !x.IsAbstract)
                                                  .Select(Activator.CreateInstance)
                                                  .Cast<ValidationRuleDescriptor>()
                                                  .Select(x => x.Create())
                                                  .Each(set.Add);
            return set;
        }
    }
}