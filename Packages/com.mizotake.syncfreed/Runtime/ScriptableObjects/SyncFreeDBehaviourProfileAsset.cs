using MizoTake.SyncFreeD.Core.Models;
using UnityEngine;

namespace MizoTake.SyncFreeD.ScriptableObjects
{
    [CreateAssetMenu(menuName = "SyncFreeD/SyncFreeD Behaviour Preset", fileName = "SyncFreeDBehaviourProfile")]
    public sealed class SyncFreeDBehaviourProfileAsset : ScriptableObject
    {
        public SyncFreeDBehaviourProfile Value = new SyncFreeDBehaviourProfile();
    }

    [System.Serializable]
    public sealed class SyncFreeDBehaviourProfile
    {
        public SyncMode SyncMode = SyncMode.VirtualMaster;
        public OutputPoseKind OutputPoseKind = OutputPoseKind.Corrected;
        public OutputTickMode OutputTickMode = OutputTickMode.LateUpdate;
        public int FixedIntervalMs = 33;
        public bool LogPacketHex;
        public SyncTuningProfileAsset TuningProfileAsset;
        public SyncTuningProfile Tuning = new SyncTuningProfile();
        public DeviceProfileAsset DeviceProfileAsset;
        public FirmwareBehaviorProfileAsset FirmwareBehaviorProfileAsset;
        public LensProfileAsset LensProfileAsset;
        public MountProfileAsset MountProfileAsset;
    }
}
