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

Inspector / Debug Window では destination count、effective send mode、send success / failure、destination spread、skipped by filter、multicast configured、packet hex、checksum、user area を確認できます。

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

## Readiness

- D1 29 byte packet、checksum、Camera ID、User Area / FrameModulo16 は実装済み
- unicast、multi-destination unicast、multicast、loopback は検証済み
- output pose 切り替え、`Blended`、`OutputDelayMs` は runtime 反映済み
- Free-D input、multicast receive、camera ID filter、driven camera pose/lens apply は実装済み
- firmware profile に応じた send mode / destination limit / mount補正範囲は runtime 反映済み
- 実機 firmware ごとの差分を使った運用最適化と相互接続確認は今後の対象です
