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
- [x] Samples と Setup Wizard の最小導線
- [x] VISCA telemetry provider adapter 契約
- [x] 標準 `ViscaTelemetryProviderBehaviour`
- [x] lens 系の observed / predicted / corrected 分離
- [ ] multicast 運用支援 UI の厚み

## 2. 仕様書との差分

Free-D 同期観点で大きい差分だけを残しています。

- `ViscaCameraSourceBehaviour` は `IViscaTelemetryProvider` と `ViscaTelemetryProviderBehaviour` で外部テレメトリを受けられるが、実機 inquiry / telemetry client 本体は未実装
- multicast は送信設定と状態表示はあるが、NIC 選択支援や障害解析は最小
- sample scene の存在と起動系テストはあるが、実機連携の end-to-end は未確認

## 3. 優先順位

1. `IViscaTelemetryProvider` を実装する実機 inquiry / telemetry client 本体を追加する
2. multicast / NIC 運用支援 UI を厚くして、受信先切り分けを Editor で完結できるようにする
3. lens correction の profile 適用を強化して、corrected lens を tuning と連動させる
