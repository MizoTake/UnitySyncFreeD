using System;
using System.Linq;
using System.Net;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;

namespace MizoTake.SyncFreeD.Networking
{
    public static class FreeDUdpConfigurationValidator
    {
        public static string Validate(PacketSendMode sendMode, string bindAddress, string destinationIpAddress, int destinationPort, FreeDUdpDestination[] additionalDestinations, string multicastGroupIpAddress, int multicastPort, string multicastInterfaceAddress, int socketBufferSize, int cameraIdFilter)
        {
            return Validate(sendMode, bindAddress, destinationIpAddress, destinationPort, additionalDestinations, multicastGroupIpAddress, multicastPort, multicastInterfaceAddress, socketBufferSize, cameraIdFilter, FreeDUdpNetworkInterfaceUtility.GetIPv4Interfaces());
        }

        public static string Validate(PacketSendMode sendMode, string bindAddress, string destinationIpAddress, int destinationPort, FreeDUdpDestination[] additionalDestinations, string multicastGroupIpAddress, int multicastPort, string multicastInterfaceAddress, int socketBufferSize, int cameraIdFilter, System.Collections.Generic.IReadOnlyList<FreeDUdpNetworkInterfaceInfo> interfaces)
        {
            if (!string.IsNullOrWhiteSpace(bindAddress) && !IPAddress.TryParse(bindAddress, out _))
            {
                return "Bind Address が不正です。";
            }

            if (!string.IsNullOrWhiteSpace(bindAddress))
            {
                if (interfaces.Count > 0 && !interfaces.Any(info => string.Equals(info.Address, bindAddress, StringComparison.OrdinalIgnoreCase)))
                {
                    return "Bind Address がローカル NIC 一覧に見つかりません。";
                }
            }

            if (socketBufferSize < 0)
            {
                return "Socket Buffer Size は 0 以上である必要があります。";
            }

            if (cameraIdFilter < -1 || cameraIdFilter > 255)
            {
                return "Camera ID Filter は -1 から 255 の範囲である必要があります。";
            }

            if (sendMode == PacketSendMode.SingleDestinationUnicast || sendMode == PacketSendMode.MultiDestinationUnicast)
            {
                if (!IPAddress.TryParse(destinationIpAddress, out _))
                {
                    return "Destination IP Address が不正です。";
                }

                if (!IsValidPort(destinationPort))
                {
                    return "Destination Port は 1 から 65535 の範囲である必要があります。";
                }
            }

            if (sendMode == PacketSendMode.MultiDestinationUnicast && additionalDestinations != null)
            {
                for (var index = 0; index < additionalDestinations.Length; index++)
                {
                    var destination = additionalDestinations[index];
                    if (!destination.Enabled)
                    {
                        continue;
                    }

                    if (!IPAddress.TryParse(destination.IpAddress, out _))
                    {
                        return $"Additional Destination {index + 1} の IP Address が不正です。";
                    }

                    if (!IsValidPort(destination.Port))
                    {
                        return $"Additional Destination {index + 1} の Port は 1 から 65535 の範囲である必要があります。";
                    }
                }
            }

            if (sendMode == PacketSendMode.Multicast)
            {
                if (!IPAddress.TryParse(multicastGroupIpAddress, out var groupAddress))
                {
                    return "Multicast Group IP が不正です。";
                }

                if (!IsValidPort(multicastPort))
                {
                    return "Multicast Port は 1 から 65535 の範囲である必要があります。";
                }

                var firstByte = groupAddress.GetAddressBytes()[0];
                if (firstByte < 224 || firstByte > 239)
                {
                    return "Multicast Group IP は 224.0.0.0 - 239.255.255.255 の範囲である必要があります。";
                }

                if (JoinMulticastRequiresInterfaceSelection(interfaces, multicastInterfaceAddress))
                {
                    return "複数の IPv4 NIC があるため、Multicast Interface Address を明示してください。";
                }

                if (BindAddressRequiresSelection(interfaces, bindAddress))
                {
                    return "複数の IPv4 NIC があるため、Bind Address を明示してください。";
                }

                if (!string.IsNullOrWhiteSpace(multicastInterfaceAddress))
                {
                    if (!IPAddress.TryParse(multicastInterfaceAddress, out _))
                    {
                        return "Multicast Interface Address が不正です。";
                    }

                    if (interfaces.Count > 0 && !interfaces.Any(info => string.Equals(info.Address, multicastInterfaceAddress, StringComparison.OrdinalIgnoreCase)))
                    {
                        return "Multicast Interface Address がローカル NIC 一覧に見つかりません。";
                    }
                }

                if (!string.IsNullOrWhiteSpace(bindAddress) && !string.IsNullOrWhiteSpace(multicastInterfaceAddress) && !string.Equals(bindAddress, multicastInterfaceAddress, StringComparison.OrdinalIgnoreCase))
                {
                    return "Bind Address と Multicast Interface Address は同じ NIC を使う構成を推奨します。";
                }
            }

            return string.Empty;
        }

        private static bool JoinMulticastRequiresInterfaceSelection(System.Collections.Generic.IReadOnlyList<FreeDUdpNetworkInterfaceInfo> interfaces, string multicastInterfaceAddress)
        {
            return interfaces != null && interfaces.Count > 1 && string.IsNullOrWhiteSpace(multicastInterfaceAddress);
        }

        private static bool BindAddressRequiresSelection(System.Collections.Generic.IReadOnlyList<FreeDUdpNetworkInterfaceInfo> interfaces, string bindAddress)
        {
            return interfaces != null && interfaces.Count > 1 && string.IsNullOrWhiteSpace(bindAddress);
        }

        private static bool IsValidPort(int port)
        {
            return port >= 1 && port <= 65535;
        }
    }
}
