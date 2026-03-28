using MizoTake.SyncFreeD.Core.Models;
using UnityEngine;

namespace MizoTake.SyncFreeD.ScriptableObjects
{
    [CreateAssetMenu(menuName = "SyncFreeD/Sync Tuning Profile", fileName = "SyncTuningProfile")]
    public sealed class SyncTuningProfileAsset : ScriptableObject
    {
        public SyncTuningProfile Value = new SyncTuningProfile();
    }
}
