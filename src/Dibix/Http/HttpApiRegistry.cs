using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Dibix.Http
{
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Api")]
    public sealed class HttpApiRegistry : IHttpApiRegistry
    {
        #region Fields
        private readonly ICollection<HttpApiDescriptor> _apis;
        private readonly IDictionary<Assembly, string> _areaNameCache;
        private readonly ICollection<HttpApiDescriptor> _declarativeApis;
        #endregion

        #region Constructor
        public HttpApiRegistry(IEnumerable<Assembly> assemblies)
        {
            this._apis = new Collection<HttpApiDescriptor>();
            this._apis.ReplaceWith(CollectHttpApiDescriptors(assemblies));
            this._areaNameCache = this._apis.GroupBy(x => new { Assembly = GetAssembly(x), x.AreaName }).ToDictionary(x => x.Key.Assembly, x => x.Key.AreaName);
            this._declarativeApis = this._apis.Where(IsDeclarative).ToArray();
        }
        #endregion

        #region IHttpApiRegistry Members
        public string GetAreaName(Assembly assembly)
        {
            if (!this._areaNameCache.TryGetValue(assembly, out string areaName))
                throw new InvalidOperationException(String.Concat("Area not registered for assembly: ", assembly));

            return areaName;
        }

//#pragma warning disable CA1024 // Use properties where appropriate
        public IEnumerable<HttpApiDescriptor> GetCustomApis()
//#pragma warning restore CA1024 // Use properties where appropriate
        {
            return this._declarativeApis;
        }
        #endregion

        #region Private Methods
        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "api", Scope = "member")]
        private static IEnumerable<HttpApiDescriptor> CollectHttpApiDescriptors(IEnumerable<Assembly> assemblies)
        {
            foreach (Assembly assembly in assemblies)
            {
                ApiRegistrationAttribute attribute = assembly.GetCustomAttribute<ApiRegistrationAttribute>();
                if (attribute == null)
                    continue;

                if (String.IsNullOrEmpty(attribute.AreaName))
                    throw new InvalidOperationException($"Area name in api registration cannot be empty: {assembly.GetName().Name}");

                ICollection<Type> types = GetLoadableTypes(assembly).ToArray();

                Type apiDescriptorType = types.FirstOrDefault(typeof(HttpApiDescriptor).GetTypeInfo().IsAssignableFrom);
                HttpApiDescriptor descriptor = apiDescriptorType != null ? (HttpApiDescriptor)Activator.CreateInstance(apiDescriptorType) : new HttpApiRegistration(assembly);
                descriptor.Configure();
                yield return descriptor;
            }
        }

        private static Assembly GetAssembly(HttpApiDescriptor descriptor) => descriptor is HttpApiRegistration registration ? registration.Assembly : descriptor.GetType().GetTypeInfo().Assembly;
        private static bool IsDeclarative(HttpApiDescriptor descriptor) => !(descriptor is HttpApiRegistration);
        #endregion

        #region Private Methods
        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }
        #endregion

        #region Nested Types
        private sealed class HttpApiRegistration : HttpApiDescriptor
        {
            public Assembly Assembly { get; }

            public HttpApiRegistration(Assembly assembly) => this.Assembly = assembly;

            public override void Configure() { }

            protected override string ResolveAreaName(Assembly assembly) => base.ResolveAreaName(this.Assembly);
        }
        #endregion
    }
}