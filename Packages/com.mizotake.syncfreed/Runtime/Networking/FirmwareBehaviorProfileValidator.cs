using MizoTake.SyncFreeD.Core.Models;

namespace MizoTake.SyncFreeD.Networking
{
    public static class FirmwareBehaviorProfileValidator
    {
        public static string Validate(FirmwareBehaviorProfile profile, PacketSendMode sendMode)
        {
            if (profile == null)
            {
                return string.Empty;
            }

            switch (sendMode)
            {
                case PacketSendMode.MultiDestinationUnicast:
                    if (!profile.SupportsMultiUnicast)
                    {
                        return "firmware preset では Multi Destination Unicast をサポートしません。";
                    }

                    return profile.MaxUnicastDestinationCount > 1 ? string.Empty : "firmware preset では Multi Destination Unicast は 1 送信先までです。";
                case PacketSendMode.Multicast:
                    return profile.SupportsMulticast ? string.Empty : "firmware preset では Multicast をサポートしません。";
                case PacketSendMode.SingleDestinationUnicast:
                default:
                    return string.Empty;
            }
        }
    }
}
