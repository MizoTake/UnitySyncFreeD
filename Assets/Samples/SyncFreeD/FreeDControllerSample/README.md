# FreeDControllerSample

`Scenes/FreeDControllerSample.unity` は、キーボードで camera を動かしながら Free-D を確認する sample です。

確認できる処理:

- controller からの transform / lens 操作
- Canonical State 更新
- Free-D D1 packet build
- UDP unicast 出力
- 操作 preset の再利用

操作:

- `W` `A` `S` `D`: 前後左右移動
- `Q` `E`: 上下移動
- `Arrow Keys`: pan / tilt
- `Z` `X`: roll
- `PageUp` `PageDown`: focal length
- `Home` `End`: focus distance
- `R`: pose / lens reset
- `Shift`: boost

1. `Assets/Samples/SyncFreeD/FreeDControllerSample/Scenes/FreeDControllerSample.unity` を開きます。
2. まず `FreeDControllerBehaviour` の `操作 preset Asset` と `FreeDUdpOutputBehaviour` の `送信設定 Asset` を確認します。
3. Play Mode に入ります。
4. キー操作で camera を動かし、packet preview と diagnostics と loopback 受信が更新されることを確認します。
