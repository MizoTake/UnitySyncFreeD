# PTZDualDriveSample

`Scenes/PTZDualDriveSample.unity` は `DualDrive` の最小 sample です。

確認できる処理:

- command / observed の 2 系統入力
- DualDrive correction
- Delay / Lens correction の確認

1. `Assets/Samples/SyncFreeD/PTZDualDriveSample/Scenes/PTZDualDriveSample.unity` を開きます。
2. `VISCA Command Target` と `VISCA Observed Target` の 2 つの目標を確認します。
3. Play Mode に入ります。
4. `VISCA Command Target` と `VISCA Observed Target` の差が `DualDrive` でどう補正されるかを packet preview と diagnostics で確認します。
