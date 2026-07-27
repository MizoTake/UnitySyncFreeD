# SyncFreeD

SyncFreeD は Unity 向けの Free-D camera sync / packet output UPM package です。

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

revisionを固定する場合は、URLの末尾に公開済みtagまたはcommit hashを付けます。branch名は更新されるため再現可能な固定ではありません。

```text
https://github.com/MizoTake/UnitySyncFreeD.git?path=/Packages/com.mizotake.syncfreed#<commit-hash>
```

## Features

- Free-D D1 29 byte packet build / parse
- D0～DB message type分類とD1 packet build / parse
- profile-driven D1 input decode (`RawUnsigned24` / `SyncFreeDPhysicalV1` / stable-ID built-in / custom)
- raw D1 field preservation、capability-aware XYZ / Roll apply、PTZ pivot rig apply
- optional frame-rate-independent position / rotation / lens motion smoothing for received Free-D cameras
- physical / effective focal length separation と profile sensor gate
- checksum、Camera ID、User Area、FrameModulo16
- UDP single destination unicast、multi-destination unicast、multicast、loopback
- Unity Camera / Tracker / Replay / Controller source
- Free-D input source と driven camera
- diagnostics、debug output、recording output
- Editor inspector、debug window、operator window、setup wizard

## Input Profiles

`FreeDUdpInputProfileAsset` の `Packet Decoding Preset` で受信形式を選びます。

- `RawUnsigned24`: 既定値です。未知のD1 packetを安全に調査するraw captureとして、pose / lens capabilityを宣言せず、値を `RawFreeD` に保持します。
- `SyncFreeDPhysicalV1`: 本packageの `FreeDPacketBuilder` と同じ従来形式。Zoomは焦点距離mm × 1000、Focusは距離の逆数です。
- `BuiltInDeviceProfile`: stable IDで検証済みdevice / lens変換を明示選択します。BRC-X1000 firmware 2.10用IDは `sony-brc-x1000-fw-2.10` です。
- `Custom`: scale、normalized unsigned 24-bit、reciprocal、piecewise-linear LUT、capability、sensor gateをデータとして指定します。Zoom / Focus decoderとcapabilityは対応させる必要があります。

D1はcamera position / orientation、D2はsystem statusであり、機種やrig種別ではありません。Zoom / Focusの単位は一般Free-D D1では固定されていないため、未知の機種は `RawUnsigned24` でraw値を確認してからbuilt-in IDまたはcustom profileを明示してください。詳細とdevice profileの留保事項は `Documentation~/Overview.md` を参照してください。

## Package Layout

- `Runtime/Core`: Unity に依存しない同期・補正・Free-D packet 処理
- `Runtime/UnityAdapters`: Unity scene / component との接続
- `Runtime/Networking`: UDP transport、receive hub、configuration validation
- `Editor`: inspector、window、setup helper
- `Samples~`: Package Manager から import する sample source
- `Documentation~`: Package Manager から参照する利用者向けドキュメント
- `Tests/Editor`, `Tests/Runtime`: Unity Test Runner 用テスト

## Samples

配布用 sample source は `Samples~` にあります。Package Manager から import すると、Unity は project 側の `Assets/Samples/...` 配下へ展開します。

- `BasicVirtualCamera`: Unity Camera をそのまま Free-D 出力
- `ExternalTrackerSample`: tracker 起点の pose 取得
- `ReplaySample`: JSON / CSV replay 読み込みと frame step
- `OutputInspectorSample`: packet preview、diagnostics、recording、loopback
- `FreeDControllerSample`: keyboard controller から Free-D を操作
- `FreeDReceiveSample`: Free-D multicast 受信で CG camera を駆動

全 sample scene には `Sample Visual Rig` を配置しています。

## Documentation

- `Documentation~/Overview.md`

## License

MIT License.
