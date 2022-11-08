using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class PathUtility
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

        public static NamespacePath BuildAbsoluteNamespace(string productName, string areaName, string layerName, string relativeNamespace)
        {
            var (_, _, path) = BuildPath(productName, ref areaName, layerName, ref relativeNamespace, targetNamePath: null);
            return new NamespacePath(productName, areaName, layerName, relativeNamespace, path);
        }

        public static TargetPath BuildAbsoluteTargetName(string productName, string areaName, string layerName, string relativeNamespace, string targetNamePath)
        {
            var (@namespace, typeName, path) = BuildPath(productName, ref areaName, layerName, ref relativeNamespace, targetNamePath);
            return new TargetPath(productName, areaName, layerName, relativeNamespace, @namespace, typeName, path);
        }

        public static string BuildRelativeNamespace(string rootNamespace, string layerName, string absoluteNamespace)
        {
            bool multipleAreas = rootNamespace.IndexOf('.') < 0;
            int startIndex = rootNamespace.Length + 1;
            if (!multipleAreas)
                startIndex += +layerName.Length + 1;

            return startIndex < absoluteNamespace.Length ? absoluteNamespace.Substring(startIndex) : null;
        }

        private static (string @namespace, string typeName, string path) BuildPath(string productName, ref string areaName, string layerName, ref string relativeNamespace, string targetNamePath)
        {
            IList<string> relativeNamespaceParts = (relativeNamespace ?? String.Empty).Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            string targetName = null;
            if (targetNamePath != null)
            {
                IList<string> childTokenParts = targetNamePath.Split('.').ToList();
                targetName = childTokenParts.Last();
                childTokenParts.RemoveAt(childTokenParts.Count - 1);
                relativeNamespaceParts.AddRange(childTokenParts);
            }

            if (areaName == null && relativeNamespaceParts.Any())
            {
                areaName = relativeNamespaceParts[0];
                relativeNamespaceParts.RemoveAt(0);
            }


            relativeNamespace = String.Join(".", relativeNamespaceParts);
            string[] finalParts = EnumerableExtensions.Create(productName, areaName, layerName, relativeNamespace, targetName)
                                                      .Where(x => !String.IsNullOrEmpty(x))
                                                      .ToArray();

            string path = String.Join(".", finalParts);
            string absoluteNamespace = targetNamePath != null ? String.Join(".", finalParts.Take(finalParts.Length - 1)) : path;
            return (absoluteNamespace, targetName, path);
        }
    }
}