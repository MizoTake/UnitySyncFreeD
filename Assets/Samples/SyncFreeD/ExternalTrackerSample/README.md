# ExternalTrackerSample

`Scenes/ExternalTrackerSample.unity` は外部トラッカー起点で Canonical State を作り、Free-D を送る sample です。

用途:

- Tracker source からの Canonical State 生成
- Free-D D1 packet build
- tracker 起点の output

シーン構成:

- `Tracked Camera`: tracker source と sync 本体だけを持つ camera
- `Sample Output`: `LocalLoopbackOutput` preset を参照する UDP output
- `Sample Debug HUD`: packet preview と diagnostics
- `Sample Visual Rig`: 構図差分を見る marker 群
- `SyncFreeDBehaviour` は `DefaultSyncBehaviour` preset を参照

1. sample を import します。
2. `Scenes/ExternalTrackerSample.unity` を開きます。
3. `Tracked Camera` が `DefaultSyncBehaviour`、`Sample Output` が `LocalLoopbackOutput` を参照していることを確認します。
4. Play Mode に入ります。
5. `Sample Visual Rig` の青い `Left Marker`、黄色の `Center Tower`、赤い `Right Marker` が視界内で相対移動することを確認します。
6. `TrackerCameraSourceBehaviour` から生成された状態と packet preview、diagnostics を確認します。
