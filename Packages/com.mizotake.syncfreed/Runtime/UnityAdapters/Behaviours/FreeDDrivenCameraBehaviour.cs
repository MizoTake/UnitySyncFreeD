using MizoTake.SyncFreeD.Core.Models;
using UnityEngine;

namespace MizoTake.SyncFreeD.UnityAdapters.Behaviours
{
    [DisallowMultipleComponent]
    public sealed class FreeDDrivenCameraBehaviour : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour sourceBehaviour;
        [SerializeField] private Transform targetTransform;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private bool applyPosition = true;
        [SerializeField] private bool applyRotation = true;
        [SerializeField] private bool applyLens = true;
        [SerializeField] private bool applyToLocalTransform;
        [SerializeField] private bool forcePhysicalCamera = true;
        [SerializeField] private bool applyFocusDistanceToDepthOfField;
        [SerializeField] private Component depthOfFieldTarget;

        public CameraObservedFrame LastAppliedFrame { get; private set; }
        public float LastAppliedFieldOfView { get; private set; }
        public float LastAppliedDepthOfFieldFocusDistance { get; private set; }
        public ICameraFrameProvider SourceProvider => sourceBehaviour as ICameraFrameProvider;

        private void Reset()
        {
            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveReferences();
        }
#endif

        private void LateUpdate()
        {
            ApplyLatestFrame();
        }

        public bool ApplyLatestFrame()
        {
            ResolveReferences();
            var provider = SourceProvider;
            if (provider == null || !provider.TryGetObservedFrame(out var frame))
            {
                return false;
            }

            if (applyPosition && frame.Validity.IsTrackingValid)
            {
                var targetPosition = new Vector3((float)(frame.Pose.Xmm / 1000d), (float)(frame.Pose.Zmm / 1000d), (float)(frame.Pose.Ymm / 1000d));
                if (applyToLocalTransform)
                {
                    targetTransform.localPosition = targetPosition;
                }
                else
                {
                    targetTransform.position = targetPosition;
                }
            }

            if (applyRotation && frame.Validity.IsTrackingValid)
            {
                var targetRotation = Quaternion.Euler((float)(-frame.Pose.TiltDeg), (float)frame.Pose.PanDeg, (float)frame.Pose.RollDeg);
                if (applyToLocalTransform)
                {
                    targetTransform.localRotation = targetRotation;
                }
                else
                {
                    targetTransform.rotation = targetRotation;
                }
            }

            if (applyLens && frame.Validity.IsLensValid && targetCamera != null)
            {
                if (forcePhysicalCamera)
                {
                    targetCamera.usePhysicalProperties = true;
                }

                if (frame.Lens.FocalLengthMm > 0d)
                {
                    targetCamera.focalLength = (float)frame.Lens.FocalLengthMm;
                    LastAppliedFieldOfView = 2f * Mathf.Atan(targetCamera.sensorSize.y / (2f * Mathf.Max(0.001f, targetCamera.focalLength))) * Mathf.Rad2Deg;
                }

                if (frame.Lens.FocusDistanceMeters > 0d)
                {
                    targetCamera.focusDistance = (float)frame.Lens.FocusDistanceMeters;
                    if (applyFocusDistanceToDepthOfField)
                    {
                        var focusDistanceMeters = (float)frame.Lens.FocusDistanceMeters;
                        if (DepthOfFieldFocusApplier.TryApply(depthOfFieldTarget, focusDistanceMeters) || DepthOfFieldFocusApplier.TryApply(gameObject, focusDistanceMeters))
                        {
                            LastAppliedDepthOfFieldFocusDistance = focusDistanceMeters;
                        }
                    }
                }
            }

            LastAppliedFrame = frame;
            return true;
        }

        public bool SetSourceBehaviour(MonoBehaviour behaviour)
        {
            if (behaviour != null && !(behaviour is ICameraFrameProvider))
            {
                return false;
            }

            sourceBehaviour = behaviour;
            return true;
        }

        private void ResolveReferences()
        {
            if (sourceBehaviour == null)
            {
                sourceBehaviour = GetComponent<FreeDInputSourceBehaviour>();
            }

            if (targetTransform == null)
            {
                targetTransform = transform;
            }

            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
            }

            depthOfFieldTarget ??= FindDepthOfFieldTarget();
        }

        private Component FindDepthOfFieldTarget()
        {
            var components = GetComponents<Component>();
            for (var i = 0; i < components.Length; i++)
            {
                var component = components[i];
                if (component != null && component.GetType().Name.Contains("DepthOfField", System.StringComparison.OrdinalIgnoreCase))
                {
                    return component;
                }
            }

            return null;
        }
    }
}
