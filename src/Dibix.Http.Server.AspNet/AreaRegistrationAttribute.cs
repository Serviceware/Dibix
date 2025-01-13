using System;

namespace Dibix
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class AreaRegistrationAttribute : Attribute
    {
        public string AreaName { get; }

        public AreaRegistrationAttribute(string areaName)
        {
            this.AreaName = areaName;
        }
    }
}