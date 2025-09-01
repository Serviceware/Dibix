using System;
using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ActionParameterConverterRegistry : IActionParameterConverterRegistry
    {
        private readonly IDictionary<string, ActionParameterConverterRegistration> _registrations = new Dictionary<string, ActionParameterConverterRegistration>();

        public void Register(string name, IEnumerable<string> requiredClaims)
        {
            if (_registrations.ContainsKey(name))
                throw new InvalidOperationException($"A converter with the name '{name}' is already registered");

            _registrations.Add(name, new ActionParameterConverterRegistration(name, requiredClaims));
        }

        public bool IsRegistered(string name) => _registrations.ContainsKey(name);

        public ActionParameterConverterRegistration GetRegistration(string converterName) => _registrations[converterName];
    }
}