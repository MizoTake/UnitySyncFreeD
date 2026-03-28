# SyncModel

`CameraSyncState` は `Command`, `Predicted`, `Observed`, `Corrected` を分けて保持します。

- `VirtualMaster`: command/predicted 優先
- `RealMaster`: observed 優先
- `DualDrive`: predicted と observed を補正器でブレンド
- `ExternalTrackingMaster`: observed 優先
- `ReplayMaster`: replay の predicted 優先

`SyncTuningProfile` では gain, snap threshold, delay を調整できます。
