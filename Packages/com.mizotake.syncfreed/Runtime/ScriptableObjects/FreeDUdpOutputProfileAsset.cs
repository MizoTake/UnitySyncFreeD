using MizoTake.SyncFreeD.Networking;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using UnityEngine;

namespace MizoTake.SyncFreeD.ScriptableObjects
{
    [CreateAssetMenu(menuName = "SyncFreeD/FreeD Output Preset", fileName = "FreeDUdpOutputProfile")]
    public sealed class FreeDUdpOutputProfileAsset : ScriptableObject
    {
        public FreeDUdpOutputProfile Value = new FreeDUdpOutputProfile();
    }

    [System.Serializable]
    public sealed class FreeDUdpOutputProfile
    {
        public PacketSendMode PacketSendMode = PacketSendMode.SingleDestinationUnicast;
        public string DestinationIpAddress = "127.0.0.1";
        public int DestinationPort = 40000;
        public FreeDUdpDestination[] AdditionalDestinations = System.Array.Empty<FreeDUdpDestination>();
        public string MulticastGroupIpAddress = "239.0.0.1";
        public int MulticastPort = 40000;
        public int MulticastTtl = 1;
        public string BindAddress = string.Empty;
        public int SocketBufferSize = 0;
        public int CameraIdFilter = -1;
        public bool JoinMulticastGroup = true;
        public string MulticastInterfaceAddress = string.Empty;
    }
}
