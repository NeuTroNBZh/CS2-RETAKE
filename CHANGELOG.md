# Changelog

All notable changes to this project are documented in this file.

## v3.0.0 - 2026-03-18

### Added
- Native CS2 buy command interception (`buy`) for round-type aware weapon selection.
- Shared persistence model for native buy and `!guns` selection paths.
- Integrated `InstaDefuseManager` directly inside this plugin (no external instadefuse DLL required).
- Runtime language support for player-facing messages (`English`, `French`).

### Changed
- Version line moved from `2.2.0` to `3.0.0` for the new release series.
- AWP flow now enforces one winner per team and active-player threshold requirements.
- Pistol rounds now enforce kevlar without helmet.
- Defuse-kit distribution made configurable (`All`, `Quota`, `Chance`) with dedicated pistol settings.
- Runtime config refresh now updates static runtime feature state on config parse.

### Fixed
- Native buy AWP toggle now respects server-side AWP feature enable/disable setting.
- Synthetic `bomb_planted` event payload consistency for `userid`.
- Post-review hardening across buy path and config reload consistency.

### Security
- Updated `Npgsql` to `8.0.3` (first patched version for GHSA-x9vc-6hfv-hg8c).

### Credits
- Upstream CS2Retake base: https://github.com/LordFetznschaedl/CS2Retake
- Upstream cs2-instadefuse logic source: https://github.com/B3none/cs2-instadefuse
