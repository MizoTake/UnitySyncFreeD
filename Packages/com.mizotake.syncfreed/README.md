# SyncFreeD

SyncFreeD は Unity 向けの Free-D camera sync / packet output UPM package です。

- Runtime Core: canonical state、synchronizer、corrector、Free-D D1 packet builder/parser
- Unity Adapters: Unity Camera、Tracker、Replay、Controller、UDP input/output、debug/recording output
- Editor: custom inspector、debug window、operator window、setup wizard
- Samples: BasicVirtualCamera、ExternalTrackerSample、ReplaySample、OutputInspectorSample、FreeDControllerSample、FreeDReceiveSample
- Sample Visual Rig: every sample scene includes shared visual markers for movement checks
- Tests: EditMode / PlayMode

## Package Layout

- `Runtime/Core`: Unity に依存しない同期・補正・Free-D packet 処理
- `Runtime/UnityAdapters`: Unity scene / component との接続
- `Runtime/Networking`: UDP transport、receive hub、configuration validation
- `Editor`: inspector、window、setup helper
- `Samples~`: Package Manager から import する配布用 sample source
- `Documentation~`: Package Manager から参照する利用者向けドキュメント
- `Tests/Editor`, `Tests/Runtime`: Unity Test Runner 用テスト

## Samples

配布用 sample source は `Samples~` にあります。Package Manager から import すると、Unity は project 側の `Assets/Samples/...` 配下へ展開します。

この開発リポジトリには、検証用に import 済みの sample fixture と shared preset を `Assets/Samples/SyncFreeD` に保持しています。配布物としての一覧は `package.json` の `samples` 配列を正とします。

## Documentation

- `Documentation~/Overview.md`
- `Documentation~/Testing.md`

## License

MIT License.
