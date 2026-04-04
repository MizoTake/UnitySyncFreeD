# ReplaySample

`Scenes/ReplaySample.unity` は、JSON / CSV の再生データが Free-D packet に変わる流れを確認する sample です。

確認できる処理:

- Replay source からの Canonical State 生成
- `ReplayTrack.json` / `ReplayTrack.csv` の読み込み
- frame step によるフレーム送り
- Output Pose 切り替え
- Free-D D1 packet build

1. `Assets/Samples/SyncFreeD/ReplaySample/Scenes/ReplaySample.unity` を開きます。
2. `ReplayCameraSourceBehaviour` の `replayDataAsset` に `ReplayTrack.json` が設定されていることを確認します。
3. Play Mode に入ります。
4. `Prev` / `Next` でフレーム送りし、packet preview が追従することを確認します。
5. CSV を確認したい場合は `ReplayTrack.csv` に差し替えて `Reload` を押します。
