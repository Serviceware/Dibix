using System.Diagnostics;
using System.Net;

namespace Dibix
{
    internal static class HostNameUtility
    {
        public static string GetFullyQualifiedDomainName()
        {
            IPHostEntry dnsEntry;
#if NET || NETSTANDARD
            try
#endif
            {
                dnsEntry = Dns.GetHostEntry("");
            }
#if NET || NETSTANDARD
            // For some weird reason this happens quite a lot on Azure Pipelines macOS agents...
            catch (System.Net.Sockets.SocketException exception) when (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX) && exception.Message == "nodename nor servname provided, or not known")
            {
                Debug.WriteLine($"Dns.GetHostEntry threw exception: {exception.Message}");
                string hostName = Dns.GetHostName();
                Debug.WriteLine($"Using Dns.GetHostName: {hostName}");
                return hostName;
            }
#endif
            return dnsEntry.HostName;
        }
    }
}