using System.Net;

namespace Dibix
{
    internal static class HostNameUtility
    {
        public static string GetFullyQualifiedDomainName()
        {
            IPHostEntry dnsEntry;
#if NET
            try
#endif
            {
                dnsEntry = Dns.GetHostEntry("");
            }
#if NET
            catch (System.Net.Sockets.SocketException exception) when (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX) && exception.Message == "nodename nor servname provided, or not known")
            {
                // For some weird reason this happens quite a lot on Azure Pipelines macOS agents...
                return Dns.GetHostName();
            }
#endif
            return dnsEntry.HostName;
        }
    }
}