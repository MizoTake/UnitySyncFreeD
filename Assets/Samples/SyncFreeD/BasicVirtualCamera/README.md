# BasicVirtualCamera

`Scenes/BasicVirtualCamera.unity` は Unity Camera から Canonical State を作り、Free-D D1 packet を UDP 送信する最小 sample です。

用途:

- Unity Camera source の最小確認
- Canonical State 生成
- Free-D D1 packet build
- UDP unicast 出力

シーン構成:

- `Main Camera`: source と sync 本体だけを持つ camera
- `Sample Output`: `LocalLoopbackOutput` preset を参照する UDP output
- `Sample Debug HUD`: packet preview 表示
- `Sample Visual Rig`: 遠近と構図変化を確認する marker 群
- `SyncFreeDBehaviour` は `DefaultSyncBehaviour` preset を参照

1. sample を import します。
2. `Scenes/BasicVirtualCamera.unity` を開きます。
3. `Main Camera` が `DefaultSyncBehaviour`、`Sample Output` が `LocalLoopbackOutput` を参照していることを確認します。
4. Play Mode に入ります。
5. `Sample Visual Rig` の黄色の `Center Tower`、緑の `Near Target`、紫の `Far Target` が見えることを確認します。
6. Camera を動かしたときに、これらの marker の見え方と遠近感が変わることを確認します。
7. 画面左上の packet preview と `127.0.0.1:40000` への UDP 送信を確認します。
