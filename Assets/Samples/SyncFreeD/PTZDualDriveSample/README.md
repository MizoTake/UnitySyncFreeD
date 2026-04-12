# PTZDualDriveSample

`Scenes/PTZDualDriveSample.unity` は `DualDrive` の最小 sample です。

用途:

- command / observed の 2 系統入力
- DualDrive correction
- Delay / Lens correction の確認

シーン構成:

- `PTZ DualDrive Camera`: sync 本体
- `Sample Output`: `LocalLoopbackOutput` preset を参照する UDP output
- `Sample Debug HUD`: packet preview と diagnostics

1. `Assets/Samples/SyncFreeD/PTZDualDriveSample/Scenes/PTZDualDriveSample.unity` を開きます。
2. `PTZ DualDrive Camera` が `DefaultSyncBehaviour`、`Sample Output` が `LocalLoopbackOutput` を参照していることを確認します。
3. `VISCA Command Target` と `VISCA Observed Target` の 2 つの目標を確認します。
4. Play Mode に入ります。
5. `VISCA Command Target` と `VISCA Observed Target` の差が `DualDrive` でどう補正されるかを packet preview と diagnostics で確認します。
6. `Tools/SyncFreeD/Debug Controller` を開き、`Pan +` や `Zoom +` を押して `PTZ DualDrive Camera` の transform / lens が変わることを確認します。
