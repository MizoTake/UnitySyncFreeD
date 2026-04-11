# SharedPresets

このフォルダには、sample scene から共通参照する ScriptableObject preset を置いています。

- `LocalLoopbackOutput.asset`: `127.0.0.1:40000` に送る送信設定
- `LocalLoopbackInput.asset`: `127.0.0.1:40000` を待ち受ける loopback 受信設定
- `FreeDMulticastInput.asset`: `239.10.10.10:41030` を待ち受ける multicast 受信設定
- `DefaultSyncBehaviour.asset`: `SyncFreeDBehaviour` の既定挙動設定
- `ComfortController.asset`: sample 確認用の controller 操作設定

使い分け:

- ネットワーク設定は output / input preset にまとめる
- 挙動設定は `DefaultSyncBehaviour` や `ComfortController` にまとめる
- scene では preset を参照し、IP address や port を component へ直書きしない
