# Free-D Sync Readiness

## 1. 完成度チェックリスト

### Free-D packet / transport

- [x] D1 29 byte packet builder
- [x] 24bit angle / position / zoom / focus encoding
- [x] checksum 計算
- [x] Camera ID / User Area / FrameModulo16
- [x] Single destination unicast
- [x] Multi destination unicast
- [x] Multicast 設計枠と最小送信設定
- [x] Loopback 受信確認

### Sync / timing

- [x] `CameraSyncEngine`
- [x] `DefaultCameraSynchronizer`
- [x] `DualDriveCameraSynchronizer`
- [x] `RealMasterCameraSynchronizer`
- [x] `ReplayCameraSynchronizer`
- [x] `OutputPoseKind` 切り替え
- [x] `LateUpdate`
- [x] `FixedInterval`
- [x] `FixedUpdate`
- [x] `ManualTick`
- [x] `TrackingDelayMs`
- [x] `VideoAlignmentDelayMs`
- [x] `OutputDelayMs`

### Source / state

- [x] `UnityCameraSourceBehaviour`
- [x] `TrackerCameraSourceBehaviour`
- [x] `ReplayCameraSourceBehaviour`
- [x] `CompositeCameraSourceBehaviour`
- [x] `LensEncoderSourceBehaviour`
- [x] `ICameraSource` と `ICameraFrameProvider` の整合
- [x] pose の `Command / Predicted / Observed / Corrected`
- [x] lens の `CommandLens / PredictedLens / ObservedLens / CorrectedLens`
- [x] `Tracking 無効 / Lens 有効` の validity 表現

### Diagnostics / editor

- [x] Pan / Tilt / Roll / Position error
- [x] Zoom error
- [x] TrackingDelay / VideoAlignmentDelay
- [x] Tracking / Lens / Degraded / Fallback
- [x] Packet hex / checksum / user area
- [x] Destination count / send success / send failure
- [x] Destination send order / spread
- [x] Debug Window の 4 状態比較
- [x] ReplaySample の JSON / CSV 読み込み
- [x] ReplaySample の frame step

### Tests

- [x] EditMode unit tests
- [x] PlayMode behaviour tests
- [x] UDP loopback test
- [x] Camera ID filter test
- [x] Multi destination send test
- [x] sample scene 起動 test

## 2. 仕様との差分一覧

### 実装済み

- Free-D D1 packet builder と UDP 送信の中核
- SyncMode 切り替えと source ごとの同期経路
- Free-D packet preview / diagnostics / setup wizard / samples
- UPM package layout, asmdef, samples, docs, tests

### 最小実装に留まるもの

- multicast の運用支援 UI
- VISCA は package の実装対象外
- lens 差分は state と diagnostics まで実装済みだが、実機 source 側の command / observed lens 分離はまだ薄い

### 未完了

- 機種 / firmware 差を用いた Free-D 運用最適化

## 3. 優先順位

### P1

- Free-D 同期品質の確認用に、command と observed の lens 差分を PlayMode で再現するテストを維持する

### P2

- firmware profile に応じた destination 制約や offset 適用の切り替えを Runtime に反映する
- sample scene を実運用寄りの preset に近づける

## 判定

現時点では、Unity 内で Free-D packet を生成し、同期 state を経由して UDP 送信し、loopback で受信確認するところまでは到達しています。したがって「Free-D 同期の基礎動作確認が可能な状態」と扱えます。  
VISCA 実装は本 package の対象外です。
