using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class UserDefinedTypeProvider : IUserDefinedTypeProvider
    {
        private readonly ICollection<UserDefinedTypeDefinition> _types;

        public IEnumerable<UserDefinedTypeDefinition> Types => this._types;

        public UserDefinedTypeProvider(IEnumerable<string> inputs)
        {
            SqlUserDefinedTypeParser parser = new SqlUserDefinedTypeParser();
            foreach (string input in inputs)
            {
                parser.Parse(input);
            }
        }
    }
}
