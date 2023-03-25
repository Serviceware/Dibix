using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Dibix.Http.Server
{
    public interface IHttpRequestDescriptor
    {
        Task<Stream> GetBody();
        IEnumerable<string> GetHeaderValues(string name);
        IEnumerable<string> GetAcceptLanguageValues();
    }
}