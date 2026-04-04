# FreeD Readiness

## 1. チェックリスト

- [x] D1 29 byte packet 生成
- [x] checksum 計算
- [x] Camera ID pack
- [x] User Area / FrameModulo16 pack
- [x] Unity Camera からの Free-D 送信
- [x] UDP loopback 受信確認
- [x] Single destination unicast
- [x] Multi destination unicast
- [x] Multicast 設計枠と設定 UI の最小実装
- [x] Output source 切り替え
- [x] `Blended` output source
- [x] `OutputDelayMs` の runtime 反映
- [x] SyncMode 切り替え
- [x] Diagnostics 基本表示
- [x] Debug Window の 4 状態比較
- [x] ReplaySample の JSON / CSV 読み込みと frame step
- [x] Multi destination diagnostics の送信順 / spread 表示
- [x] Samples と Setup Wizard の最小導線
- [x] OutputInspector で loopback 導線を即利用可能
- [x] 処理単位ごとの sample / test 対応表
- [x] Free-D controller sample
- [x] sample を `Assets/Samples/SyncFreeD` 配下で確認可能
- [x] sample scene 起動 PlayMode テスト
- [x] Fixed Camera の `Tracking 無効 / Lens 有効`
- [x] pose / lens 別ソースの合成
- [x] lens 系の observed / predicted / corrected 分離
- [x] multicast 運用支援 UI の厚み
- [x] Free-D UDP 受信
- [x] Free-D multicast 受信
- [x] Free-D 受信値の CG camera pose / lens 反映

## 2. 仕様書との差分

Free-D 同期観点で大きい差分だけを残しています。

- VISCA は package の実装対象から外し、Free-D 側の同期・出力・diagnostics に集中する
- multicast は送信設定、送信順 / spread 表示、Bind / Multicast NIC 選択支援、複数 NIC 時の warning まで対応
- Free-D input source は unicast / multicast の UDP 受信を扱い、D1 packet を pose / lens / timing に復元できる
- Free-D driven camera は受信した pose と lens を Unity Camera に反映できる
- lens correction は `LensProfile` を使って corrected lens の `FocalLengthMm` / `ZoomNormalized` / `FocusDistanceMeters` を補完可能
- sample scene の起動系テストはあるが、実機連携はスコープ外

## 3. 優先順位

1. multicast / diagnostics / profile まわりの Free-D 運用品質を維持する
