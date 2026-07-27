# Changelog

## 0.2.0 - 2026-07-27

- Added generic Free-D message categories, D1-specific input decoding, fail-closed raw D1 defaults, and stable-ID built-in device profiles without vendor-specific rig branches.
- Preserve raw D1 pan, tilt, roll, XYZ, zoom, focus, user data, and checksum values while keeping physical lens conversion optional.
- Added generic scale, normalized, reciprocal, and piecewise-linear lens field decoders with profile validation for custom camera and lens formats.
- Added Sony BRC-X1000 optical/effective zoom separation, VISCA focus reference conversion, and an effective 16:9 sensor gate derived from Sony's published 64.6-degree horizontal field of view.
- Isolated the Sony BRC-X1000 firmware 2.10 conversion table behind built-in profile ID `sony-brc-x1000-fw-2.10`; D1 parser, capability routing, smoothing, and rig application remain device-independent.
- Apply Free-D position and roll only when the selected profile declares those capabilities, and support installation-relative pan/tilt/roll pivot transforms for PTZ rigs.
- Make raw inspection and invalid custom profiles fail closed with no pose/lens capabilities, validate capability/decoder consistency, preserve Free-D metadata through composite sources, and fall back safely from incomplete PTZ rigs.
- Apply profile sensor dimensions and effective focal length to Unity physical cameras while retaining the original package builder/parser encoding as `SyncFreeDPhysicalV1`.
- Add optional frame-rate-independent exponential smoothing for driven-camera position, rotation, focal length, and focus distance; the default remains immediate application.
- Default driven-camera rotation to installation-relative PTZ application; select `Absolute` to retain the 0.1.0 single-transform behavior.

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
