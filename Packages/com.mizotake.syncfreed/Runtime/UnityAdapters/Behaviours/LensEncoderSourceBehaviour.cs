using MizoTake.SyncFreeD.Core.Abstractions;
using MizoTake.SyncFreeD.Core.Models;
using UnityEngine;

namespace MizoTake.SyncFreeD.UnityAdapters.Behaviours
{
    [DisallowMultipleComponent]
    public sealed class LensEncoderSourceBehaviour : MonoBehaviour, ILensDataSource
    {
        [SerializeField] private float focalLengthMm = 35f;
        [SerializeField] private float focusDistanceMeters = 2f;
        [SerializeField] private float irisFNumber = 2.8f;
        [SerializeField] private bool lensValid = true;

        public bool TryGetLensState(out LensState lens)
        {
            lens = new LensState
            {
                FocalLengthMm = focalLengthMm,
                FocusDistanceMeters = focusDistanceMeters,
                IrisFNumber = irisFNumber
            };
            return lensValid;
        }
    }
}
