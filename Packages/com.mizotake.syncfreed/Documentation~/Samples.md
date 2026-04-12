# Samples

確認用 sample は `Assets/Samples/SyncFreeD` 配下に配置して扱います。

- `BasicVirtualCamera`: Unity Camera をそのまま Free-D 出力
- `ExternalTrackerSample`: tracker 起点の pose 取得
- `ReplaySample`: JSON / CSV replay 読み込みと frame step
- `OutputInspectorSample`: packet preview と diagnostics と loopback 確認
- `FreeDControllerSample`: keyboard controller から Free-D を操作
- `FreeDReceiveSample`: Free-D multicast 受信で CG camera を駆動
- `SharedPresets`: sample で再利用する送信設定 / 操作設定の ScriptableObject
- `Operator Window`: sample 選択、rig 状態確認、色付き UI、shared preset 適用、preset 作成導線、おすすめ sample 案内、FreeD カメラの直接操作

## Sample Visual Rig

全 sample scene には `Sample Visual Rig` を配置しています。Play Mode では次の marker を共通の目印として使えます。

- Yellow: `Center Tower` / `Depth Pole`
- Blue: `Left Marker`
- Red: `Right Marker`
- Green: `Near Target`
- Purple: `Far Target`
- White: `Center Line` / `Cross Line`

camera が想定通りに動くと、左右 marker の相対位置、`Near Target` と `Far Target` の遠近感、`Center Line` に対する構図が分かりやすく変化します。

## Debug Controller

- `Tools/SyncFreeD/Debug Controller` は全 sample scene で使えます。
- `FreeDReceiveSample` のように `FreeDInputSourceBehaviour` がある sample では、その待受設定に向けて Free-D packet を送り、受信 camera の transform / lens 反映を確認できます。
- 送信系 sample や controller sample では、window が対象 camera を直接 debug apply するため、Play Mode 中に pan / tilt / zoom を触ると camera transform の変化をすぐ確認できます。

## Free-D 処理ごとの対応

### 1. Canonical State 生成

- sample: `BasicVirtualCamera`
- sample: `ExternalTrackerSample`
- sample: `ReplaySample`
- test: `SyncFreeDBehaviourPlayModeTests`
- test: `TrackerCameraSourceBehaviourPlayModeTests`
- test: `ReplayCameraSourceBehaviourPlayModeTests`
- test: `ReplayPoseDataParserTests`

### 2. Output Pose 切り替え

- sample: `ReplaySample`
- test: `CameraSyncStateSelectorTests`
- test: `CameraSyncEngineTests`

### 3. Delay / Lens Correction

- test: `DelayCompensatorTests`
- test: `SyncFreeDOutputDelayPlayModeTests`
- test: `DefaultCameraSynchronizerTests`

### 4. Free-D D1 Packet Build

- sample: `OutputInspectorSample`
- test: `FreeDEncodingTests`
- test: `FreeDPacketBuilderTests`
- test: `FreeDOutputTests`

### 5. UDP Output / Multi Destination / Filter

- sample: `BasicVirtualCamera`
- sample: `OutputInspectorSample`
- test: `FreeDUdpTransportTests`
- test: `FreeDUdpOutputBehaviourMultiDestinationPlayModeTests`
- test: `FreeDUdpOutputBehaviourFilterPlayModeTests`

### 6. Loopback / Packet Inspection / Diagnostics

- sample: `OutputInspectorSample`
- sample: `FreeDControllerSample`
- test: `FreeDUdpLoopbackPlayModeTests`
- test: `SyncDiagnosticsEvaluatorTests`
- test: `SyncDiagnosticsBehaviourPlayModeTests`

### 7. Controller Operation

- sample: `FreeDControllerSample`
- test: `FreeDControllerBehaviourPlayModeTests`
- test: `FreeDControllerProfileAssetPlayModeTests`

### 8. Preset Asset の再利用

- sample: `SharedPresets`
- sample: `BasicVirtualCamera`
- sample: `OutputInspectorSample`
- sample: `FreeDControllerSample`
- test: `ProfileAssetTests`
- test: `FreeDUdpOutputProfileAssetPlayModeTests`
- test: `FreeDControllerProfileAssetPlayModeTests`

### 9. Free-D Input / CG Camera Drive

- sample: `FreeDReceiveSample`
- test: `FreeDPacketParserTests`
- test: `FreeDInputSourceBehaviourPlayModeTests`
- test: `FreeDDrivenCameraBehaviourPlayModeTests`
