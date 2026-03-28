# PTZDualDriveSample

`Scenes/PTZDualDriveSample.unity` は `DualDrive` の最小 sample です。

1. sample を import します。
2. `PTZDualDriveSample.unity` を開きます。
3. Play Mode に入ります。
4. 必要なら `ViscaTelemetryProviderBehaviour` に外部実装から telemetry を流し込みます。
5. `ViscaCameraSourceBehaviour` が telemetry provider または `VISCA Command Target` / `VISCA Observed Target` を参照し、`DualDrive` 補正結果を packet preview と diagnostics で確認します。
