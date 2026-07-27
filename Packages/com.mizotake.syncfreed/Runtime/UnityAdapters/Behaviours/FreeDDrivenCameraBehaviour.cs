using MizoTake.SyncFreeD.Core.Models;
using UnityEngine;

namespace MizoTake.SyncFreeD.UnityAdapters.Behaviours
{
    public enum FreeDRotationApplicationMode
    {
        InstallationRelative = 0,
        Absolute = 1
    }

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
        [SerializeField] private FreeDRotationApplicationMode rotationApplicationMode = FreeDRotationApplicationMode.InstallationRelative;
        [SerializeField] private bool forcePhysicalCamera = true;
        [SerializeField] private bool applySensorSize = true;
        [SerializeField] private Transform panAxis;
        [SerializeField] private Transform tiltAxis;
        [SerializeField] private Transform rollAxis;
        [SerializeField] private bool enableMotionSmoothing;
        [SerializeField] [Min(0f)] private float positionSmoothingHalfLifeSeconds = 0.04f;
        [SerializeField] [Min(0f)] private float rotationSmoothingHalfLifeSeconds = 0.04f;
        [SerializeField] [Min(0f)] private float lensSmoothingHalfLifeSeconds = 0.06f;
        [SerializeField] private bool applyFocusDistanceToDepthOfField;
        [SerializeField] private Component depthOfFieldTarget;

        private bool initialPoseCaptured;
        private Quaternion initialTargetLocalRotation;
        private Quaternion initialTargetWorldRotation;
        private Quaternion initialPanLocalRotation;
        private Quaternion initialTiltLocalRotation;
        private Quaternion initialRollLocalRotation;
        private Vector2 initialCameraSensorSize;
        private float initialCameraFocalLength;
        private float initialCameraFocusDistance;
        private bool initialCameraUsePhysicalProperties;
        private bool positionSmoothingInitialized;
        private bool combinedRotationSmoothingInitialized;
        private bool panSmoothingInitialized;
        private bool tiltSmoothingInitialized;
        private bool rollSmoothingInitialized;
        private bool zoomSmoothingInitialized;
        private bool focusSmoothingInitialized;
        private bool smoothingIdentityInitialized;
        private string smoothingSourceId = string.Empty;
        private int smoothingCameraId;

        public CameraObservedFrame LastAppliedFrame { get; private set; }
        public float LastAppliedFieldOfView { get; private set; }
        public float LastAppliedDepthOfFieldFocusDistance { get; private set; }
        public string LastRigError { get; private set; } = string.Empty;
        public ICameraFrameProvider SourceProvider => sourceBehaviour as ICameraFrameProvider;
        public bool MotionSmoothingEnabled => enableMotionSmoothing;
        public float PositionSmoothingHalfLifeSeconds => positionSmoothingHalfLifeSeconds;
        public float RotationSmoothingHalfLifeSeconds => rotationSmoothingHalfLifeSeconds;
        public float LensSmoothingHalfLifeSeconds => lensSmoothingHalfLifeSeconds;

        private void Reset()
        {
            ResolveReferences();
            CaptureInitialPose();
        }

        private void Awake()
        {
            ResolveReferences();
            CaptureInitialPose();
        }

        private void OnEnable()
        {
            ResolveReferences();
            CaptureInitialPose();
        }

        private void OnDisable()
        {
            ResetMotionSmoothing();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            positionSmoothingHalfLifeSeconds = SanitizeHalfLife(positionSmoothingHalfLifeSeconds);
            rotationSmoothingHalfLifeSeconds = SanitizeHalfLife(rotationSmoothingHalfLifeSeconds);
            lensSmoothingHalfLifeSeconds = SanitizeHalfLife(lensSmoothingHalfLifeSeconds);
            ResolveReferences();
        }
#endif

        private void LateUpdate()
        {
            ApplyLatestFrame(Time.unscaledDeltaTime);
        }

        public bool ApplyLatestFrame()
        {
            return ApplyLatestFrame(Time.unscaledDeltaTime);
        }

        public bool ApplyLatestFrame(float deltaTimeSeconds)
        {
            ResolveReferences();
            CaptureInitialPose();
            var provider = SourceProvider;
            if (provider == null || !provider.TryGetObservedFrame(out var frame))
            {
                return false;
            }

            EnsureMotionSmoothingIdentity(frame);

            if (applyPosition && frame.Validity.IsTrackingValid && HasCapability(frame, CameraCapabilities.Position))
            {
                var targetPosition = new Vector3((float)(frame.Pose.Xmm / 1000d), (float)(frame.Pose.Zmm / 1000d), (float)(frame.Pose.Ymm / 1000d));
                if (applyToLocalTransform)
                {
                    targetTransform.localPosition = SmoothPosition(targetTransform.localPosition, targetPosition, deltaTimeSeconds);
                }
                else
                {
                    targetTransform.position = SmoothPosition(targetTransform.position, targetPosition, deltaTimeSeconds);
                }
            }

            if (applyRotation && frame.Validity.IsTrackingValid)
            {
                if (HasCapability(frame, CameraCapabilities.PanTilt | CameraCapabilities.Roll) && !TryApplyPtzRigRotation(frame, deltaTimeSeconds))
                {
                    ApplyCombinedRotation(frame, deltaTimeSeconds);
                }
            }

            if (applyLens && targetCamera != null)
            {
                var hasZoom = HasCapability(frame, CameraCapabilities.Zoom);
                var hasFocus = HasCapability(frame, CameraCapabilities.Focus);
                if (!hasZoom)
                {
                    RestoreInitialZoomState();
                }
                else if (frame.Validity.IsLensValid)
                {
                    ApplyZoom(frame, deltaTimeSeconds);
                }

                if (!hasFocus)
                {
                    RestoreInitialFocusState();
                }
                else if (frame.Validity.IsLensValid && frame.Lens.FocusDistanceMeters > 0d)
                {
                    ApplyFocusDistance((float)frame.Lens.FocusDistanceMeters, deltaTimeSeconds);
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

            if (sourceBehaviour != behaviour)
            {
                sourceBehaviour = behaviour;
                ResetMotionSmoothing();
            }
            return true;
        }

        public void SetMotionSmoothing(bool enabled, float positionHalfLifeSeconds, float rotationHalfLifeSeconds, float lensHalfLifeSeconds)
        {
            enableMotionSmoothing = enabled;
            positionSmoothingHalfLifeSeconds = SanitizeHalfLife(positionHalfLifeSeconds);
            rotationSmoothingHalfLifeSeconds = SanitizeHalfLife(rotationHalfLifeSeconds);
            lensSmoothingHalfLifeSeconds = SanitizeHalfLife(lensHalfLifeSeconds);
        }

        public void ResetMotionSmoothing()
        {
            ResetMotionSmoothingChannels();
            smoothingIdentityInitialized = false;
            smoothingSourceId = string.Empty;
            smoothingCameraId = 0;
        }

        private void ResetMotionSmoothingChannels()
        {
            positionSmoothingInitialized = false;
            combinedRotationSmoothingInitialized = false;
            panSmoothingInitialized = false;
            tiltSmoothingInitialized = false;
            rollSmoothingInitialized = false;
            zoomSmoothingInitialized = false;
            focusSmoothingInitialized = false;
        }

        public void RecaptureRigRestPose()
        {
            initialPoseCaptured = false;
            ResetMotionSmoothing();
            ResolveReferences();
            CaptureInitialPose();
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

        private void CaptureInitialPose()
        {
            if (initialPoseCaptured || targetTransform == null)
            {
                return;
            }

            initialTargetLocalRotation = targetTransform.localRotation;
            initialTargetWorldRotation = targetTransform.rotation;
            initialPanLocalRotation = panAxis != null ? panAxis.localRotation : Quaternion.identity;
            initialTiltLocalRotation = tiltAxis != null ? tiltAxis.localRotation : Quaternion.identity;
            initialRollLocalRotation = rollAxis != null ? rollAxis.localRotation : Quaternion.identity;
            if (targetCamera != null)
            {
                initialCameraSensorSize = targetCamera.sensorSize;
                initialCameraFocalLength = targetCamera.focalLength;
                initialCameraFocusDistance = targetCamera.focusDistance;
                initialCameraUsePhysicalProperties = targetCamera.usePhysicalProperties;
            }
            initialPoseCaptured = true;
        }

        private bool TryApplyPtzRigRotation(in CameraObservedFrame frame, float deltaTimeSeconds)
        {
            var hasPanTilt = HasCapability(frame, CameraCapabilities.PanTilt);
            var hasRoll = HasCapability(frame, CameraCapabilities.Roll);
            var hasAnyConfiguredAxis = panAxis != null || tiltAxis != null || rollAxis != null;
            if (!hasAnyConfiguredAxis)
            {
                LastRigError = string.Empty;
                return false;
            }

            if ((hasPanTilt && (panAxis == null || tiltAxis == null)) || (hasRoll && rollAxis == null))
            {
                LastRigError = "The configured PTZ rig does not provide every axis required by the current packet profile. Combined rotation fallback was used.";
                return false;
            }

            if ((hasPanTilt && panAxis == tiltAxis) || (hasRoll && (rollAxis == panAxis || rollAxis == tiltAxis)))
            {
                LastRigError = "Each configured PTZ rig axis must use a distinct Transform. Combined rotation fallback was used.";
                return false;
            }

            LastRigError = string.Empty;
            if (hasPanTilt)
            {
                var panBase = rotationApplicationMode == FreeDRotationApplicationMode.InstallationRelative ? initialPanLocalRotation : Quaternion.identity;
                var tiltBase = rotationApplicationMode == FreeDRotationApplicationMode.InstallationRelative ? initialTiltLocalRotation : Quaternion.identity;
                panAxis.localRotation = SmoothRotation(panAxis.localRotation, panBase * Quaternion.Euler(0f, (float)frame.Pose.PanDeg, 0f), ref panSmoothingInitialized, deltaTimeSeconds);
                tiltAxis.localRotation = SmoothRotation(tiltAxis.localRotation, tiltBase * Quaternion.Euler((float)-frame.Pose.TiltDeg, 0f, 0f), ref tiltSmoothingInitialized, deltaTimeSeconds);
            }

            if (hasRoll)
            {
                var rollBase = rotationApplicationMode == FreeDRotationApplicationMode.InstallationRelative ? initialRollLocalRotation : Quaternion.identity;
                rollAxis.localRotation = SmoothRotation(rollAxis.localRotation, rollBase * Quaternion.Euler(0f, 0f, (float)frame.Pose.RollDeg), ref rollSmoothingInitialized, deltaTimeSeconds);
            }

            return true;
        }

        private void ApplyCombinedRotation(in CameraObservedFrame frame, float deltaTimeSeconds)
        {
            var hasPanTilt = HasCapability(frame, CameraCapabilities.PanTilt);
            var hasRoll = HasCapability(frame, CameraCapabilities.Roll);
            var rotationOffset = Quaternion.Euler(hasPanTilt ? (float)-frame.Pose.TiltDeg : 0f, hasPanTilt ? (float)frame.Pose.PanDeg : 0f, hasRoll ? (float)frame.Pose.RollDeg : 0f);
            if (rotationApplicationMode == FreeDRotationApplicationMode.Absolute)
            {
                if (applyToLocalTransform)
                {
                    targetTransform.localRotation = SmoothRotation(targetTransform.localRotation, rotationOffset, ref combinedRotationSmoothingInitialized, deltaTimeSeconds);
                }
                else
                {
                    targetTransform.rotation = SmoothRotation(targetTransform.rotation, rotationOffset, ref combinedRotationSmoothingInitialized, deltaTimeSeconds);
                }
                return;
            }

            if (applyToLocalTransform)
            {
                targetTransform.localRotation = SmoothRotation(targetTransform.localRotation, initialTargetLocalRotation * rotationOffset, ref combinedRotationSmoothingInitialized, deltaTimeSeconds);
            }
            else
            {
                targetTransform.rotation = SmoothRotation(targetTransform.rotation, initialTargetWorldRotation * rotationOffset, ref combinedRotationSmoothingInitialized, deltaTimeSeconds);
            }
        }

        private void ApplyZoom(in CameraObservedFrame frame, float deltaTimeSeconds)
        {
            if (forcePhysicalCamera)
            {
                targetCamera.usePhysicalProperties = true;
            }

            if (applySensorSize)
            {
                targetCamera.sensorSize = frame.Projection.SensorWidthMm > 0d && frame.Projection.SensorHeightMm > 0d ? new Vector2((float)frame.Projection.SensorWidthMm, (float)frame.Projection.SensorHeightMm) : initialCameraSensorSize;
            }

            var projectionFocalLengthMm = frame.Lens.EffectiveFocalLengthMm > 0d ? frame.Lens.EffectiveFocalLengthMm : frame.Lens.FocalLengthMm;
            var targetFocalLength = projectionFocalLengthMm > 0d ? (float)projectionFocalLengthMm : initialCameraFocalLength;
            targetCamera.focalLength = SmoothScalar(targetCamera.focalLength, targetFocalLength, ref zoomSmoothingInitialized, lensSmoothingHalfLifeSeconds, deltaTimeSeconds);
            LastAppliedFieldOfView = 2f * Mathf.Atan(targetCamera.sensorSize.y / (2f * Mathf.Max(0.001f, targetCamera.focalLength))) * Mathf.Rad2Deg;
        }

        private void RestoreInitialZoomState()
        {
            targetCamera.usePhysicalProperties = initialCameraUsePhysicalProperties;
            if (applySensorSize)
            {
                targetCamera.sensorSize = initialCameraSensorSize;
            }
            targetCamera.focalLength = initialCameraFocalLength;
            zoomSmoothingInitialized = false;
            LastAppliedFieldOfView = 2f * Mathf.Atan(targetCamera.sensorSize.y / (2f * Mathf.Max(0.001f, targetCamera.focalLength))) * Mathf.Rad2Deg;
        }

        private void ApplyFocusDistance(float focusDistanceMeters, float deltaTimeSeconds)
        {
            var smoothedFocusDistanceMeters = SmoothScalar(targetCamera.focusDistance, focusDistanceMeters, ref focusSmoothingInitialized, lensSmoothingHalfLifeSeconds, deltaTimeSeconds);
            targetCamera.focusDistance = smoothedFocusDistanceMeters;
            if (applyFocusDistanceToDepthOfField && (DepthOfFieldFocusApplier.TryApply(depthOfFieldTarget, smoothedFocusDistanceMeters) || DepthOfFieldFocusApplier.TryApply(gameObject, smoothedFocusDistanceMeters)))
            {
                LastAppliedDepthOfFieldFocusDistance = smoothedFocusDistanceMeters;
            }
        }

        private void RestoreInitialFocusState()
        {
            targetCamera.focusDistance = initialCameraFocusDistance;
            focusSmoothingInitialized = false;
            if (applyFocusDistanceToDepthOfField && (DepthOfFieldFocusApplier.TryApply(depthOfFieldTarget, initialCameraFocusDistance) || DepthOfFieldFocusApplier.TryApply(gameObject, initialCameraFocusDistance)))
            {
                LastAppliedDepthOfFieldFocusDistance = initialCameraFocusDistance;
            }
        }

        private Vector3 SmoothPosition(Vector3 current, Vector3 target, float deltaTimeSeconds)
        {
            if (!positionSmoothingInitialized)
            {
                positionSmoothingInitialized = true;
                return target;
            }

            return Vector3.LerpUnclamped(current, target, CalculateSmoothingFactor(positionSmoothingHalfLifeSeconds, deltaTimeSeconds));
        }

        private Quaternion SmoothRotation(Quaternion current, Quaternion target, ref bool initialized, float deltaTimeSeconds)
        {
            if (!initialized)
            {
                initialized = true;
                return target;
            }

            return Quaternion.SlerpUnclamped(current, target, CalculateSmoothingFactor(rotationSmoothingHalfLifeSeconds, deltaTimeSeconds));
        }

        private float SmoothScalar(float current, float target, ref bool initialized, float halfLifeSeconds, float deltaTimeSeconds)
        {
            if (!initialized)
            {
                initialized = true;
                return target;
            }

            return Mathf.LerpUnclamped(current, target, CalculateSmoothingFactor(halfLifeSeconds, deltaTimeSeconds));
        }

        private float CalculateSmoothingFactor(float halfLifeSeconds, float deltaTimeSeconds)
        {
            if (!enableMotionSmoothing || halfLifeSeconds <= 0f || float.IsNaN(halfLifeSeconds) || float.IsInfinity(halfLifeSeconds))
            {
                return 1f;
            }

            var finiteDeltaTimeSeconds = float.IsNaN(deltaTimeSeconds) || float.IsInfinity(deltaTimeSeconds) ? 0f : Mathf.Max(0f, deltaTimeSeconds);
            return 1f - Mathf.Pow(0.5f, finiteDeltaTimeSeconds / halfLifeSeconds);
        }

        private void EnsureMotionSmoothingIdentity(in CameraObservedFrame frame)
        {
            if (smoothingIdentityInitialized && smoothingCameraId == frame.CameraId && string.Equals(smoothingSourceId, frame.SourceId, System.StringComparison.Ordinal))
            {
                return;
            }

            ResetMotionSmoothingChannels();
            smoothingIdentityInitialized = true;
            smoothingSourceId = frame.SourceId ?? string.Empty;
            smoothingCameraId = frame.CameraId;
        }

        private static float SanitizeHalfLife(float halfLifeSeconds)
        {
            return float.IsNaN(halfLifeSeconds) || float.IsInfinity(halfLifeSeconds) ? 0f : Mathf.Max(0f, halfLifeSeconds);
        }

        private static bool HasCapability(in CameraObservedFrame frame, CameraCapabilities capability)
        {
            return (frame.Capabilities & capability) != 0;
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
