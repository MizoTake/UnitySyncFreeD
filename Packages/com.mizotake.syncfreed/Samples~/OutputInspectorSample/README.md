# OutputInspectorSample

この sample は packet preview、diagnostics、loopback 受信確認のための sample です。

確認できる処理:

- Free-D D1 packet inspection
- UDP output / loopback
- diagnostics / checksum / user area / network warning

1. sample を import します。
2. `Scenes/OutputInspectorSample.unity` を開くか、`BasicVirtualCamera` を複製して利用します。
3. `Sample Visual Rig` の黄色の `Center Tower` と青/赤の左右 marker が camera movement に応じて変化することを確認します。
4. `SyncFreeDPacketPreviewBehaviour` と `SyncDiagnosticsBehaviour` の表示を確認します。
5. `FreeDLoopbackReceiverBehaviour` の `Received Count`、`Remote Endpoint`、受信 packet が増えることを確認します。
