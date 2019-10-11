using System;

namespace Dibix
{
    internal sealed class ObfuscatedProperty
    {
        private readonly Action<object> _deobfuscator;

        public ObfuscatedProperty(Action<object> deobfuscator) => this._deobfuscator = deobfuscator;

        public void DeobfuscateValue(object instance) => this._deobfuscator(instance);
    }
}