using MizoTake.SyncFreeD.Core.Models;
using UnityEngine;

namespace MizoTake.SyncFreeD.ScriptableObjects
{
    [CreateAssetMenu(menuName = "SyncFreeD/Lens Profile", fileName = "LensProfile")]
    public sealed class LensProfileAsset : ScriptableObject
    {
        public LensProfile Value = new LensProfile();
    }
}
