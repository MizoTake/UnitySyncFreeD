# OutputInspectorSample

この sample は「送れているか」「どこに送っているか」「受信できているか」を確認するための sample です。

確認できる処理:

- Free-D D1 packet inspection
- UDP output / loopback
- diagnostics / checksum / user area / network warning

1. `Assets/Samples/SyncFreeD/OutputInspectorSample/Scenes/OutputInspectorSample.unity` を開きます。
2. `FreeDUdpOutputBehaviour` の送り先と warning 表示を確認します。
3. `SyncFreeDPacketPreviewBehaviour` と `SyncDiagnosticsBehaviour` の表示を確認します。
4. `FreeDLoopbackReceiverBehaviour` の `Received Count`、`Remote Endpoint`、受信 packet が増えることを確認します。
