# FreeDReceiveSample

`Scenes/FreeDReceiveSample.unity` は Free-D 受信で CG camera を動かす sample です。

確認できる処理:

- Free-D multicast 受信
- 受信した pose / focal length / focus distance の CG camera 反映
- Builtin Post Processing Stack v2 が入っている環境での `DepthOfField` focus distance 反映
- 受信待機時の multicast group / port 設定確認

1. sample を import します。
2. `Scenes/FreeDReceiveSample.unity` を開きます。
3. `FreeD Driven Camera` の `FreeDInputSourceBehaviour` で `239.10.10.10:41030` を待ち受ける設定になっていることを確認します。
4. Play Mode に入ります。
5. Game view は `FreeD Driven Camera` の映像です。Editor の `Tools/SyncFreeD/Debug Controller` を開くと scene 上の `FreeDInputSourceBehaviour` を自動検出して送信先がそろいます。pan / tilt / zoom のボタンで Free-D を送ると、`Sample Visual Rig` の `Near Target`、`Center Tower`、`Depth Pole` の見え方が変わることを確認します。
6. `com.unity.postprocessing` が project に入っている場合、sample の `BuiltinPostProcessDepthOfFieldTargetBehaviour` が `PostProcessLayer` / `PostProcessVolume` / `DepthOfField` を自動で組みます。sample 既定値は `Priority=100`, `Aperture=4`, `FocalLength=50`, `KernelSize=Medium` 相当です。`Focus +` / `Focus -` の操作に応じて被写界深度も変わります。
7. 既定では multicast を使います。必要なら sender 側と receiver 側の group / port を同じ値にそろえて変更します。
