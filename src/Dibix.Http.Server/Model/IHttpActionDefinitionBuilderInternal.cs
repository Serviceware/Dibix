namespace Dibix.Http.Server
{
    public interface IHttpActionDefinitionBuilderInternal
    {
        void RegisterRequiredClaim(string claimType);
        void AddParameterDescription(string parameterName, string description);
    }
}