# Overview

`SyncFreeD` は Unity でカメラ状態を正規化し、同期・補正を経て Free-D 出力へ変換するための package です。

## Package Scope

- Free-D D1 29 byte packet build / parse
- checksum、Camera ID、User Area、FrameModulo16
- UDP single destination unicast、multi-destination unicast、multicast、loopback
- Unity Camera / Tracker / Replay / Controller source
- Free-D input source と driven camera
- diagnostics、debug output、recording output
- Editor inspector、debug window、operator window、setup wizard
- Package Manager samples

VISCA 実装は本 package の対象外です。

## Installation

Unity Package Manager の Git URL からインストールできます。

```text
https://github.com/MizoTake/UnitySyncFreeD.git?path=/Packages/com.mizotake.syncfreed
```

`Packages/manifest.json` に直接追加する場合は、`dependencies` に次を追加します。

```json
{
  "dependencies": {
    "com.mizotake.syncfreed": "https://github.com/MizoTake/UnitySyncFreeD.git?path=/Packages/com.mizotake.syncfreed"
  }
}
```

## Architecture

Core は `Runtime/Core` にあり、独立 asmdef `com.mizotake.syncfreed.core` と `noEngineReferences: true` で Unity 非依存の同期・補正・Free-D packet 処理を保持します。

Unity 依存の結線は `Runtime/UnityAdapters`、UDP transport / receive hub / validation は `Runtime/Networking`、profile asset は `Runtime/ScriptableObjects` にあります。

利用側で独自の source を作る場合は `ICameraFrameProvider` を実装し、`SyncFreeDBehaviour.SetSourceBehaviour` で接続します。asmdef を使う場合、Unity adapter には `com.mizotake.syncfreed` を、core 型を直接使う assembly には `com.mizotake.syncfreed.core` も参照してください。

## Sync Model

`CameraSyncState` は `Command`, `Predicted`, `Observed`, `Corrected` を分けて保持します。

- `VirtualMaster`: command / predicted 優先
- `RealMaster`: observed 優先
- `DualDrive`: 操作中は predicted と observed を補正し、入力停止後は observed へ最終着地
- `ExternalTrackingMaster`: observed 優先
- `ReplayMaster`: replay の predicted 優先

`SyncTuningProfile` では gain、snap threshold、delay を調整できます。

## Free-D Output

Free-D D1 packet は `FreeDPacketBuilder` が生成し、`FreeDPacketParser` が復元します。UDP runtime は `FreeDUdpOutputBehaviour` と `FreeDUdpTransport` が担当します。

`FreeDUdpOutputBehaviour` は `PacketSendMode` に応じて `SingleDestinationUnicast`、`MultiDestinationUnicast`、`Multicast` を切り替えます。ネットワーク設定として `Bind Address`、`Socket Buffer Size`、`Camera ID Filter`、`Multicast TTL`、`Join Multicast Group`、`Multicast Interface Address` を持ちます。

実行中に input / output profile を即時反映した場合、listen port、bind address、socket buffer、multicast membership などの network identity を既存 socket の表示値だけでなく実 resource にも反映します。無効化された output component への UDP、debug log、recording の副作用は停止しますが、`SyncFreeDBehaviour` 自体が有効なら同期 state と update event は更新を継続します。

Profile Asset を設定しない構成、または自動反映を無効にした構成では、custom Inspector から component の serialized 設定を直接編集できます。自動反映が有効な場合は Profile Asset を設定の正本として扱います。

Inspector / Debug Window では destination count、effective send mode、send success / failure、destination spread、skipped by filter、multicast configured、packet hex、checksum、user area を確認できます。

## Free-D Input Decoding

Free-Dのmessage typeは用途を表します。D0はpoll / command、D1はcamera position / orientation、D2はsystem statusであり、camera modelやrig構造の分類ではありません。本packageのcamera入力codecはD1を対象とし、D1以外は `UnsupportedMessageType` として記録します。`FreeDMessageType` はD0～DBのカテゴリを表しますが、D2以降のpayload decodeは未実装です。

Free-D D1はPan / Tilt / Roll / XYZの固定小数点表現を規定しますが、ZoomとFocusは24-bit unsignedの任意単位であり、物理焦点距離やFocus距離への共通変換を規定しません。`FreeDPacketParser` はD1構造とchecksumを検証した後、`FreeDPacketDecodingProfile` でZoom / Focus / User16を解釈します。元のmessage typeとsigned / unsigned値は常に `CameraObservedFrame.RawFreeD` に保持します。

`FreeDUdpInputProfileAsset.PacketDecodingPreset` には次の選択肢があります。

- `RawUnsigned24`: 既定値です。pose / lens capabilityを持たない安全なD1 raw captureとして、Zoom / Focus / User16を `CameraObservedFrame.RawFreeD` に保持し、Unity TransformやCameraへは適用しません。未知機種の最初の選択肢です。
- `SyncFreeDPhysicalV1`: `FreeDPacketBuilder` の従来形式。Zoomは焦点距離mm × 1000、Focusは `2^18 / distanceMeters` の逆変換です。package loopbackを明示的に選ぶ場合に使用します。
- `BuiltInDeviceProfile`: `BuiltInPacketDecodingProfileId` のstable IDで検証済みdevice / lens profileを選択します。未知IDや空IDはraw captureへfail closedします。
- `Custom`: `ScaleAndOffset`、`NormalizeUnsigned24`、`Reciprocal`、`PiecewiseLinear`を各lens fieldへ指定し、capability、User16、sensor gateをデータとして設定します。

custom profileは `FreeDUdpInputProfileAsset.Value.CustomPacketDecodingProfile` に格納されるserializable dataです。`FreeDPacketDecodingProfileValidator.TryValidate` はNaN / Infinity、sensor寸法の片側欠落、LUT raw keyの範囲外・重複・未整列に加え、Zoom / Focus / Iris capabilityとdecoderの不整合を拒否します。custom profileが無効な場合、`FreeDInputSourceBehaviour` は全capabilityを無効にしたraw captureへfail closedし、理由を `LastProfileError` に保持します。public parser APIへ無効なprofileを直接渡した場合も `InvalidDecodingProfile` でpacketを拒否します。

一般Free-Dの全機種へ同じcapabilityを仮定しません。parserはprofileで宣言されたfieldだけを `CameraObservedFrame.Capabilities` とpose / lensへ反映します。`FreeDDrivenCameraBehaviour` もPosition / PanTilt / Roll / Zoom / Focusを個別にgateし、capabilityがないCamera lens項目は起動時の値へ戻します。6DoF trackerではPosition / Rollを含むcustom profileを使用できます。

## Built-In Device Profile Example

stable ID `sony-brc-x1000-fw-2.10` は、2026-07-27に確認できたSony公開情報に基づくBRC-X1000用D1 decoding profileです。最新公開firmwareは2.10、Camera Tracking Integration ManualはRevision 1.0であり、次の公開値を使用します。この機種名はリグやparserの分岐ではなく、変換表の適用範囲を限定するためprofile catalogにだけ保持します。

- Zoom Position `0x0000 = 1x`、`0x1800 = 2x`、`0x4000 = 12x`
- 光学焦点距離 `9.3mm`から`111.6mm`
- Focus Position `0x1000 = infinity`、`0x2000 = 5m`、`0x3000 = 3m`、`0x4000 = 2m`から`0xF000 = 0.08m`までの参考表
- User16の下位12bit = F-number × 100、上位4bit = frame modulo 16
- Sonyが出力するfield = Pan / Tilt / Zoom / Focus / Iris。Position / Rollは無効。source分類として `ExternalTracking` capabilityも有効

Focus表はSony資料でもreference valueであり、個体・Zoom位置・被写体条件を含むDOF校正を置き換えるものではありません。`0x1000`のinfinityはUnityへ有限値として渡すため1000mに丸めています。各公開anchor間はpiecewise-linear補間であり、特にinfinityから5mまでの中間値はSony仕様値ではなく実装上の近似です。厳密なFocus運用ではcustom LUTへ実測値を設定してください。

光学中間焦点距離とClear Image Zoomの実効焦点距離は、公称Wide焦点距離9.3mmとSonyの参考Zoom倍率から導出した値で、実測lens calibrationではありません。Clear Image Zoomの `0x5580 = 18x`、`0x6000 = 24x` は物理レンズ焦点距離ではないため、`LensState.FocalLengthMm` は光学上限111.6mmを保持し、投影画角用の `EffectiveFocalLengthMm` をそれぞれ167.4mm、223.2mmとして分離します。Sony製品仕様上、最大倍率は4Kで18x、HDで24xと映像modeに依存します。Tele ConvertはSony資料上Zoom metadataへ反映されないため、利用側で別途補正が必要です。

profileのsensor gate `11.7584319mm × 6.6141179mm` は物理1.0型sensor全体の寸法ではありません。Sony公称の広角水平画角64.6°、焦点距離9.3mm、16:9から逆算した投影用active gateです。`FreeDDrivenCameraBehaviour` はZoom capabilityとprofile値が有効な場合だけUnity physical Cameraの `sensorSize` へ反映し、Zoom capabilityのないprofileへ切り替えた場合は起動時のsensor size / focal length / physical propertiesへ戻します。

参照したSony公開資料:

- [BRC-X1000 / X400 Camera Tracking Integration Manual v1.0](https://pro.sony/s3/2020/08/04144807/BRC-X1000_X400_series_integration_manual_CameraTrackingFunction_v1.0.pdf)
- [BRC-X1000 / H800 / H780 Technical Manual](https://pro.sony/support/res/manuals/C456/26213181c7ae83822c3124b5b3b836ee/C4561001M.pdf)
- [BRC-X1000 downloads](https://www.sony.com/electronics/support/studio-and-broadcast-cameras-pan-tilt-zoom-cameras/brc-x1000/downloads)

## Capability-Based D1 Rig And Timing

D1で得られるfieldは、選択profileの `CameraCapabilities` で個別に宣言します。機械式PTZを単一Transformへ絶対回転すると、設置向きや回転軸と光学中心のoffsetを表現できません。`FreeDDrivenCameraBehaviour` の `Pan Axis`、`Tilt Axis`、任意の `Roll Axis` へ別Transformを指定すると、それぞれの初期local rotationを基準に相対適用します。推奨階層は `Installation Root -> Pan Axis -> Tilt Axis -> Optical Center / Camera` です。軸間距離と光学中心offsetは機種・設置固有なのでpackageでは固定しません。`Rotation Application Mode` は既定の `InstallationRelative` と、6DoF tracking座標を直接使う `Absolute` を明示的に選べます。profileが必要とする軸の一部だけが設定された場合や同じTransformを複数軸へ指定した場合は、`LastRigError` を設定して単一Transformのcombined rotationへfallbackし、軸値を無言で捨てません。

`FreeDDrivenCameraBehaviour` の `Enable Motion Smoothing` を有効にすると、Position、Rotation、Lensを描画frameごとの指数補間で追従させます。各 `Smoothing Half Life Seconds` は誤差が半分になる時間で、frame rateへ依存しません。最初の有効sample、component再有効化後、`ResetMotionSmoothing()` 後は現在値へ即時同期し、その後のsample差分だけを平滑化します。既定は無効で、half-life 0も即時反映です。runtimeからは `SetMotionSmoothing(enabled, positionHalfLifeSeconds, rotationHalfLifeSeconds, lensHalfLifeSeconds)` で変更できます。

D1入力の開始値としてRotation 0.04秒、Lens 0.06秒を試せますが、protocolやvendorが規定する推奨値ではありません。実際のpacket周期と許容追従遅れを見て調整してください。この機能はlatest-only入力の段差を視覚的に弱めるlow-pass filterであり、固定遅延付き時刻補間や映像同期校正の代替ではありません。正確なAR合成で追加位相遅れを許容できない場合は無効のまま使用してください。

0.1.0相当の単一Transform絶対回転が必要な既存sceneは `Rotation Application Mode` を `Absolute` に設定してください。0.2.0の新規既定値は固定設置PTZを安全に扱う `InstallationRelative` です。

Free-D UDPとSDI / capture videoの遅延は同じとは限りません。選択profileがUser16から `FrameModulo16` を復元する場合は対応frame調査の手掛かりになりますが、正しい `TrackingDelayMs`、`OutputDelayMs`、`VideoAlignmentDelayMs` は実機映像とpacketを同時収録して測定してください。0msは未校正値であり、実機一致を保証する値ではありません。

## Runtime Update Events

外部 code から更新を push 型で受け取りたい場合は、Free-D input の受理済み frame は `FreeDInputSourceBehaviour.ObservedFrameUpdated`、同期 tick 後の state は `SyncFreeDBehaviour.StateUpdated` を購読します。

`ObservedFrameUpdated` は checksum / camera ID filter を通過した packet が `lastFrame` に反映された後に呼ばれます。`StateUpdated` は `ManualTick`、`LateUpdate`、`FixedUpdate`、または fixed interval の tick が成功し、`LastState`、`LastOutputState`、`LastDiagnostics` が更新された後に呼ばれます。

Replay source の自動再生時間は Unity 全体の起動時刻ではなく component の enable / reload を起点にします。Pause は現在の補間位置を維持し、Resume は停止時間を加算せずその位置から再開します。

## Samples

配布用 sample source は package 内の `Samples~` 配下にあります。Package Manager から import すると、Unity project 側の `Assets/Samples/...` に展開されます。

- `BasicVirtualCamera`: Unity Camera をそのまま Free-D 出力
- `ExternalTrackerSample`: tracker 起点の pose 取得
- `ReplaySample`: JSON / CSV replay 読み込みと frame step
- `OutputInspectorSample`: packet preview、diagnostics、recording、loopback
- `FreeDControllerSample`: keyboard controller から Free-D を操作
- `FreeDReceiveSample`: Free-D multicast 受信で CG camera を駆動

全 sample scene には `Sample Visual Rig` を配置しています。Play Mode では `Center Tower`、`Depth Pole`、`Left Marker`、`Right Marker`、`Near Target`、`Far Target`、`Center Line`、`Cross Line` を共通の目印として使えます。

この開発リポジトリでは、PlayMode 起動確認や shared preset の検証用 fixture として `Assets/Samples/SyncFreeD` に import 済み sample も保持しています。公開UPMとして利用者へ見せる sample 一覧は `package.json` の `samples` 配列を正とします。

Release gate では `Samples~` を一時的な `Assets` 配下へ import し、missing script / missing asset reference と主要 component をEditModeで確認した後、同じimport結果をPlayModeで起動します。`Assets/Samples/SyncFreeD` は開発用fixtureであり、配布sampleの合否判定には使用しません。

## Readiness

- D1 29 byte packet、checksum、Camera ID、User Area / FrameModulo16 は実装済み
- unicast、multi-destination unicast、multicast、loopback は検証済み
- output pose 切り替え、`Blended`、`OutputDelayMs` は runtime 反映済み
- Free-D input、multicast receive、camera ID filter、driven camera pose/lens apply は実装済み
- message type分類、profile-driven D1 input decode、raw D1保持、stable-ID built-in device profile、custom LUT / capabilityは実装済み
- BRC-X1000の公開Zoom / Focus参照点と投影sensor gateは回帰テスト済み。実機packet captureとの相互接続は未実施
- firmware profile に応じた send mode / destination limit / mount補正範囲は runtime 反映済み
- 実機 firmware ごとの差分、Focus / distortion / PTZ軸offset / video遅延の校正と相互接続確認は今後の対象です
