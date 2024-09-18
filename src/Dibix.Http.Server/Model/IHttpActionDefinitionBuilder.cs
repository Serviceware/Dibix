using System;
using System.Collections.Generic;

namespace Dibix.Http.Server
{
    public interface IHttpActionDefinitionBuilder : IHttpActionBuilderBase
    {
        string ActionName { get; set; }
        string RelativeNamespace { get; set; }
        HttpApiMethod Method { get; set; }
        string Description { get; set; }
        string ChildRoute { get; set; }
        Type BodyContract { get; set; }
        Type BodyBinder { get; set; }
        ICollection<string> SecuritySchemes { get; }
        HttpFileResponseDefinition FileResponse { get; set; }

        void DisableStatusCodeDetection(int statusCode);
        void SetStatusCodeDetectionResponse(int statusCode, int errorCode, string errorMessage);
        void AddAuthorizationBehavior(IHttpActionTarget target, Action<IHttpAuthorizationBuilder> setupAction);
        void RegisterDelegate(Delegate @delegate);
    }
}