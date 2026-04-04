# ExternalTrackerSample

`Scenes/ExternalTrackerSample.unity` は外部トラッカー起点の最小サンプルです。

確認できる処理:

- Tracker source からの Canonical State 生成
- Free-D D1 packet build
- tracker 起点の output

1. sample を import します。
2. `ExternalTrackerSample.unity` を開きます。
3. Play Mode に入ります。
4. `Sample Visual Rig` の青い `Left Marker`、黄色の `Center Tower`、赤い `Right Marker` が視界内で相対移動することを確認します。
5. `TrackerCameraSourceBehaviour` から生成された状態と packet preview を確認します。
