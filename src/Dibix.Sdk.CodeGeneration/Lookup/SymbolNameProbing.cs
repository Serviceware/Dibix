using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dibix.Sdk.CodeGeneration
{
    public static class SymbolNameProbing
    {
        public static IEnumerable<string> EvaluateProbingCandidates(string productName, string areaName, string layerName, string relativeNamespace, string targetNamePath)
        {
            // Respect current namespace scope (based on relative namespace)
            TargetPath path = PathUtility.BuildAbsoluteTargetName(productName, areaName, layerName, relativeNamespace, targetNamePath);
            yield return path.Path;

            // Ignore relative namespace to enable referencing schemas outside of the current scope
            var (normalizedAreaName, normalizedTargetNamePath) = EnsureAreaName(areaName, targetNamePath);
            StringBuilder sb = new StringBuilder(productName);
            IEnumerable<string> parts = EnumerableExtensions.Create(normalizedAreaName, path.LayerName, path.TargetName).Where(x => !String.IsNullOrEmpty(x));
            foreach (string part in parts)
            {
                yield return $"{sb}.{normalizedTargetNamePath}";

                sb.Append('.')
                  .Append(part);
            }

            // Assume absolute target path
            yield return normalizedTargetNamePath;
        }

        // If the project contains multiple areas, extract the area name from the first segment of the target name
        private static (string normalizedAreaName, string normalizedTargetNamePath) EnsureAreaName(string areaName, string targetNamePath)
        {
            string normalizedAreaName = areaName;
            string normalizedTypeName = targetNamePath;
            if (areaName == null)
            {
                string[] typeNameParts = normalizedTypeName.Split(new[] { '.' }, 2);
                if (typeNameParts.Length > 1)
                {
                    normalizedAreaName = typeNameParts[0];
                    normalizedTypeName = typeNameParts[1];
                }
            }
            return (normalizedAreaName, normalizedTypeName);
        }
    }
}