# SyncFreeD

Free-D only camera sync core and output package for Unity.

- Runtime Core: canonical state, synchronizer, corrector, Free-D D1 packet builder
- Unity Adapters: UnityCamera, Tracker, Replay, UDP/debug/recording outputs
- Editor: custom inspector, debug window, setup wizard
- Samples: BasicVirtualCamera, ExternalTrackerSample, ReplaySample, OutputInspectorSample, FreeDControllerSample
- Sample Visual Rig: every sample scene includes shared visual markers for movement checks
- Tests: EditMode and PlayMode

Documentation:

- `Documentation~/Overview.md`
- `Documentation~/Architecture.md`
- `Documentation~/FreeDReadiness.md`
- `Documentation~/SyncModel.md`
- `Documentation~/FreeDOutput.md`
- `Documentation~/Samples.md`
- `Documentation~/Testing.md`

## TAKT setup helper

This repository includes a PowerShell helper for checking a local `takt` environment and generating minimal config files.

Examples:

- `.\tools\takt\Invoke-TaktSetup.ps1 inspect`
- `.\tools\takt\Invoke-TaktSetup.ps1 init-project`
- `.\tools\takt\Invoke-TaktSetup.ps1 init-user-template`
- `.\tools\takt\Invoke-TaktSetup.ps1 report -Json`
