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

revision を固定する場合は、URL の末尾に branch、tag、commit を付けます。

```text
https://github.com/MizoTake/UnitySyncFreeD.git?path=/Packages/com.mizotake.syncfreed#master
```

## Features

- Free-D D1 29 byte packet build / parse
- checksum、Camera ID、User Area、FrameModulo16
- UDP single destination unicast、multi-destination unicast、multicast、loopback
- Unity Camera / Tracker / Replay / Controller source
- Free-D input source と driven camera
- diagnostics、debug output、recording output
- Editor inspector、debug window、operator window、setup wizard

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
