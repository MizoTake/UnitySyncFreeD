# ReplaySample

`Scenes/ReplaySample.unity` は JSON / CSV の記録データ再生を Free-D 出力へ流す sample です。

用途:

- Replay source からの Canonical State 生成
- `ReplayTrack.json` / `ReplayTrack.csv` の読み込み
- frame step によるフレーム送り
- Output Pose 切り替え
- Free-D D1 packet build

シーン構成:

- `Replay Camera`: replay source、sync、UDP output、packet preview、diagnostics
- `Sample Visual Rig`: 再生差分を見る marker 群

1. sample を import します。
2. `Scenes/ReplaySample.unity` を開きます。
3. `Replay Camera` に replay source、sync、UDP output、packet preview、diagnostics が付いていることを確認します。
4. `ReplayCameraSourceBehaviour` の `replayDataAsset` に `ReplayTrack.json` が設定されていることを確認します。
5. Play Mode に入ります。
6. `Sample Visual Rig` を見ながら `Prev` / `Next` でフレーム送りし、黄色の `Center Tower`、紫の `Far Target`、奥の `Depth Pole` の見え方が段階的に切り替わることを確認します。
7. JSON 読み込み済みの姿勢、packet preview、diagnostics が切り替わることを確認します。
8. CSV を確認したい場合は `ReplayTrack.csv` に差し替えて `Reload` を押します。
