# SyncModel

`CameraSyncState` は `Command`, `Predicted`, `Observed`, `Corrected` を分けて保持します。

- `VirtualMaster`: command/predicted 優先
- `RealMaster`: observed 優先
- `DualDrive`: 操作中は predicted と observed を補正し、入力停止後は observed へ最終着地
- `ExternalTrackingMaster`: observed 優先
- `ReplayMaster`: replay の predicted 優先

`SyncTuningProfile` では gain, snap threshold, delay を調整できます。
