using System;

namespace Dibix.Http.Server.AspNet
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