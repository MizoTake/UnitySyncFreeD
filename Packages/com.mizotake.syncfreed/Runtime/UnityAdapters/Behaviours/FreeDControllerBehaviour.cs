using UnityEngine;
using MizoTake.SyncFreeD.ScriptableObjects;

namespace MizoTake.SyncFreeD.UnityAdapters.Behaviours
{
    [DisallowMultipleComponent]
    public sealed class FreeDControllerBehaviour : MonoBehaviour
    {
        [SerializeField] private Transform controlledTransform;
        [SerializeField] private Camera controlledCamera;
        [SerializeField] private FreeDControllerProfileAsset profileAsset;
        [SerializeField] private bool applyProfileOnAwake = true;
        [SerializeField] private bool allowKeyboardControl = true;
        [SerializeField] private bool useUnscaledTime = true;
        [SerializeField] private float moveSpeedMetersPerSecond = 2f;
        [SerializeField] private float rotateSpeedDegreesPerSecond = 60f;
        [SerializeField] private float rollSpeedDegreesPerSecond = 45f;
        [SerializeField] private float focalLengthSpeedMmPerSecond = 20f;
        [SerializeField] private float focusDistanceSpeedMetersPerSecond = 1f;
        [SerializeField] private float boostMultiplier = 3f;

        private Vector3 initialLocalPosition;
        private Quaternion initialLocalRotation;
        private float initialFocalLength;
        private float initialFocusDistance;

        public Transform ControlledTransform => controlledTransform != null ? controlledTransform : transform;
        public Camera ControlledCamera => controlledCamera != null ? controlledCamera : GetComponent<Camera>();
        public FreeDControllerProfileAsset ProfileAsset => profileAsset;
        public bool ApplyProfileOnAwake => applyProfileOnAwake;
        public bool AllowKeyboardControl => allowKeyboardControl;
        public bool UseUnscaledTime => useUnscaledTime;
        public float MoveSpeedMetersPerSecond => moveSpeedMetersPerSecond;
        public float RotateSpeedDegreesPerSecond => rotateSpeedDegreesPerSecond;
        public float RollSpeedDegreesPerSecond => rollSpeedDegreesPerSecond;
        public float FocalLengthSpeedMmPerSecond => focalLengthSpeedMmPerSecond;
        public float FocusDistanceSpeedMetersPerSecond => focusDistanceSpeedMetersPerSecond;
        public float BoostMultiplier => boostMultiplier;

        private void Reset()
        {
            ResolveReferences();
            if (applyProfileOnAwake)
            {
                ApplyProfile();
            }
            CaptureInitialState();
        }

        private void Awake()
        {
            ResolveReferences();
            if (applyProfileOnAwake)
            {
                ApplyProfile();
            }
            CaptureInitialState();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveReferences();
            if (applyProfileOnAwake)
            {
                ApplyProfile();
            }
        }
#endif

        private void Update()
        {
            if (!allowKeyboardControl)
            {
                return;
            }

            var deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            var moveMultiplier = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? boostMultiplier : 1f;
            var localMove = Vector3.zero;
            if (Input.GetKey(KeyCode.W))
            {
                localMove += Vector3.forward;
            }

            if (Input.GetKey(KeyCode.S))
            {
                localMove += Vector3.back;
            }

            if (Input.GetKey(KeyCode.A))
            {
                localMove += Vector3.left;
            }

            if (Input.GetKey(KeyCode.D))
            {
                localMove += Vector3.right;
            }

            if (Input.GetKey(KeyCode.E))
            {
                localMove += Vector3.up;
            }

            if (Input.GetKey(KeyCode.Q))
            {
                localMove += Vector3.down;
            }

            var rotationDelta = Vector3.zero;
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                rotationDelta.y -= rotateSpeedDegreesPerSecond * deltaTime * moveMultiplier;
            }

            if (Input.GetKey(KeyCode.RightArrow))
            {
                rotationDelta.y += rotateSpeedDegreesPerSecond * deltaTime * moveMultiplier;
            }

            if (Input.GetKey(KeyCode.UpArrow))
            {
                rotationDelta.x -= rotateSpeedDegreesPerSecond * deltaTime * moveMultiplier;
            }

            if (Input.GetKey(KeyCode.DownArrow))
            {
                rotationDelta.x += rotateSpeedDegreesPerSecond * deltaTime * moveMultiplier;
            }

            if (Input.GetKey(KeyCode.Z))
            {
                rotationDelta.z -= rollSpeedDegreesPerSecond * deltaTime * moveMultiplier;
            }

            if (Input.GetKey(KeyCode.X))
            {
                rotationDelta.z += rollSpeedDegreesPerSecond * deltaTime * moveMultiplier;
            }

            var focalLengthDelta = 0f;
            if (Input.GetKey(KeyCode.PageUp))
            {
                focalLengthDelta += focalLengthSpeedMmPerSecond * deltaTime * moveMultiplier;
            }

            if (Input.GetKey(KeyCode.PageDown))
            {
                focalLengthDelta -= focalLengthSpeedMmPerSecond * deltaTime * moveMultiplier;
            }

            var focusDistanceDelta = 0f;
            if (Input.GetKey(KeyCode.Home))
            {
                focusDistanceDelta += focusDistanceSpeedMetersPerSecond * deltaTime * moveMultiplier;
            }

            if (Input.GetKey(KeyCode.End))
            {
                focusDistanceDelta -= focusDistanceSpeedMetersPerSecond * deltaTime * moveMultiplier;
            }

            if (localMove != Vector3.zero)
            {
                ApplyTranslation(localMove.normalized * moveSpeedMetersPerSecond * deltaTime * moveMultiplier);
            }

            if (rotationDelta != Vector3.zero)
            {
                ApplyRotation(rotationDelta);
            }

            if (focalLengthDelta != 0f || focusDistanceDelta != 0f)
            {
                ApplyLensDelta(focalLengthDelta, focusDistanceDelta);
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetPoseAndLens();
            }
        }

        public void ApplyTranslation(Vector3 localTranslationMeters)
        {
            ControlledTransform.Translate(localTranslationMeters, Space.Self);
        }

        public void SetProfileAsset(FreeDControllerProfileAsset asset, bool applyImmediately)
        {
            profileAsset = asset;
            if (applyImmediately)
            {
                ApplyProfile();
                CaptureInitialState();
            }
        }

        public void ApplyProfile()
        {
            if (profileAsset == null || profileAsset.Value == null)
            {
                return;
            }

            var profile = profileAsset.Value;
            allowKeyboardControl = profile.AllowKeyboardControl;
            useUnscaledTime = profile.UseUnscaledTime;
            moveSpeedMetersPerSecond = profile.MoveSpeedMetersPerSecond;
            rotateSpeedDegreesPerSecond = profile.RotateSpeedDegreesPerSecond;
            rollSpeedDegreesPerSecond = profile.RollSpeedDegreesPerSecond;
            focalLengthSpeedMmPerSecond = profile.FocalLengthSpeedMmPerSecond;
            focusDistanceSpeedMetersPerSecond = profile.FocusDistanceSpeedMetersPerSecond;
            boostMultiplier = profile.BoostMultiplier;
        }

        public void ApplyRotation(Vector3 deltaEulerDegrees)
        {
            ControlledTransform.Rotate(deltaEulerDegrees, Space.Self);
        }

        public void ApplyLensDelta(float focalLengthDeltaMm, float focusDistanceDeltaMeters)
        {
            var cameraToUse = ControlledCamera;
            if (cameraToUse == null)
            {
                return;
            }

            cameraToUse.focalLength = Mathf.Max(1f, cameraToUse.focalLength + focalLengthDeltaMm);
            cameraToUse.focusDistance = Mathf.Max(0.01f, cameraToUse.focusDistance + focusDistanceDeltaMeters);
        }

        public void ResetPoseAndLens()
        {
            ControlledTransform.localPosition = initialLocalPosition;
            ControlledTransform.localRotation = initialLocalRotation;
            var cameraToUse = ControlledCamera;
            if (cameraToUse != null)
            {
                cameraToUse.focalLength = initialFocalLength;
                cameraToUse.focusDistance = initialFocusDistance;
            }
        }

        private void ResolveReferences()
        {
            if (controlledTransform == null)
            {
                controlledTransform = transform;
            }

            if (controlledCamera == null)
            {
                controlledCamera = GetComponent<Camera>();
            }
        }

        private void CaptureInitialState()
        {
            initialLocalPosition = ControlledTransform.localPosition;
            initialLocalRotation = ControlledTransform.localRotation;
            var cameraToUse = ControlledCamera;
            if (cameraToUse != null)
            {
                initialFocalLength = cameraToUse.focalLength;
                initialFocusDistance = cameraToUse.focusDistance;
            }
        }
    }
}
