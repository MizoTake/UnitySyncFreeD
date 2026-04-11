using System;
using MizoTake.SyncFreeD.Core.Abstractions;
using MizoTake.SyncFreeD.Core.Diagnostics;
using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.Networking;
using MizoTake.SyncFreeD.Core.Sync;
using MizoTake.SyncFreeD.ScriptableObjects;
using UnityEngine;

namespace MizoTake.SyncFreeD.UnityAdapters.Behaviours
{
    public sealed class SyncFreeDBehaviour : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour sourceBehaviour;
        [SerializeField] private FreeDUdpOutputBehaviour outputBehaviour;
        [SerializeField] private DebugLogOutputBehaviour debugLogOutputBehaviour;
        [SerializeField] private RecordingOutputBehaviour recordingOutputBehaviour;
        [SerializeField] private SyncFreeDBehaviourProfileAsset profileAsset;
        [SerializeField] private bool applyProfileOnAwake = true;
        [SerializeField] private SyncMode syncMode = SyncMode.VirtualMaster;
        [SerializeField] private OutputPoseKind outputPoseKind = OutputPoseKind.Corrected;
        [SerializeField] private SyncTuningProfileAsset tuningProfileAsset;
        [SerializeField] private DeviceProfileAsset deviceProfileAsset;
        [SerializeField] private FirmwareBehaviorProfileAsset firmwareBehaviorProfileAsset;
        [SerializeField] private LensProfileAsset lensProfileAsset;
        [SerializeField] private MountProfileAsset mountProfileAsset;
        [SerializeField] private OutputTickMode outputTickMode = OutputTickMode.LateUpdate;
        [SerializeField] private int fixedIntervalMs = 33;
        [SerializeField] private SyncTuningProfile tuning = new SyncTuningProfile();
        [SerializeField] private bool logPacketHex;

        private readonly ITimestampProvider timestampProvider = new SystemTimestampProvider();
        private readonly SyncTickProcessor tickProcessor = new SyncTickProcessor();
        private float nextFixedIntervalTime;

        public CameraSyncState LastState { get; private set; }
        public SyncDiagnosticsSnapshot LastDiagnostics { get; private set; }
        public int CorrectionAppliedCount { get; private set; }
        public ICameraFrameProvider SourceProvider => sourceBehaviour as ICameraFrameProvider;
        public FreeDUdpOutputBehaviour OutputBehaviour => outputBehaviour;
        public SyncMode SyncMode => syncMode;
        public OutputPoseKind OutputPoseKind => outputPoseKind;
        public SyncTuningProfile EffectiveTuning => tuningProfileAsset != null && tuningProfileAsset.Value != null ? tuningProfileAsset.Value : tuning;
        public SyncFreeDBehaviourProfileAsset ProfileAsset => profileAsset;
        public bool ApplyProfileOnAwake => applyProfileOnAwake;
        public SyncTuningProfileAsset TuningProfileAsset => tuningProfileAsset;
        public DeviceProfileAsset DeviceProfileAsset => deviceProfileAsset;
        public FirmwareBehaviorProfileAsset FirmwareBehaviorProfileAsset => firmwareBehaviorProfileAsset;
        public LensProfileAsset LensProfileAsset => lensProfileAsset;
        public MountProfileAsset MountProfileAsset => mountProfileAsset;
        public OutputTickMode OutputTickMode => outputTickMode;
        public int FixedIntervalMs => fixedIntervalMs;
        public bool LogPacketHex => logPacketHex;
        public string FirmwareBehaviorWarning => FirmwareBehaviorProfileValidator.Validate(firmwareBehaviorProfileAsset != null ? firmwareBehaviorProfileAsset.Value : null, outputBehaviour != null ? outputBehaviour.SendMode : PacketSendMode.SingleDestinationUnicast);
        public bool HasFirmwareBehaviorWarning => !string.IsNullOrEmpty(FirmwareBehaviorWarning);

        private void Reset()
        {
            ResolveReferences();
            if (applyProfileOnAwake)
            {
                ApplyProfile();
            }
        }

        private void Awake()
        {
            ResolveReferences();
            if (applyProfileOnAwake)
            {
                ApplyProfile();
            }
            ResetFixedIntervalClock();
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

        private void LateUpdate()
        {
            if (outputTickMode != OutputTickMode.LateUpdate)
            {
                return;
            }

            Tick();
        }

        private void Update()
        {
            if (outputTickMode != OutputTickMode.FixedInterval)
            {
                return;
            }

            if (Time.unscaledTime < nextFixedIntervalTime)
            {
                return;
            }

            Tick();
            nextFixedIntervalTime = Time.unscaledTime + Mathf.Max(0.001f, fixedIntervalMs / 1000f);
        }

        private void FixedUpdate()
        {
            if (outputTickMode != OutputTickMode.FixedUpdate)
            {
                return;
            }

            Tick();
        }

        public bool ManualTick()
        {
            return Tick();
        }

        public void SetProfileAsset(SyncFreeDBehaviourProfileAsset asset, bool applyImmediately)
        {
            profileAsset = asset;
            if (applyImmediately)
            {
                ApplyProfile();
            }
        }

        public void ApplyProfile()
        {
            if (profileAsset == null || profileAsset.Value == null)
            {
                return;
            }

            var profile = profileAsset.Value;
            syncMode = profile.SyncMode;
            outputPoseKind = profile.OutputPoseKind;
            outputTickMode = profile.OutputTickMode;
            fixedIntervalMs = profile.FixedIntervalMs;
            logPacketHex = profile.LogPacketHex;
            tuningProfileAsset = profile.TuningProfileAsset;
            tuning = profile.Tuning ?? new SyncTuningProfile();
            deviceProfileAsset = profile.DeviceProfileAsset;
            firmwareBehaviorProfileAsset = profile.FirmwareBehaviorProfileAsset;
            lensProfileAsset = profile.LensProfileAsset;
            mountProfileAsset = profile.MountProfileAsset;
        }

        private void OnEnable()
        {
            ResetFixedIntervalClock();
            tickProcessor.Reset();
        }

        private bool Tick()
        {
            ResolveReferences();
            var sourceProvider = SourceProvider;
            if (sourceProvider == null || outputBehaviour == null)
            {
                return false;
            }

            if (!sourceProvider.TryGetObservedFrame(out var observedFrame))
            {
                return false;
            }

            var effectiveTuning = EffectiveTuning ?? new SyncTuningProfile();
            var result = tickProcessor.Process(new SyncTickRequest(timestampProvider.GetTimestampTicks(), observedFrame, sourceProvider.CaptureCommandFrame(), syncMode, outputPoseKind, effectiveTuning, lensProfileAsset != null ? lensProfileAsset.Value : null));
            LastState = result.State;
            LastDiagnostics = result.Diagnostics;
            if (LastDiagnostics.CorrectionApplied)
            {
                CorrectionAppliedCount++;
            }

            var packet = outputBehaviour.BuildPacket(LastState);
            if (debugLogOutputBehaviour != null)
            {
                debugLogOutputBehaviour.Send(LastState);
            }

            if (recordingOutputBehaviour != null)
            {
                recordingOutputBehaviour.Send(LastState);
            }

            if (logPacketHex)
            {
                Debug.Log(BitConverter.ToString(packet));
            }

            outputBehaviour.Send(LastState);
            return true;
        }

        private void ResolveReferences()
        {
            if (sourceBehaviour == null)
            {
                sourceBehaviour = GetComponent<CompositeCameraSourceBehaviour>();
            }

            if (sourceBehaviour == null)
            {
                sourceBehaviour = GetComponent<UnityCameraSourceBehaviour>();
            }

            if (sourceBehaviour == null)
            {
                sourceBehaviour = GetComponent<TrackerCameraSourceBehaviour>();
            }

            if (sourceBehaviour == null)
            {
                sourceBehaviour = GetComponent<ReplayCameraSourceBehaviour>();
            }

            if (outputBehaviour == null)
            {
                outputBehaviour = GetComponent<FreeDUdpOutputBehaviour>();
            }

            if (debugLogOutputBehaviour == null)
            {
                debugLogOutputBehaviour = GetComponent<DebugLogOutputBehaviour>();
            }

            if (recordingOutputBehaviour == null)
            {
                recordingOutputBehaviour = GetComponent<RecordingOutputBehaviour>();
            }
        }

        private void ResetFixedIntervalClock()
        {
            nextFixedIntervalTime = Time.unscaledTime;
        }
    }
}
