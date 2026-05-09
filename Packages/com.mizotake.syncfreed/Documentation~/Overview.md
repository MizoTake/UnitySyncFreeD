# Overview

`SyncFreeD` は Unity でカメラ状態を正規化し、同期・補正を経て Free-D 出力へ変換するための package です。

現在の実装には以下が含まれます。

- Canonical camera state
- Default synchronizer and corrector
- Free-D D1 packet builder
- Unity camera / tracker / replay source behaviours
- UDP output, debug output, recording output
- Editor inspector, debug window, setup wizard
- Basic / ExternalTracker / Replay / OutputInspector / FreeDController / FreeDReceive sample
