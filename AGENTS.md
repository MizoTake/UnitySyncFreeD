# AGENTS.md

## 基本方針

- 返答は日本語。
- 断定は避け、結論はログ、テスト結果、参照ファイルを根拠にする。
- `現状確認 -> 最小修正 -> テスト/検証 -> 説明` の順で進める。
- 1 回の変更意図は絞り、差分は最小にする。
- `.editorconfig` があるため、実装時はその設定を優先する。
- 1 行が長くても、既存スタイルに反する不要な改行は入れない。
- PowerShell ではなく `cmd.exe` を使う。

## リポジトリの目的

- Unity 向け UPM package `com.mizotake.syncfreed` を実装・保守する。
- 主対象は Free-D camera sync / packet output。
- VISCA 実機連携は主対象外。必要最小限の adapter 契約に留める。

## 設計境界

- `Runtime/Core`: Unity 非依存の Pure C# core。
- `Runtime/UnityAdapters`: Unity component との接続。
- `Runtime/Networking`: UDP transport、receive hub、configuration validation。
- `Editor`: inspector、debug/operator window、setup helper。
- `Samples~`: Package Manager で配布する sample source。
- `Tests/Editor`, `Tests/Runtime`: Unity Test Runner 用テスト。

既存の Pure C# core + Unity adapter + Editor + Samples + Tests の責務分割を優先する。新規依存は必要理由と代替案を添えて提案ベースにする。

## ドキュメント方針

- root `README.md`: GitHub 用の概要と UPM インストール手順。
- `Packages/com.mizotake.syncfreed/README.md`: Package Manager で見る package 概要。
- `Packages/com.mizotake.syncfreed/Documentation~/Overview.md`: 利用者向けの機能、構成、運用概要。
- `Packages/com.mizotake.syncfreed/CHANGELOG.md`: release note。
- `Packages/com.mizotake.syncfreed/LICENSE.md`: license。
- `Packages/com.mizotake.syncfreed/Third Party Notices.md`: 追加 notice が必要な配布物の有無。
- `Samples~/.../README.md`: sample ごとの起動・確認手順。

大きな仕様書は持たず、必要な情報は README と `Documentation~/Overview.md` に集約する。日時付きのテスト実行ログは Markdown に固定せず、最終報告で実行結果を述べる。

## Free-D 実装で見る場所

- `Packages/com.mizotake.syncfreed/Runtime/Core/Outputs/FreeDPacketBuilder.cs`
- `Packages/com.mizotake.syncfreed/Runtime/Core/Outputs/FreeDOutput.cs`
- `Packages/com.mizotake.syncfreed/Runtime/UnityAdapters/Behaviours/FreeDUdpOutputBehaviour.cs`
- `Packages/com.mizotake.syncfreed/Runtime/UnityAdapters/Behaviours/SyncFreeDBehaviour.cs`
- `Packages/com.mizotake.syncfreed/Runtime/UnityAdapters/Behaviours/SyncDiagnosticsBehaviour.cs`
- `Packages/com.mizotake.syncfreed/Editor/Windows/SyncFreeDDebugWindow.cs`
- `Packages/com.mizotake.syncfreed/Documentation~/Overview.md`

## unicli

Unity プロジェクトでは `unicli` が使えるか確認し、使用可能なら積極的に Compile / TestRunner / PlayMode 状態確認に使う。`unicli` は並列実行に弱いため、Compile、TestRunner、PlayMode は直列で流す。

基本コマンド:

- Compile: `unicli exec Compile`
- EditMode: `unicli exec TestRunner.RunEditMode`
- PlayMode: `unicli exec TestRunner.RunPlayMode`
- EditMode list: `unicli exec TestRunner.List '{"mode":"EditMode"}'`
- PlayMode list: `unicli exec TestRunner.List '{"mode":"PlayMode"}'`
- Asset import: `unicli exec AssetDatabase.Import '{"paths":["Packages/com.mizotake.syncfreed/Runtime/..."]}'`
- Play Mode status: `unicli exec PlayMode.Status`

`Server not responding` や `Server disconnected, retrying...` が出ても、テスト結果本文が返っている場合は結果本文を優先して扱う。`Cannot execute ... while in Play Mode`、`while compiling`、`Server is busy executing ...` が出た場合は待ってから直列で再実行する。

## 検証

- C# 変更後は `unicli exec Compile` を実行する。
- package の挙動に触れた場合は必要な EditMode / PlayMode テストを実行する。
- package 配布物を整理した場合は `unity-upm-package-maintainer` の audit を実行する。
- 最終報告では、実際に確認できた Compile / EditMode / PlayMode / audit の結果だけを述べる。

## Safety

- 生成物 `Library/Temp/obj/bin` は編集・コミットしない。
- ユーザーや他ツールの未コミット変更は戻さない。
- 新規依存追加、package rename、asmdef rename、public API 移動は事前に理由と影響を説明する。
