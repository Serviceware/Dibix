using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class UserDefinedTypeProvider : IUserDefinedTypeProvider
    {
        public ICollection<UserDefinedTypeDefinition> Types { get; }

        public UserDefinedTypeProvider(IEnumerable<string> inputs, IErrorReporter errorReporter)
        {
            this.Types = new Collection<UserDefinedTypeDefinition>();

            SqlUserDefinedTypeParser parser = new SqlUserDefinedTypeParser(errorReporter);
            this.Types.AddRange(inputs.Select(x => parser.Parse(x)).Where(x => x != null));
        }
    }
}
