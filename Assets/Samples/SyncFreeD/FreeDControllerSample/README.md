# FreeDControllerSample

`Scenes/FreeDControllerSample.unity` は controller 操作で camera を動かし、loopback まで含めて Free-D を確認する sample です。

用途:

- controller からの transform / lens 操作
- Canonical State 更新
- Free-D D1 packet build
- UDP unicast 出力
- loopback 確認

シーン構成:

- `FreeD Controller Camera`: camera / source / sync / controller
- `Sample Output`: `LocalLoopbackOutput` preset を参照する UDP output
- `Sample Debug HUD`: packet preview と diagnostics
- `Sample Loopback Receiver`: `LocalLoopbackInput` preset を参照する loopback receiver
- `Sample Visual Rig`: camera movement の変化を見る marker 群
- `SyncFreeDBehaviour` は `DefaultSyncBehaviour`、controller は `ComfortController` preset を参照

操作:

- `W` `A` `S` `D`: 前後左右移動
- `Q` `E`: 上下移動
- `Arrow Keys`: pan / tilt
- `Z` `X`: roll
- `PageUp` `PageDown`: focal length
- `Home` `End`: focus distance
- `R`: pose / lens reset
- `Shift`: boost

1. sample を import します。
2. `Scenes/FreeDControllerSample.unity` を開きます。
3. `FreeD Controller Camera` が `DefaultSyncBehaviour`、`Sample Output` が `LocalLoopbackOutput`、`Sample Loopback Receiver` が `LocalLoopbackInput` を参照していることを確認します。
4. Play Mode に入ります。
5. `Sample Visual Rig` を見ながらキー操作で camera を動かし、緑の `Near Target`、黄色の `Center Tower`、奥の `Depth Pole` の見え方が大きく変わることを確認します。
6. packet preview と diagnostics、loopback 受信が更新されることを確認します。
7. `Tools/SyncFreeD/Debug Controller` を開くと `FreeD Controller Camera` を直接 debug apply できるので、`Pan +` や `Zoom +` でも camera transform / lens の変化を確認できます。
