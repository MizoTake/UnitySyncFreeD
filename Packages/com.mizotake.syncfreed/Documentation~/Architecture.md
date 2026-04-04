# Architecture

```text
Sources
  UnityCameraSourceBehaviour
  TrackerCameraSourceBehaviour
  ReplayCameraSourceBehaviour
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
VISCA は package の実装対象に含めず、必要なら外部側で adapter 契約へ接続する前提に留めます。
Profile asset は `Runtime/ScriptableObjects`、UDP transport helper は `Runtime/Networking` にあります。
