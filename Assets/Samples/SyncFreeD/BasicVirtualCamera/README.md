# BasicVirtualCamera

`Scenes/BasicVirtualCamera.unity` は、Unity Camera の動きがそのまま Free-D として送られる最小構成の sample です。

確認できる処理:

- Unity Camera の動きから Canonical State を作る
- Free-D D1 packet build
- UDP で 1 か所に送る

1. `Assets/Samples/SyncFreeD/BasicVirtualCamera/Scenes/BasicVirtualCamera.unity` を開きます。
2. `SyncFreeDBehaviour` と `FreeDUdpOutputBehaviour` を確認します。
3. Play Mode に入ります。
4. 画面左上の packet preview と `127.0.0.1:40000` への UDP 送信を確認します。
