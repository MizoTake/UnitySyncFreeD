using MizoTake.SyncFreeD.Core.Models;
using UnityEngine;

namespace MizoTake.SyncFreeD.ScriptableObjects
{
    [CreateAssetMenu(menuName = "SyncFreeD/Device Profile", fileName = "DeviceProfile")]
    public sealed class DeviceProfileAsset : ScriptableObject
    {
        public DeviceProfile Value = new DeviceProfile();
    }
}
