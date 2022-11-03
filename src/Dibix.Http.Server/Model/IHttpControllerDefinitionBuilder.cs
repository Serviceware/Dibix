using System;

namespace Dibix.Http.Server
{
    public interface IHttpControllerDefinitionBuilder
    {
        void AddAction(IHttpActionTarget target, Action<IHttpActionDefinitionBuilder> setupAction);
        void Import(string fullControllerTypeName);
    }
}