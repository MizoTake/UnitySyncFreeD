using MizoTake.SyncFreeD.Core.Models;
using UnityEngine;

namespace MizoTake.SyncFreeD.ScriptableObjects
{
    [CreateAssetMenu(menuName = "SyncFreeD/Mount Profile", fileName = "MountProfile")]
    public sealed class MountProfileAsset : ScriptableObject
    {
        public MountProfile Value = new MountProfile();
    }
}
