using MizoTake.SyncFreeD.Core.Models;
using UnityEngine;

namespace MizoTake.SyncFreeD.ScriptableObjects
{
    [CreateAssetMenu(menuName = "SyncFreeD/Firmware Behavior Profile", fileName = "FirmwareBehaviorProfile")]
    public sealed class FirmwareBehaviorProfileAsset : ScriptableObject
    {
        public FirmwareBehaviorProfile Value = new FirmwareBehaviorProfile();
    }
}
