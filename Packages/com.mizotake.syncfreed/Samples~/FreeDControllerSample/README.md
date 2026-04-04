# FreeDControllerSample

`Scenes/FreeDControllerSample.unity` は Free-D の controller 操作確認用 sample です。

確認できる処理:

- controller からの transform / lens 操作
- Canonical State 更新
- Free-D D1 packet build
- UDP unicast 出力

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
3. Play Mode に入ります。
4. `Sample Visual Rig` を見ながらキー操作で camera を動かし、緑の `Near Target`、黄色の `Center Tower`、奥の `Depth Pole` の見え方が大きく変わることを確認します。
5. packet preview と diagnostics と loopback 受信が更新されることを確認します。
