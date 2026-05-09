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

Core は `Runtime/Core` にあり、独立 asmdef `com.mizotake.syncfreed.core` と `noEngineReferences: true` で Unity 非依存の同期・補正・出力ロジックを保持します。
Unity 依存の結線は `Runtime/UnityAdapters` にまとめています。
source behaviour は `ICameraFrameProvider` と `ICameraSource` の両方を満たし、Core 側抽象としても扱えます。
Runtime 側は Free-D 入出力と Unity source の結線に絞っています。
Profile asset は `Runtime/ScriptableObjects`、UDP transport helper は `Runtime/Networking` にあります。

利用側で独自の `MonoBehaviour` source を作る場合は `ICameraFrameProvider` を実装し、`SyncFreeDBehaviour.SetSourceBehaviour` で接続できます。Inspector でも同 interface を満たさない入力元は warning として扱います。

利用側が asmdef を使う場合、`SyncFreeDBehaviour` などの Unity adapter には `com.mizotake.syncfreed` を、`CameraSyncState` や `FreeDPacketBuilder` などの core 型を直接使う assembly には `com.mizotake.syncfreed.core` も参照してください。
