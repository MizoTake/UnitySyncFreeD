# SharedPresets

このフォルダには、sample から再利用する `ScriptableObject` preset を置いています。

- `LocalLoopbackOutput.asset`: `127.0.0.1:40000` に送る送信設定
- `ComfortController.asset`: sample 確認用の controller 操作設定

sample scene で `送信設定 Asset` や `操作 preset Asset` に割り当てると、scene を複製しなくても同じ設定を再利用できます。
