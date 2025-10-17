using System;
using System.Linq;
using Microsoft.OpenApi;

namespace Dibix.Sdk.CodeGeneration.OpenApi
{
    internal static class ValidationRules
    {
        public static ValidationRuleSet All { get; } = BuildValidationRuleSet();

        private static ValidationRuleSet BuildValidationRuleSet()
        {
            ValidationRuleSet set = ValidationRuleSet.GetDefaultRuleSet();
            var rules = typeof(OpenApiArtifactsGenerationUnit).Assembly
                                                              .GetTypes()
                                                              .Where(x => typeof(ValidationRuleDescriptor).IsAssignableFrom(x) && !x.IsAbstract)
                                                              .Select(Activator.CreateInstance)
                                                              .Cast<ValidationRuleDescriptor>()
                                                              .Select(x => x.Create());

            foreach (ValidationRule rule in rules)
            {
                set.Add(rule.GetType(), rule);
            }

            return set;
        }
    }
}