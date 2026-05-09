# FreeDReceiveSample

`Scenes/FreeDReceiveSample.unity` は Free-D multicast を受けて CG camera を動かす sample です。

用途:

- Free-D multicast 受信
- 受信した pose / focal length / focus distance の CG camera 反映
- 同じ UDP port を複数の `FreeDInputSourceBehaviour` で共有し、`Camera ID` ごとに受信先を分ける運用確認
- Builtin Post Processing Stack v2 が入っている環境での `DepthOfField` focus distance 反映

シーン構成:

- `FreeD Driven Camera`: CG camera、Free-D input source、apply behaviour
- `Sample Visual Rig`: 受信結果の見え方を確認する marker 群

複数カメラ運用:

- 同じ UDP port を使う複数入力でも `Camera ID Filter` を変えると受信先を分けられます
- A/B 系統を作るときは `FreeDInputSourceBehaviour` を複製し、`Camera ID Filter` だけ切り替えます

1. sample を import します。
2. `Scenes/FreeDReceiveSample.unity` を開きます。
3. `FreeD Driven Camera` に `FreeDInputSourceBehaviour` と `FreeDDrivenCameraBehaviour` が付いていることを確認します。
4. Play Mode に入ります。
5. Game view は `FreeD Driven Camera` の映像です。Editor の `Tools/SyncFreeD/Debug Controller` を開くと scene 上の `FreeDInputSourceBehaviour` を自動検出して送信先がそろいます。
6. pan / tilt / zoom のボタンで Free-D を送ると、`Sample Visual Rig` の `Near Target`、`Center Tower`、`Depth Pole` の見え方が変わることを確認します。
7. `com.unity.postprocessing` が project に入っている場合、`BuiltinPostProcessDepthOfFieldTargetBehaviour` が `PostProcessLayer` / `PostProcessVolume` / `DepthOfField` を自動で組みます。`Focus +` / `Focus -` の操作に応じて被写界深度も変わります。
8. multicast group / port を変える場合は `FreeDInputSourceBehaviour` の受信設定を編集します。
