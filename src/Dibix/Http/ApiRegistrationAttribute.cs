using System;

namespace Dibix.Http
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class ApiRegistrationAttribute : Attribute
    {
        public string AreaName { get; }

        public ApiRegistrationAttribute(string areaName)
        {
            this.AreaName = areaName;
        }
    }
}