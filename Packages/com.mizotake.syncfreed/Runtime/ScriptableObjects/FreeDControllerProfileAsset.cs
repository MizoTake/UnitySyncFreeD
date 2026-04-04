using UnityEngine;

namespace MizoTake.SyncFreeD.ScriptableObjects
{
    [CreateAssetMenu(menuName = "SyncFreeD/FreeD Controller Preset", fileName = "FreeDControllerProfile")]
    public sealed class FreeDControllerProfileAsset : ScriptableObject
    {
        public FreeDControllerProfile Value = new FreeDControllerProfile();
    }

    [System.Serializable]
    public sealed class FreeDControllerProfile
    {
        public bool AllowKeyboardControl = true;
        public bool UseUnscaledTime = true;
        public float MoveSpeedMetersPerSecond = 2f;
        public float RotateSpeedDegreesPerSecond = 60f;
        public float RollSpeedDegreesPerSecond = 45f;
        public float FocalLengthSpeedMmPerSecond = 20f;
        public float FocusDistanceSpeedMetersPerSecond = 1f;
        public float BoostMultiplier = 3f;
    }
}
