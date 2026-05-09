# Testing

Unity Test Runner を前提にしています。

- EditMode: Core のエンコード、補正、同期、curve、recording/debug output、sample/documentation 導線
- PlayMode: Unity behaviour の起動、source behaviour、diagnostics、recording/debug output、UDP loopback、Free-D input apply、sample scene 起動

最近の確認結果:

- 2026-05-09 `unicli exec Compile`: 0 errors, 0 warnings
- 2026-05-09 `unicli exec TestRunner.RunEditMode --assemblies com.mizotake.syncfreed.editor.tests --resultFilter all --stackTraceLines 20`: 153 passed, 0 failed, 0 skipped
- 2026-05-09 `unicli exec TestRunner.RunPlayMode --assemblies com.mizotake.syncfreed.runtime.tests --resultFilter all --stackTraceLines 20`: 54 passed, 0 failed, 0 skipped
- 2026-05-09 `unicli exec BuildPlayer.Compile --target StandaloneWindows64`: 0 errors, 0 warnings, 10 assemblies

PlayMode 後に UniCli の `Server not responding` / `Server disconnected, retrying...` が出る場合があります。テスト結果本文が返っている場合は、結果本文を優先して扱います。
