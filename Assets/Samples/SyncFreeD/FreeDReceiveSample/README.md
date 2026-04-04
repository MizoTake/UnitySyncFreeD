# FreeDReceiveSample

`Scenes/FreeDReceiveSample.unity` は、Free-D 受信で CG camera を動かす sample です。

確認できる処理:

- Free-D multicast 受信
- 受信した pose / focal length / focus distance の CG camera 反映
- 受信待機時の multicast group / port 設定確認

1. `Assets/Samples/SyncFreeD/FreeDReceiveSample/Scenes/FreeDReceiveSample.unity` を開きます。
2. `FreeD Driven Camera` の `FreeDInputSourceBehaviour` が `239.10.10.10:41030` を待ち受ける設定になっていることを確認します。
3. Play Mode に入ります。
4. Editor の `Tools/SyncFreeD/Debug Controller` を開くと scene 上の `FreeDInputSourceBehaviour` を自動検出して送信先がそろいます。pan / tilt / zoom のボタンで Free-D を送ると、Game view の CG camera が追従することを確認します。
