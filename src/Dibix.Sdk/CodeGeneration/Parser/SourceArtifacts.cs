﻿using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class SourceArtifacts
    {
        public IList<SqlStatementInfo> Statements { get; }
        public IList<UserDefinedTypeDefinition> UserDefinedTypes { get; }
        public ICollection<ContractDefinition> Contracts { get; }
        public ICollection<ControllerDefinition> Controllers { get; }

        public SourceArtifacts()
        {
            this.Statements = new Collection<SqlStatementInfo>();
            this.UserDefinedTypes = new Collection<UserDefinedTypeDefinition>();
            this.Contracts = new Collection<ContractDefinition>();
            this.Controllers = new Collection<ControllerDefinition>();
        }
    }
}
