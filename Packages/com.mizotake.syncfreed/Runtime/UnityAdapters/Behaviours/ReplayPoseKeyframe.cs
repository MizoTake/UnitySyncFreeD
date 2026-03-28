using System;
using UnityEngine;

namespace MizoTake.SyncFreeD.UnityAdapters.Behaviours
{
    [Serializable]
    public struct ReplayPoseKeyframe
    {
        public float TimeSeconds;
        public Vector3 Position;
        public Vector3 Rotation;
        public float FocalLengthMm;
    }
}
