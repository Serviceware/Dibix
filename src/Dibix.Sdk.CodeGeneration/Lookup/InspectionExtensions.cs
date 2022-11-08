using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class InspectionExtensions
    {
        public static bool IsArtifactAssembly(this Assembly assembly)
        {
            Guard.IsNotNull(assembly, nameof(assembly));
            try
            {
                return assembly.CustomAttributes.Any(x => x.AttributeType.FullName == "Dibix.ArtifactAssemblyAttribute");
            }
            catch (FileNotFoundException)
            {
                return false;
            }
            catch (TypeLoadException)
            {
                return false;
            }
        }

        public static bool IsDatabaseAccessor(this Type type) => type.CustomAttributes.Any(x => x.AttributeType.FullName == "Dibix.DatabaseAccessorAttribute");

        public static void CollectErrorResponses(this MethodInfo method, Action<int, int, string> errorResponseMatchHandler)
        {
            foreach (CustomAttributeData attribute in method.GetCustomAttributesData())
            {
                if (attribute.AttributeType.FullName != "Dibix.Http.Server.ErrorResponseAttribute")
                    continue;

                int statusCode = (int)attribute.ConstructorArguments[0].Value;
                int errorCode = (int)attribute.ConstructorArguments[1].Value;
                string errorDescription = (string)attribute.ConstructorArguments[2].Value;
                errorResponseMatchHandler(statusCode, errorCode, errorDescription);
            }
        }

        public static string GetUdtName(this Type type)
        {
            return type.CustomAttributes
                       .Where(x => x.AttributeType.FullName == "Dibix.StructuredTypeAttribute")
                       .Select(x => (string)x.ConstructorArguments.Select(y => y.Value).Single())
                       .FirstOrDefault();
        }

        public static IEnumerable<ParameterInfo> GetExternalParameters(this MethodInfo method, bool isAsync)
        {
            IList<ParameterInfo> parameters = method.GetParameters().ToList();
            if (parameters[0].ParameterType.FullName == "Dibix.IDatabaseAccessorFactory")
                parameters.RemoveAt(0);

            int lastIndex = parameters.Count - 1;
            if (isAsync && parameters[lastIndex].ParameterType == typeof(CancellationToken))
                parameters.RemoveAt(lastIndex);

            return parameters;
        }
    }
}