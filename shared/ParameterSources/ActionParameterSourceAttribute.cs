using System;

namespace Dibix
{
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class ActionParameterSourceAttribute : Attribute
    {
        public string SourceName { get; }

        public ActionParameterSourceAttribute(string sourceName)
        {
            SourceName = sourceName;
        }
    }
}