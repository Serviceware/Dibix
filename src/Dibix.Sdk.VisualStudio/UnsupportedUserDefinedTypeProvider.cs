using System;
using System.Collections.Generic;
using Dibix.Sdk.CodeGeneration;

namespace Dibix.Sdk.VisualStudio
{
    internal sealed class UnsupportedUserDefinedTypeProvider : IUserDefinedTypeProvider
    {
        public IEnumerable<UserDefinedTypeSchema> Types { get { yield break; } }
        public IEnumerable<SchemaDefinition> Schemas => throw new NotSupportedException();
    }
}