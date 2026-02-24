// ReSharper disable RedundantNameQualifier
// ReSharper disable UnusedMember.Global
namespace Dibix.Testing.Generators
{
    [global::System.AttributeUsage(global::System.AttributeTargets.Method)]
    internal sealed class EndpointAttribute : global::System.Attribute
    {
        public bool WithAuthorization { get; set; }
        public bool Anonymous { get; set; }
#pragma warning disable CS8618
        public string ActionName { get; set; }
#pragma warning restore CS8618
    }
}