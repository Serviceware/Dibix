using System;
using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ActionParameterConverterRegistry : IActionParameterConverterRegistry
    {
        private readonly ICollection<string> _registrations = new HashSet<string>();

        public void Register(string name)
        {
            if (this._registrations.Contains(name))
                throw new InvalidOperationException($"A converter with the name '{name}' is already registered");

            this._registrations.Add(name);
        }

        public bool IsRegistered(string name) => this._registrations.Contains(name);
    }
}