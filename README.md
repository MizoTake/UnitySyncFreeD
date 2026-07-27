# UnitySyncFreeD

UnitySyncFreeD は Unity 向け UPM package `com.mizotake.syncfreed` の開発リポジトリです。

Free-D を中心にした camera sync / packet output 基盤と、機種別profileで入力を正規化するRuntime、Editor tooling、Samples、Testsを含みます。

## Installation

Unity Package Manager の Git URL からインストールできます。package はリポジトリ直下ではなく `Packages/com.mizotake.syncfreed` にあるため、`path` query を付けます。

```text
https://github.com/MizoTake/UnitySyncFreeD.git?path=/Packages/com.mizotake.syncfreed
```

Package Manager window から入れる場合は、`Window > Package Manager` を開き、`+` から `Add package from git URL...` を選んで上記 URL を入力します。

`Packages/manifest.json` に直接追加する場合は、`dependencies` に次を追加します。

```json
{
  "dependencies": {
    "com.mizotake.syncfreed": "https://github.com/MizoTake/UnitySyncFreeD.git?path=/Packages/com.mizotake.syncfreed"
  }
}
```

revisionを固定する場合は公開済みtagまたはcommit hashを末尾に付けます。branch名は更新されるため再現可能な固定ではありません。

```text
https://github.com/MizoTake/UnitySyncFreeD.git?path=/Packages/com.mizotake.syncfreed#<commit-hash>
```

## Package

- Package name: `com.mizotake.syncfreed`
- Package root: `Packages/com.mizotake.syncfreed`
- Unity version: `2022.3` or newer
- Package version: `0.2.0`
- License: MIT

## Free-D Input Compatibility

`0.2.0` はFree-D message typeを分類し、D1入力をraw unsigned 24-bit、従来のpackage物理値形式、stable-ID built-in device profile、データ駆動custom profileとして扱います。一般Free-DはZoom / Focusの物理単位を規定しないため、未知機種では既定のraw保持を起点に校正します。受信Cameraにはframe-rate-independentなPosition / Rotation / Lens smoothingを任意で有効化できます。

公開tagはリポジトリに存在するものだけを固定URLへ使用してください。上記のGit URLは現在のbranchを参照し、再現可能な配布には検証済みcommit hashまたは公開済みtagを末尾へ付けます。

## Documentation

- Package README: `Packages/com.mizotake.syncfreed/README.md`
- Overview: `Packages/com.mizotake.syncfreed/Documentation~/Overview.md`
- Samples: `Packages/com.mizotake.syncfreed/Samples~`
