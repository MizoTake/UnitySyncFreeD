using System;
using System.Linq;
using System.Net;

namespace MizoTake.SyncFreeD.Networking
{
    public static class FreeDUdpConfigurationValidator
    {
        public static string Validate(string bindAddress, string multicastGroupIpAddress, string multicastInterfaceAddress, PacketSendMode sendMode)
        {
            if (!string.IsNullOrWhiteSpace(bindAddress) && !IPAddress.TryParse(bindAddress, out _))
            {
                return "Bind Address が不正です。";
            }

            if (sendMode == PacketSendMode.Multicast)
            {
                if (!IPAddress.TryParse(multicastGroupIpAddress, out var groupAddress))
                {
                    return "Multicast Group IP が不正です。";
                }

                var firstByte = groupAddress.GetAddressBytes()[0];
                if (firstByte < 224 || firstByte > 239)
                {
                    return "Multicast Group IP は 224.0.0.0 - 239.255.255.255 の範囲である必要があります。";
                }

                if (!string.IsNullOrWhiteSpace(multicastInterfaceAddress))
                {
                    if (!IPAddress.TryParse(multicastInterfaceAddress, out _))
                    {
                        return "Multicast Interface Address が不正です。";
                    }

                    var interfaces = FreeDUdpNetworkInterfaceUtility.GetIPv4Interfaces();
                    if (interfaces.Count > 0 && !interfaces.Any(info => string.Equals(info.Address, multicastInterfaceAddress, StringComparison.OrdinalIgnoreCase)))
                    {
                        return "Multicast Interface Address がローカル NIC 一覧に見つかりません。";
                    }
                }
            }

            return string.Empty;
        }
    }
}
