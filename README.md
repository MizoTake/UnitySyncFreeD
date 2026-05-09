# UnitySyncFreeD

UnitySyncFreeD は Unity 向け UPM package `com.mizotake.syncfreed` の開発リポジトリです。

Free-D を中心にした camera sync / packet output 基盤を Unity で扱うための Runtime、Editor tooling、Samples、Tests を含みます。

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

branch、tag、commit を固定する場合は revision を末尾に付けます。

```text
https://github.com/MizoTake/UnitySyncFreeD.git?path=/Packages/com.mizotake.syncfreed#master
```

## Package

- Package name: `com.mizotake.syncfreed`
- Package root: `Packages/com.mizotake.syncfreed`
- Unity version: `2022.3` or newer
- License: MIT

## Documentation

- Package README: `Packages/com.mizotake.syncfreed/README.md`
- Overview: `Packages/com.mizotake.syncfreed/Documentation~/Overview.md`
- Samples: `Packages/com.mizotake.syncfreed/Samples~`
