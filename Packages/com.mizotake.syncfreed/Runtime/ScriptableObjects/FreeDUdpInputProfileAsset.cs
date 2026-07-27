using MizoTake.SyncFreeD.Core.Outputs;
using UnityEngine;

namespace MizoTake.SyncFreeD.ScriptableObjects
{
    [CreateAssetMenu(menuName = "SyncFreeD/FreeD Input Preset", fileName = "FreeDUdpInputProfile")]
    public sealed class FreeDUdpInputProfileAsset : ScriptableObject
    {
        public FreeDUdpInputProfile Value = new FreeDUdpInputProfile();
    }

    [System.Serializable]
    public sealed class FreeDUdpInputProfile
    {
        public int ListenPort = 40000;
        public string BindAddress = string.Empty;
        public bool ValidateChecksum = true;
        public int CameraIdFilter = -1;
        public bool JoinMulticastGroup;
        public string MulticastGroupIpAddress = "239.0.0.1";
        public string MulticastInterfaceAddress = string.Empty;
        public FreeDPacketDecodingPreset PacketDecodingPreset = FreeDPacketDecodingPreset.RawUnsigned24;
        public string BuiltInPacketDecodingProfileId = string.Empty;
        public FreeDPacketDecodingProfile CustomPacketDecodingProfile = new FreeDPacketDecodingProfile();
    }
}
