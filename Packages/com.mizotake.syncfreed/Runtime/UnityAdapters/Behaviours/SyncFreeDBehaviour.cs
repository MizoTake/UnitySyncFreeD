using System;
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

        private readonly CameraSyncEngine synchronizer = new CameraSyncEngine();
        private readonly DelayCompensator outputDelayCompensator = new DelayCompensator();
        private float nextFixedIntervalTime;

        public CameraSyncState LastState { get; private set; }
        public SyncDiagnosticsSnapshot LastDiagnostics { get; private set; }
        public int CorrectionAppliedCount { get; private set; }
        public ICameraFrameProvider SourceProvider => sourceBehaviour as ICameraFrameProvider;
        public SyncMode SyncMode => syncMode;
        public SyncTuningProfile EffectiveTuning => tuningProfileAsset != null && tuningProfileAsset.Value != null ? tuningProfileAsset.Value : tuning;
        public SyncTuningProfileAsset TuningProfileAsset => tuningProfileAsset;
        public DeviceProfileAsset DeviceProfileAsset => deviceProfileAsset;
        public FirmwareBehaviorProfileAsset FirmwareBehaviorProfileAsset => firmwareBehaviorProfileAsset;
        public LensProfileAsset LensProfileAsset => lensProfileAsset;
        public MountProfileAsset MountProfileAsset => mountProfileAsset;
        public string FirmwareBehaviorWarning => FirmwareBehaviorProfileValidator.Validate(firmwareBehaviorProfileAsset != null ? firmwareBehaviorProfileAsset.Value : null, outputBehaviour != null ? outputBehaviour.SendMode : PacketSendMode.SingleDestinationUnicast);
        public bool HasFirmwareBehaviorWarning => !string.IsNullOrEmpty(FirmwareBehaviorWarning);

        private void Reset()
        {
            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
            ResetFixedIntervalClock();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveReferences();
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

        private void OnEnable()
        {
            ResetFixedIntervalClock();
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
            var context = new CameraSyncContext(DateTime.UtcNow.Ticks, observedFrame, sourceProvider.CaptureCommandFrame(), syncMode, effectiveTuning);
            var state = synchronizer.Update(context, outputPoseKind);
            state = ApplyLensProfile(state);
            state = ApplyOutputDelay(state, effectiveTuning.OutputDelayMs);
            LastState = state;
            LastDiagnostics = SyncDiagnosticsEvaluator.Evaluate(state);
            if (LastDiagnostics.CorrectionApplied)
            {
                CorrectionAppliedCount++;
            }

            var packet = outputBehaviour.BuildPacket(state);
            if (debugLogOutputBehaviour != null)
            {
                debugLogOutputBehaviour.Send(state);
            }

            if (recordingOutputBehaviour != null)
            {
                recordingOutputBehaviour.Send(state);
            }

            if (logPacketHex)
            {
                Debug.Log(BitConverter.ToString(packet));
            }

            outputBehaviour.Send(state);
            return true;
        }

        private CameraSyncState ApplyLensProfile(in CameraSyncState state)
        {
            var profile = lensProfileAsset != null ? lensProfileAsset.Value : null;
            return LensProfileApplicator.Apply(state, profile);
        }

        private CameraSyncState ApplyOutputDelay(in CameraSyncState state, int outputDelayMs)
        {
            outputDelayCompensator.Push(state.Corrected);
            if (outputDelayMs <= 0)
            {
                return state;
            }

            var delayedState = state;
            delayedState.Corrected = outputDelayCompensator.SampleDelayed(state.Corrected.TimestampTicks, outputDelayMs);
            return delayedState;
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
