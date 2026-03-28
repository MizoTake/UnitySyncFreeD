using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace MizoTake.SyncFreeD.Networking
{
    public static class FreeDUdpNetworkInterfaceUtility
    {
        public static IReadOnlyList<FreeDUdpNetworkInterfaceInfo> GetIPv4Interfaces()
        {
            var interfaces = new List<FreeDUdpNetworkInterfaceInfo>();
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            for (var i = 0; i < networkInterfaces.Length; i++)
            {
                var networkInterface = networkInterfaces[i];
                if (networkInterface.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }

                var properties = networkInterface.GetIPProperties();
                var addresses = properties.UnicastAddresses;
                for (var j = 0; j < addresses.Count; j++)
                {
                    var address = addresses[j];
                    if (address.Address.AddressFamily != AddressFamily.InterNetwork)
                    {
                        continue;
                    }

                    interfaces.Add(new FreeDUdpNetworkInterfaceInfo(networkInterface.Name, address.Address.ToString()));
                }
            }

            return interfaces;
        }
    }
}
