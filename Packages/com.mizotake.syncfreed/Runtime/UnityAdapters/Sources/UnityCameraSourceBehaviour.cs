using System;
using MizoTake.SyncFreeD.Core.Abstractions;
using MizoTake.SyncFreeD.Core.Models;
using UnityEngine;

namespace MizoTake.SyncFreeD.UnityAdapters.Sources
{
    [Obsolete("Use MizoTake.SyncFreeD.UnityAdapters.Behaviours.UnityCameraSourceBehaviour instead.")]
    [DisallowMultipleComponent]
    public class UnityCameraSourceBehaviour : Behaviours.UnityCameraSourceBehaviour, ICameraSource
    {
        public new bool TryGetObservedState(out CameraObservedFrame frame)
        {
            return TryGetObservedFrame(out frame);
        }
    }
}
