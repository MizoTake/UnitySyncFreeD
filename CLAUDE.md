# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.
詳細な方針は `AGENTS.md` も参照すること。

## Project Overview

Unity UPM package (`com.mizotake.syncfreed`) implementing Free-D camera sync and UDP packet output for virtual production. Unity 2022.3 LTS. Root namespace: `MizoTake.SyncFreeD`. No external dependencies.

Free-D camera sync and UDP packet output に特化したパッケージ。VISCA 実装は本パッケージに含めない。

## Language and Style

- 返答は日本語。
- 断定は避け、結論は `unicli` のログ、テスト結果、参照ファイルを根拠にする。
- 1 行が長くても不要な改行を避ける。

## Build and Test Commands

All commands use `unicli` and must run **serially** (no parallel execution). PowerShell ではなく bash / cmd.exe で実行すること。

```bash
# Compile
unicli exec Compile

# EditMode tests (pure C# core, editor tools)
unicli exec TestRunner.RunEditMode '{"assemblies":["com.mizotake.syncfreed.editor.tests"],"resultFilter":"all","stackTraceLines":20}'

# PlayMode tests (MonoBehaviour integration)
unicli exec TestRunner.RunPlayMode '{"assemblies":["com.mizotake.syncfreed.runtime.tests"],"resultFilter":"all","stackTraceLines":20}'

# List available tests
unicli exec TestRunner.List '{"mode":"EditMode"}'
unicli exec TestRunner.List '{"mode":"PlayMode"}'

# Force asset reimport (required after adding new .cs files)
unicli exec AssetDatabase.Import '{"paths":["Packages/com.mizotake.syncfreed/Runtime/..."]}'

# Check Play Mode state
unicli exec PlayMode.Status
```

### unicli quirks

- After PlayMode tests, `Server not responding` / `Server disconnected, retrying...` may appear — if test results are present in the output, they are valid.
- `Cannot execute ... while in Play Mode` or `while compiling` — wait a few seconds and retry.
- `Server is busy executing ...` — another command is running; queue sequentially.
- 失敗時はすぐに設計判断をせず、再試行してログを確認する。
- 最終報告では、実際に確認できた `Compile` / `EditMode` / `PlayMode` の結果だけを述べる。

## Architecture

**Pure C# Core + Unity Adapter** separation is the fundamental design constraint. Do not break this boundary.

### Assembly structure (4 assemblies)

| Assembly | Namespace | Depends on | Platform |
|---|---|---|---|
| `com.mizotake.syncfreed` | `MizoTake.SyncFreeD` | (none) | All |
| `com.mizotake.syncfreed.editor` | `MizoTake.SyncFreeD.Editor` | runtime | Editor |
| `com.mizotake.syncfreed.editor.tests` | `MizoTake.SyncFreeD.Tests` | runtime, editor | Editor |
| `com.mizotake.syncfreed.runtime.tests` | `MizoTake.SyncFreeD.Tests.Runtime` | runtime | All |

### Key layers

- **Runtime/Core/Abstractions/** — `ICameraSource`, `ICameraOutput`, `ICameraSynchronizer` 等のインターフェース。Unity 非依存。
- **Runtime/Core/Models/** — `CameraSyncState`（4 pose variants: Command/Predicted/Observed/Corrected + 対応する lens variants）等の値型。
- **Runtime/Core/Sync/** — 同期エンジン群。`CameraSyncEngine` がオーケストレータ。
- **Runtime/Core/Outputs/** — Free-D D1 29-byte packet 生成。24-bit big-endian encoding。
- **Runtime/Networking/** — UDP transport、configuration validator、NIC enumeration。
- **Runtime/UnityAdapters/Behaviours/** — MonoBehaviour ラッパー。Source 系・Output 系・Orchestration 系に分類される。
- **Editor/** — Inspector、Debug/Operator Window、Setup Wizard。
- **Samples~/** — 5 sample scenes。各シーンに共通の visual reference rig markers を含む。

## Free-D Implementation Priority

重視する順（`SyncFreeD_spec.md` のうち Free-D 側を優先）：

1. D1 29 byte packet
2. checksum
3. Camera ID / User Area / FrameModulo16
4. UDP unicast / multi-destination unicast
5. multicast の最小送信枠と運用支援
6. output source 切り替え
7. output delay の runtime 反映
8. loopback による送受信確認
9. lens correction と diagnostics の整合

## Current Implementation Status

- Free-D D1 packet builder 実装済み。
- UDP 出力は `SingleDestinationUnicast` / `MultiDestinationUnicast` / `Multicast` の最小実装あり。
- `OutputPoseKind.Blended` と `OutputDelayMs` の runtime 反映あり。
- lens は `CommandLens / PredictedLens / ObservedLens / CorrectedLens` に分離済み。
- `ZoomCorrectionGain` と `SnapThresholdZoom` は `CorrectedLens` に反映済み。

## Free-D Key Files

- `SyncFreeD_spec.md` — Full design spec
- `Packages/com.mizotake.syncfreed/Runtime/Core/Outputs/FreeDPacketBuilder.cs` — Packet builder
- `Packages/com.mizotake.syncfreed/Runtime/Core/Outputs/FreeDOutput.cs` — Output abstraction
- `Packages/com.mizotake.syncfreed/Runtime/UnityAdapters/Behaviours/FreeDUdpOutputBehaviour.cs` — UDP output
- `Packages/com.mizotake.syncfreed/Runtime/UnityAdapters/Behaviours/SyncFreeDBehaviour.cs` — Runtime entry point
- `Packages/com.mizotake.syncfreed/Documentation~/FreeDReadiness.md` — Completion checklist
- `Packages/com.mizotake.syncfreed/Documentation~/FreeDOutput.md` — Packet implementation details
- `Packages/com.mizotake.syncfreed/Documentation~/Testing.md` — Test inventory (keep counts in sync)

## Development Workflow

1. `SyncFreeD_spec.md` と `Packages/com.mizotake.syncfreed/Documentation~/FreeDReadiness.md` で対象差分を決める。
2. 対象ファイルを読む。
3. 最小修正する（1 回の変更意図は絞る）。
4. 新規 `.cs` 追加時は `unicli exec AssetDatabase.Import ...` を流す。
5. `unicli exec Compile` を流す。
6. 必要な EditMode / PlayMode テストを流す。
7. 件数が変わったら `Packages/com.mizotake.syncfreed/Documentation~/Testing.md` を更新する。

## Conventions

- `現状確認 -> 最小修正 -> テスト/検証 -> 説明` の順で進める。
- 新規依存は提案ベース。勝手に追加しない。
- 生成物 `Library/`, `Temp/`, `obj/`, `bin/` は触らない。
- Free-D 側で変更したら packet builder、UDP output、diagnostics、samples、tests の整合を見る。
- `Testing.md`、`FreeDOutput.md`、`FreeDReadiness.md` の 3 つは実装実態と同期させる。
- VISCA 実装は不要。本パッケージに含めない。
