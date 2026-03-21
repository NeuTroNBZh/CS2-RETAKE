# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [3.0.0] — 2026-03-21

### Added
- **Native CS2 buy menu integration** — players can now use the built-in CS2 buy menu to select their weapon for the current round type; purchases are intercepted server-side and converted into persistent selections (no instant weapon give)
- **Weapon choice persistence** — selections are saved per player × team × round type via SQLite (default) or PostgreSQL, surviving map changes and server restarts
- **AWP restrictions** — AWP is locked unless 5+ active players are present; maximum 1 AWP per team per round; 30% chance by default (configurable); toggle ON/OFF via `!guns` or the native buy menu
- **Pistol round helmet removal** — helmets are stripped at round start on pistol rounds (kevlar vest unaffected)
- **Smart CT defuse kit distribution** — three modes: `All` (every CT), `Quota` (fixed count), `Chance` (per-player probability); dedicated pistol-round settings with optional guaranteed minimum
- **InstaDefuse integration** — fully self-contained `InstaDefuseManager`, no separate DLL required; based on [B3none/cs2-instadefuse](https://github.com/B3none/cs2-instadefuse)
  - Configurable blocking conditions: HE, Molotov, Inferno (with proximity distance)
  - Optional forced explosion when defuse time is insufficient
  - Chat notifications in English or French
- **Round type system** — Pistol / Mid / Full Buy round types with independent weapon pools, configurable sequence, random, or fixed mode
- **`!guns` weapon selection menu** — ChatMenu-based weapon picker for all round types and teams
- **Zeus distribution** — optional random Zeus taser (configurable chance %)
- **Language support** — chat messages in English and French (`MessageLanguage` config key)
- **`HowToMessage`** — periodic in-game reminder about `!guns` (configurable delay and text)
- **PostgreSQL support** — optional alternative to SQLite for weapon persistence
- **Debug mode** — verbose server console logging via `EnableDebug`

### Changed
- Forked from [LordFetznschaedl/CS2Retake](https://github.com/LordFetznschaedl/CS2Retake) as the base
- Allocator extended from `CommandAllocator` with full ChatMenu support and DB persistence
- `Npgsql` upgraded to `8.0.3` (patches security advisory GHSA-x9vc-6hfv-hg8c)
- `Microsoft.Data.Sqlite` upgraded to `8.0.3`
- All compiler warnings resolved (nullable annotations, dead variable cleanup)

### Fixed
- **Buy menu regression** — players could receive weapons (e.g. AK-47 in pistol round) via native buy; `buy` listener now returns `HookResult.Handled` to fully block the real purchase
- **Glock/HKP2000 overwrite bug** — auto-spawn pickups no longer overwrite saved weapon preferences
- **M4A1-S vs M4A4 misidentification** — `EventItemPickup` now uses `Defindex` as primary resolver to correctly distinguish silenced and unsilenced M4 variants
- **Weapon pool completeness** — plugin injects a baseline of missing weapons at load time so `!guns` and the buy menu share a consistent, complete pool regardless of JSON file version

---

## [2.x] — Upstream

For changes prior to v3.0.0, refer to the upstream repository:  
[github.com/LordFetznschaedl/CS2Retake](https://github.com/LordFetznschaedl/CS2Retake/blob/main/CHANGELOG.md)
