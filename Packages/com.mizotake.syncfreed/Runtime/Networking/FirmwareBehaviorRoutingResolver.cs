using MizoTake.SyncFreeD.Core.Models;

namespace MizoTake.SyncFreeD.Networking
{
    public static class FirmwareBehaviorRoutingResolver
    {
        public static PacketSendMode ResolveSendMode(PacketSendMode requestedSendMode, FirmwareBehaviorProfile profile)
        {
            if (profile == null)
            {
                return requestedSendMode;
            }

            switch (requestedSendMode)
            {
                case PacketSendMode.MultiDestinationUnicast:
                    return ResolveMultiDestinationMode(profile);
                case PacketSendMode.Multicast:
                    if (profile.SupportsMulticast)
                    {
                        return PacketSendMode.Multicast;
                    }

                    return ResolveMultiDestinationMode(profile);
                case PacketSendMode.SingleDestinationUnicast:
                default:
                    return PacketSendMode.SingleDestinationUnicast;
            }
        }

        public static int ResolveAdditionalDestinationLimit(PacketSendMode effectiveSendMode, FirmwareBehaviorProfile profile)
        {
            if (effectiveSendMode != PacketSendMode.MultiDestinationUnicast)
            {
                return 0;
            }

            if (profile == null)
            {
                return int.MaxValue;
            }

            return System.Math.Max(0, GetMaxUnicastDestinationCount(profile) - 1);
        }

        private static PacketSendMode ResolveMultiDestinationMode(FirmwareBehaviorProfile profile)
        {
            return profile.SupportsMultiUnicast && GetMaxUnicastDestinationCount(profile) > 1 ? PacketSendMode.MultiDestinationUnicast : PacketSendMode.SingleDestinationUnicast;
        }

        private static int GetMaxUnicastDestinationCount(FirmwareBehaviorProfile profile)
        {
            if (profile == null)
            {
                return int.MaxValue;
            }

            return profile.MaxUnicastDestinationCount > 0 ? profile.MaxUnicastDestinationCount : 1;
        }
    }
}
