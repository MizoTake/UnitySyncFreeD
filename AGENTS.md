# AGENTS.md

## このリポジトリの目的

- このプロジェクトは Unity 向け UPM package `com.mizotake.syncfreed` を実装するためのものです。
- 主目的は `Free-D` を中心にした camera sync / packet output 基盤を Unity 内で扱えるようにすることです。
- 現在の優先対象は `Free-D` 実装です。
- `VISCA` 実機連携は優先対象から外れており、必要最小限の adapter 契約だけを残します。

## この対話で固定された方針

- 返答は日本語。
- 断定は避け、結論は `unicli` のログ、テスト結果、参照ファイルを根拠にする。
- 小さく刻む。1 回の変更意図は絞る。
- `現状確認 -> 最小修正 -> テスト/検証 -> 説明` の順で進める。
- 既存の Pure C# core + Unity adapter + Editor + Samples + Tests の責務分割を崩さない。
- 新規依存は提案ベース。勝手に追加しない。
- 生成物 `Library/Temp/obj/bin` は触らない。
- 1 行が長くても不要な改行を避ける。

## Free-D 実装で優先してケアすること

- `SyncFreeD_spec.md` のうち Free-D 側を優先する。
- 重視する順は以下。
- `D1 29 byte packet`
- `checksum`
- `Camera ID / User Area / FrameModulo16`
- `UDP unicast / multi-destination unicast`
- `multicast の最小送信枠と運用支援`
- `output source 切り替え`
- `output delay の runtime 反映`
- `loopback による送受信確認`
- `lens correction と diagnostics の整合`

## 現在の到達点

- Free-D D1 packet builder は実装済み。
- UDP 出力は `SingleDestinationUnicast` / `MultiDestinationUnicast` / `Multicast` の最小実装あり。
- `OutputPoseKind.Blended` と `OutputDelayMs` の runtime 反映あり。
- lens は `CommandLens / PredictedLens / ObservedLens / CorrectedLens` に分離済み。
- `ZoomCorrectionGain` と `SnapThresholdZoom` は `CorrectedLens` に反映済み。
- `Documentation~/Overview.md` に Free-D 観点の概要、構成、到達点あり。
- `Testing.md` の件数は更新して維持する。

## Free-D に関する主要ファイル

- `SyncFreeD_spec.md`
- `Packages/com.mizotake.syncfreed/Runtime/Core/Outputs/FreeDPacketBuilder.cs`
- `Packages/com.mizotake.syncfreed/Runtime/Core/Outputs/FreeDOutput.cs`
- `Packages/com.mizotake.syncfreed/Runtime/UnityAdapters/Behaviours/FreeDUdpOutputBehaviour.cs`
- `Packages/com.mizotake.syncfreed/Runtime/UnityAdapters/Behaviours/SyncFreeDBehaviour.cs`
- `Packages/com.mizotake.syncfreed/Documentation~/Overview.md`
- `Packages/com.mizotake.syncfreed/Documentation~/Testing.md`

## unicli の使い方

### 基本コマンド

- compile
- `unicli exec Compile`

- EditMode テスト
- `unicli exec TestRunner.RunEditMode '{"assemblies":["com.mizotake.syncfreed.editor.tests"],"resultFilter":"all","stackTraceLines":20}'`

- PlayMode テスト
- `unicli exec TestRunner.RunPlayMode '{"assemblies":["com.mizotake.syncfreed.runtime.tests"],"resultFilter":"all","stackTraceLines":20}'`

- テスト列挙
- `unicli exec TestRunner.List '{"mode":"EditMode"}'`
- `unicli exec TestRunner.List '{"mode":"PlayMode"}'`

- Asset 再読込
- `unicli exec AssetDatabase.Import '{"paths":["Packages/com.mizotake.syncfreed/Runtime/..."]}'`

- Play Mode 状態確認
- `unicli exec PlayMode.Status`

### unicli 運用の実務ルール

- `unicli` は並列実行に弱い。`Compile`、`TestRunner`、`PlayMode` は基本的に直列で流す。
- 新規 `.cs` 追加後は `AssetDatabase.Import` で明示再読込すると `TestRunner.List` に反映されやすい。
- `PlayMode` 後に `Server not responding` や `Server disconnected, retrying...` が出やすい。
- 上記メッセージが出ても、テスト結果本文が返っていれば結果自体は有効として扱ってよい。
- `Cannot execute ... while in Play Mode` や `while compiling` が出たら、数秒待って再実行する。
- `Server is busy executing ...` が出たら、同時実行を疑って順番待ちにする。

## 具体的な連携方法

### 開発フロー

1. `SyncFreeD_spec.md` と `Documentation~/Overview.md` で対象差分を決める。
2. 対象ファイルを読む。
3. `apply_patch` で最小修正する。
4. 新規ファイルやテスト追加時は `unicli exec AssetDatabase.Import ...` を流す。
5. `unicli exec Compile` を流す。
6. 必要な `EditMode` / `PlayMode` テストを流す。
7. 件数が変わったら `Documentation~/Testing.md` を更新する。

### Unity との連携で見る場所

- runtime entry point
- `Packages/com.mizotake.syncfreed/Runtime/UnityAdapters/Behaviours/SyncFreeDBehaviour.cs`

- packet 出力
- `Packages/com.mizotake.syncfreed/Runtime/UnityAdapters/Behaviours/FreeDUdpOutputBehaviour.cs`

- diagnostics
- `Packages/com.mizotake.syncfreed/Runtime/UnityAdapters/Behaviours/SyncDiagnosticsBehaviour.cs`
- `Packages/com.mizotake.syncfreed/Editor/Windows/SyncFreeDDebugWindow.cs`

- samples
- `Packages/com.mizotake.syncfreed/Samples~/BasicVirtualCamera`
- `Packages/com.mizotake.syncfreed/Samples~/OutputInspectorSample`

## 今後も維持するべき注意点

- `VISCA` は Free-D 実装を補助する adapter 契約までで十分。主軸に戻さない。
- Free-D 側で変更したら packet builder、UDP output、diagnostics、samples、tests の整合を見る。
- `Documentation~/Overview.md` と `Documentation~/Testing.md` は実装実態と同期させる。
- `unicli` の不安定さを前提に、失敗時はすぐに設計判断をせず、再試行してログを確認する。
- 最終報告では、実際に確認できた `Compile` / `EditMode` / `PlayMode` の結果だけを述べる。
- PowerShell ではなく cmd.exe を使うこと
