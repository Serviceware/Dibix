using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Schema;

namespace Dibix.Sdk
{
    internal static class JsonSchemaExtensions
    {
        public static IEnumerable<ValidationError> Flatten(this IEnumerable<ValidationError> errors)
        {
            foreach (ValidationError error in errors.Reverse())
            {
                foreach (ValidationError childError in Flatten(error.ChildErrors))
                {
                    yield return childError;
                }
                yield return error;
            }
        }
    }
}