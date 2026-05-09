# ExternalTrackerSample

`Scenes/ExternalTrackerSample.unity` は外部トラッカー起点で Canonical State を作り、Free-D を送る sample です。

用途:

- Tracker source からの Canonical State 生成
- Free-D D1 packet build
- tracker 起点の output

シーン構成:

- `Tracked Camera`: tracker source、sync、UDP output、packet preview、diagnostics を持つ camera
- `Sample Visual Rig`: 構図差分を見る marker 群

1. sample を import します。
2. `Scenes/ExternalTrackerSample.unity` を開きます。
3. `Tracked Camera` に tracker source、`SyncFreeDBehaviour`、`FreeDUdpOutputBehaviour`、packet preview、diagnostics が付いていることを確認します。
4. Play Mode に入ります。
5. `Sample Visual Rig` の青い `Left Marker`、黄色の `Center Tower`、赤い `Right Marker` が視界内で相対移動することを確認します。
6. `TrackerCameraSourceBehaviour` から生成された状態と packet preview、diagnostics を確認します。
