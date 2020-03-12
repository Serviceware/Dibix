using System.Reflection;

namespace Dibix
{
    internal sealed class TextObfuscationEntityPropertyFormatter : AttributedEntityPropertyFormatter<ObfuscatedAttribute>, IEntityPropertyFormatter
    {
        protected override MethodInfo GetValueFormatterMethod() => typeof(TextObfuscator).GetRuntimeMethod(nameof(TextObfuscator.Deobfuscate), new[] { typeof(string) });
    }
}