# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [3.1.0] — 2026-07-03

### ⚠️ Breaking / Requirements
- **Requires CounterStrikeSharp v1.0.369 or newer** (the .NET 10 runtime line). Servers still running a .NET 8 CounterStrikeSharp build will not load this version.
- Target framework migrated `net8.0` → `net10.0`; `CounterStrikeSharp.API` pinned to `1.0.370` (`MinimumApiVersion(370)`).

### Fixed
- **Weapon preferences could load the wrong team's choice after reconnect** — SQL JOINs on `UserId` only (missing `Team` condition) in both SQLite and PostgreSQL repositories; queries rewritten per-table with proper team filtering.
- **`!guns` Mid primary and Pistol selections were persisted with `Team=0`** (team was parsed from `weapon_xxx` instead of the menu label) and therefore lost on reconnect. Players may need to re-select their Mid/Pistol preference once.
- Crash (`ArgumentOutOfRangeException`) at the end of a round-type sequence without a `-1` entry.
- `retake.cfg` could be re-executed every tick during map start (gamerules fetch inside `OnTick`); possible `NullReferenceException` on missing gamerules proxy.
- `FormatException` in logging when player-controlled text contained `{`/`}` (e.g. buy command arguments).
- Duplicate `!guns` hint timers after config hot reload; plugin hot reload no longer reloads the whole map (uses `mp_restartgame 1`).
- Unclosed database readers leaking handles on long-running servers.

### Changed
- **Dead native-buy code removed** (~600 lines: buy interception, `item_pickup` capture, numeric payload resolvers). Weapon selection is `!guns` only; `retake.cfg` now sets `mp_buytime 0` and `mp_buy_anywhere 0` so the client buy menu no longer opens.
- Database schema hardened: legacy duplicate rows deduplicated, `UNIQUE(UserId, Team)` index added, `INSERT ... ON CONFLICT DO UPDATE` upserts; one pooled connection per operation (thread-safe).
- Player preference loading at connect now runs off the game thread (no more server hitch with a remote PostgreSQL).
- `OnTick` work throttled to ~4 checks/second instead of 64.
- Dependencies: `Npgsql 10.0.3`, `Microsoft.Data.Sqlite 10.0.9`, plus a direct `SQLitePCLRaw.bundle_e_sqlite3 3.0.3` reference fixing CVE-2025-6965 (bundled SQLite < 3.50.2).
- **Abandoned `CSZoneNet.Plugin.*` NuGet packages internalized** under `CS2Retake/Vendor/CSZoneNet/` (sources from the original GitHub repos, public API verified identical by reflection). The `CSZoneNet.*.dll` files are no longer shipped — delete leftovers when updating an existing server install.

---

## [3.0.1] — 2026-03-21

### Changed
- **Native buy menu integration removed** — weapon selection is now fully handled through `!guns` and its aliases.
- **Documentation update** — README updated to reflect current behavior and remove native buy flow references.

### Notes
- AWP preference and weapon selection remain available through `!guns`.
- Persistence model (SQLite / PostgreSQL) is unchanged.

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
