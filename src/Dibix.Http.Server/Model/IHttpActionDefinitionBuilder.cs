using System;

namespace Dibix.Http.Server
{
    public interface IHttpActionDefinitionBuilder : IHttpActionBuilderBase
    {
        HttpApiMethod Method { get; set; }
        string Description { get; set; }
        string ChildRoute { get; set; }
        Type BodyContract { get; set; }
        Type BodyBinder { get; set; }
        bool IsAnonymous { get; set; }
        HttpFileResponseDefinition FileResponse { get; set; }

        void WithAuthorization(IHttpActionTarget target, Action<IHttpAuthorizationBuilder> setupAction);
    }
}