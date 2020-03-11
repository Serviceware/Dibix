using System;
using System.Linq;
using System.Text;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class NamespaceUtility
    {
        public static string EnsureAreaName(string areaName)
        {
            if (String.IsNullOrEmpty(areaName))
                throw new InvalidOperationException("The project does not support areas since no 'AreaName' property was specified");

            return areaName;
        }

        public static string BuildRootNamespace(string productName, string areaName)
        {
            StringBuilder sb = new StringBuilder(productName);
            if (!String.IsNullOrEmpty(areaName))
                sb.Append('.')
                  .Append(areaName);

            return sb.ToString();
        }

        public static string BuildAbsoluteNamespace(string productName, string areaName, string layerName, string relativeNamespace)
        {
            if (productName == null)
            {
                // Namespaces not supported in this context (probably T4)
                return null;
            }

            Guard.IsNotNullOrEmpty(layerName, nameof(layerName));

            string[] parts = (relativeNamespace ?? String.Empty).Split('.');
            bool multipleAreas = areaName == null;
            if (multipleAreas && parts.Length < 1)
                throw new InvalidOperationException("If the project has multiple areas, contract reference must be prefixed with the area name like '#Area.Contract'");

            if (areaName == null)
                areaName = parts[0];

            relativeNamespace = String.Join(".", parts.Skip(multipleAreas ? 1 : 0));


            StringBuilder sb = new StringBuilder(productName);

            if (!String.IsNullOrEmpty(areaName))
            {
                sb.Append('.')
                  .Append(areaName);
            }

            sb.Append('.')
              .Append(layerName);

            if (!String.IsNullOrEmpty(relativeNamespace))
            {
                sb.Append('.')
                  .Append(relativeNamespace);
            }

            return sb.ToString();
        }

        public static string BuildRelativeNamespace(string rootNamespace, string layerName, string absoluteNamespace)
        {
            bool multipleAreas = rootNamespace.IndexOf('.') < 0;
            int startIndex = rootNamespace.Length + 1;
            if (!multipleAreas)
                startIndex += +layerName.Length + 1;

            return startIndex < absoluteNamespace.Length ? absoluteNamespace.Substring(startIndex) : null;
        }
    }
}
