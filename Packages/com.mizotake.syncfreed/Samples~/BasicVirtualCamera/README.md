# BasicVirtualCamera

`Scenes/BasicVirtualCamera.unity` に最小構成のサンプルシーンが含まれます。

確認できる処理:

- Canonical State 生成
- Free-D D1 packet build
- UDP unicast 出力

1. Package Manager から sample を import します。
2. `BasicVirtualCamera.unity` を開きます。
3. Play Mode に入ります。
4. 画面内の `Sample Visual Rig` にある黄色の `Center Tower`、緑の `Near Target`、紫の `Far Target` が見えることを確認します。
5. Camera を動かしたときに、これらの marker の見え方と遠近感が変わることを確認します。
6. 画面左上の packet preview と `127.0.0.1:40000` への UDP 送信を確認します。
