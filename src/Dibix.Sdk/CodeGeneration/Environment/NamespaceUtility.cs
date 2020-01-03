using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        public static string BuildNamespace(string productName, string areaName, string layerName, string relativeNamespace)
        {
            Guard.IsNotNullOrEmpty(layerName, nameof(layerName));

            ICollection<string> segments = new Collection<string>();
            if (!String.IsNullOrEmpty(productName))
                segments.Add(productName);

            if (String.IsNullOrEmpty(areaName) && !String.IsNullOrEmpty(relativeNamespace))
            {
                string[] parts = relativeNamespace.Split(new[] { '.' }, 2);
                areaName = parts[0];
                relativeNamespace = parts.Length > 1 ? parts[1] : null;
            }

            if (!String.IsNullOrEmpty(areaName))
                segments.Add(areaName);

            segments.Add(layerName);

            if (!String.IsNullOrEmpty(relativeNamespace))
                segments.Add(relativeNamespace);

            string @namespace = String.Join(".", segments);
            return @namespace;
        }
    }
}
