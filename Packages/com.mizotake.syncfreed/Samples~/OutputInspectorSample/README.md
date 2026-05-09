# OutputInspectorSample

`Scenes/OutputInspectorSample.unity` は packet preview、diagnostics、recording、loopback 導線を分けて確認する sample です。

用途:

- Free-D D1 packet inspection
- UDP output / loopback
- diagnostics / checksum / user area / network warning
- debug log / recording output の確認

シーン構成:

- `Output Inspector Camera`: camera、source、sync、UDP output、packet preview、diagnostics、debug/recording output
- `Sample Visual Rig`: 構図差分を見る marker 群

1. sample を import します。
2. `Scenes/OutputInspectorSample.unity` を開きます。
3. `Output Inspector Camera` に source、sync、UDP output、packet preview、diagnostics、debug/recording output が付いていることを確認します。
4. Play Mode に入ります。
5. `Sample Visual Rig` の黄色の `Center Tower` と青/赤の左右 marker が camera movement に応じて変化することを確認します。
6. `SyncFreeDPacketPreviewBehaviour` と `SyncDiagnosticsBehaviour` を確認します。
7. packet preview、diagnostics、recorded frame count、checksum、user area、network warning が期待通りに更新されることを確認します。
