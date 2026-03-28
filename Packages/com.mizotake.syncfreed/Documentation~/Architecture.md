# Architecture

```text
Sources
  UnityCameraSourceBehaviour
  TrackerCameraSourceBehaviour
  ReplayCameraSourceBehaviour
  ViscaCameraSourceBehaviour
    optional IViscaTelemetryProvider
    ↓
SyncFreeDBehaviour
    ↓
DefaultCameraSynchronizer
    ↓
Diagnostics / Outputs
  FreeDUdpOutputBehaviour
  DebugLogOutputBehaviour
  RecordingOutputBehaviour
Editor
  SyncFreeDBehaviourEditor
  FreeDUdpOutputBehaviourEditor
  SyncDiagnosticsBehaviourEditor
  SyncFreeDDebugWindow
  SyncFreeDSetupWizard
```

Core は `Runtime/Core` にあり、Unity 非依存の同期・補正・出力ロジックを保持します。
Unity 依存の結線は `Runtime/UnityAdapters` にまとめています。
source behaviour は `ICameraFrameProvider` と `ICameraSource` の両方を満たし、Core 側抽象としても扱えます。
`ViscaCameraSourceBehaviour` は transform fallback に加えて `IViscaTelemetryProvider` から observed pose / lens / timing を受けられます。
Profile asset は `Runtime/ScriptableObjects`、UDP transport helper は `Runtime/Networking` にあります。
