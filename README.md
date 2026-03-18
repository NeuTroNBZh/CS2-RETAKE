# CS2-RETAKE (V3)

Professional CS2 Retake plugin built on CounterStrikeSharp, with integrated native buy interception, persistent weapon preferences, advanced AWP rules, configurable CT kit distribution, and integrated instant-defuse logic.

This project is intended for international use and production servers.

## Requirements

- CounterStrikeSharp API: minimum 228
- Game mode: Casual or Competitive
- .NET runtime compatible with CounterStrikeSharp plugins

## Highlights (V3)

- Native CS2 buy menu interception (`buy` command listener) with selection-only behavior
- Shared persistence between native buy flow and `!guns` flow
- 6-slot weapon preference model per player (`T/CT x Pistol/Mid/FullBuy`)
- AWP restrictions:
  - disabled unless strictly more than 4 active players (`T + CT`)
  - max 1 AWP winner per team per round
  - toggle preference with unified `0/30` persistence model
- Pistol round armor change: kevlar only (no helmet)
- Smart CT defuse-kit distribution with configurable modes
- Integrated InstaDefuse manager (no separate DLL required)
- EN/FR runtime message language support

## Commands

| Command | Parameters | Description | Permission |
|---|---|---|---|
| `!guns` | - | Opens weapon preference menus (`css_guns` aliases supported) | - |
| `!retakeinfo` | - | Prints plugin information | - |
| `!retakespawn` | `<index>` | Teleports player to spawn index | `@cs2retake/admin` |
| `!retakewrite` | - | Saves spawns for current map | `@cs2retake/admin` |
| `!retakeread` | - | Loads spawns for current map | `@cs2retake/admin` |
| `!retakescramble` | - | Flags team scramble | `@cs2retake/admin` |
| `!retaketeleport` | `<x> <y> <z>` | Teleports player to coordinates | `@cs2retake/admin` |
| `!retakeaddspawn` | `<team> <site>` | Adds a spawn (`2=T`, `3=CT`; `0=A`, `1=B`) | `@cs2retake/admin` |

## Installation

1. Download the release assets for your platform.
2. Copy the plugin files into your CS2 dedicated server `csgo` directory.
3. Ensure this DLL is loaded under CounterStrikeSharp plugin loading path.
4. Start/restart the server once to generate default config files.
5. Edit configs, then restart or hot-reload as needed.

Typical config paths:

- Base config: `addons/counterstrikesharp/configs/plugins/CS2Retake/CS2Retake.json`
- Allocator config: `addons/counterstrikesharp/configs/plugins/CS2Retake/CommandAllocator/CommandAllocator.json`

## Core Configuration

Base config (`CS2Retake.json`) includes:

- `PlantType`: `AutoPlant` or `FastPlant`
- `RoundTypeMode`: `Sequence`, `Specific`, `Random`
- `RoundTypeSequence`: weighted round flow (for sequence mode)
- `RoundTypeSpecific`: fixed mode value
- `Allocator`: allocator implementation selector
- `MessageLanguage`: `English` or `French`
- Integrated InstaDefuse toggles:
  - `InstaDefuseEnabled`
  - `InstaDefuseRequireNoTAlive`
  - `InstaDefuseBlockOnHe`
  - `InstaDefuseBlockOnMolotov`
  - `InstaDefuseBlockOnInferno`
  - `InstaDefuseInfernoDistance`
  - `InstaDefuseForceExplodeIfNoTime`
  - `InstaDefuseChatNotification`

Command allocator config includes:

- `DefuseKitMode`: `All`, `Quota`, `Chance`
- `DefuseKitQuota`
- `DefuseKitChance`
- `PistolDefuseKitChance`
- `PistolDefuseKitGuaranteeMinimum`
- `EnableZeus` and `ZeusChance`
- DB mode (`SQLite` or `PostgreSQL`)

## What Is Different vs Upstream Projects

### Compared to LordFetznschaedl/CS2Retake

- Added native CS2 buy interception and round-type aware selection persistence
- Unified AWP toggle logic across `!guns` and native buy behavior
- Added strict AWP eligibility logic (`>4` active players, one winner per team)
- Improved pistol equipment behavior (no helmet in pistol rounds)
- Added configurable smart CT defuse-kit distribution model
- Added runtime-safe config refresh behavior on config parse
- Added consistency hardening for synthetic `bomb_planted` event payload

### Compared to B3none/cs2-instadefuse

- Logic is fused into `InstaDefuseManager` inside this plugin
- No secondary plugin DLL required
- Behavior is scoped to active retake rounds outside warmup
- Integrated with shared runtime config and plugin messaging

## Credits and Upstream Authors

This project builds on excellent open-source work from:

- LordFetznschaedl: https://github.com/LordFetznschaedl/CS2Retake
- B3none: https://github.com/B3none/cs2-instadefuse

Additional inspiration:

- splewis: https://github.com/splewis/csgo-retakes

## Release Strategy (V3 series)

Release naming follows the upstream style, adapted for this repository:

- Title format: `Release-3.x.x CS2-RETAKE`
- Tag format: `v3.x.x`
- First release of this branch: `v3.0.0`

See `CHANGELOG.md` and `RELEASE_NOTES_V3.0.0.md` for the initial V3 release payload.

## Documentation

- Step-by-step server guide: [INSTALLATION.md](INSTALLATION.md)
- Upstream attribution and technical deltas: [UPSTREAM_DIFFERENCES.md](UPSTREAM_DIFFERENCES.md)


