# FreeDReceiveSample

`Scenes/FreeDReceiveSample.unity` は Free-D multicast を受けて CG camera を動かす sample です。

用途:

- Free-D multicast 受信
- 受信した pose / focal length / focus distance の CG camera 反映
- 受信機器に応じた packet decode preset とraw値の確認
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

## Packet Decoding Preset

`FreeDInputSourceBehaviour` へ `FreeDUdpInputProfileAsset` を設定し、assetの `Packet Decoding Preset` を選びます。

- 未知機種のraw調査と既定値は `RawUnsigned24`
- package内の送受信確認は `SyncFreeDPhysicalV1`
- 検証済みdevice profileは `BuiltInDeviceProfile` と `BuiltIn Packet Decoding Profile Id`
- ベンダー固有LUTや式は `Custom`

`RawUnsigned24` は全capabilityを無効にしてTransform / Cameraへ値を適用せず、`LastPacketHex` と `LastAppliedFrame.RawFreeD` へD1のraw fieldを保持します。`Custom` でも `RawFreeD` を使って元のZoom / Focus / User16を確認できます。Zoom値0は有効値になり得るため、0だけを欠損判定に使わないでください。

## Capability-Based D1 PTZ Rig

D1はmessage categoryであり、実際に有効なPan / Tilt / Roll / XYZ / Zoom / Focus / Irisは選択profileのcapabilityで決まります。Position / Rollを持たないPTZ profileで設置位置を保持するには、`FreeDDrivenCameraBehaviour` の `Pan Axis` と `Tilt Axis` に次の階層のTransformを割り当てます。

```text
Installation Root
└─ Pan Axis
   └─ Tilt Axis
      └─ Optical Center / Camera
```

軸Transformの初期local rotationを設置基準として保持し、Pan / Tiltを相対適用します。軸や光学中心の実寸offsetはカメラ個体・設置条件に合わせて校正してください。たとえばbuilt-in ID `sony-brc-x1000-fw-2.10` はPosition / Rollを宣言しませんが、リグ実装自体に機種分岐はありません。

## Motion Smoothing

D1の更新周期がUnityの描画fpsより低い場合、同じ値を数frame保持してから次値へ進む段差が見えることがあります。`FreeDDrivenCameraBehaviour` の `Enable Motion Smoothing` を有効にし、最初は次を試してください。

- Position Smoothing Half Life Seconds: `0.04`
- Rotation Smoothing Half Life Seconds: `0.04`
- Lens Smoothing Half Life Seconds: `0.06`

half-lifeを大きくすると滑らかになりますが、追従遅れも増えます。これは視覚的なlow-pass filterであり、Free-D / SDI映像間のDelay校正ではありません。正確なAR同期では実機収録で遅延を測定し、追加遅れを許容できない場合は `Enable Motion Smoothing` を無効にしてください。runtimeからは `SetMotionSmoothing(...)` で同じ値を変更できます。
