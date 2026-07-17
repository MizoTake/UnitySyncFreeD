# Changelog

## Unreleased

- Rebuild UDP receive and send resources when an active input or output profile changes network identity, and stop output side effects while their component is disabled.
- Harden shared UDP receive dispatch against listener removal during callbacks, repeated release, invalid bind cleanup, equivalent configuration duplication, and unbounded per-frame draining.
- Drop obsolete multicast memberships when multicast is disabled or its group changes.
- Saturate finite out-of-range Free-D values before integer conversion and preserve valid fallback lens values when corrected lens data is not finite.
- Preserve lens validity through composite sources and use a component-local replay clock with predictable pause, resume, reload, and re-enable behavior.
- Allow component settings to be edited directly when a profile is not assigned or automatic profile application is disabled.
- Verify the actual `Samples~` payload by importing it into a temporary Assets location for EditMode reference checks and PlayMode startup checks.
- Added package documentation, changelog, license, repository, and author URLs to the package manifest.

## 0.1.0

- Added Free-D D1 29 byte packet build/parse, checksum, Camera ID, User Area, and FrameModulo16 support.
- Added UDP output for single destination unicast, multi-destination unicast, multicast, and loopback verification.
- Added Free-D UDP input, multicast receive, camera ID filtering, and driven camera pose/lens application.
- Added Unity camera, tracker, replay, controller, diagnostics, debug, recording, and profile assets.
- Added Package Manager samples for BasicVirtualCamera, ExternalTrackerSample, ReplaySample, OutputInspectorSample, FreeDControllerSample, and FreeDReceiveSample.
- Added EditMode and PlayMode coverage for core sync, packet encoding, UDP behaviour, diagnostics, samples, and documentation consistency.
- Selected MIT as the package license.
