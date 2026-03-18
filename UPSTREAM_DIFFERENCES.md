# Upstream Differences and Attribution

This repository is based on and inspired by upstream projects. This file documents attribution and the major differences introduced in CS2-RETAKE V3.

## Upstream Projects

- LordFetznschaedl/CS2Retake
  - https://github.com/LordFetznschaedl/CS2Retake
- B3none/cs2-instadefuse
  - https://github.com/B3none/cs2-instadefuse

## Attribution

Many core architectural ideas and gameplay foundations come from the upstream CS2Retake project, while instant-defuse behavior was researched from the cs2-instadefuse implementation and then integrated into this plugin.

## Major V3 Differences

### Native Buy Integration

- Added pre-hook interception for native `buy` command.
- Native buy now writes persistent weapon preferences instead of immediate equip.
- Native buy and `!guns` now share one persistence model.

### Persistence and Selection Model

- Team + round-type scoped selection slots (`T/CT x Pistol/Mid/FullBuy`).
- Behavior persists through cache + DB flow used by allocator managers.

### AWP Control System

- AWP eligibility requires strictly more than 4 active players.
- Maximum one AWP winner per team per round.
- Unified toggle persistence model used in both menu flows.

### Equipment Rules

- Pistol rounds enforce kevlar-only (no helmet).
- CT defuse-kit distribution supports configurable modes:
  - `All`
  - `Quota`
  - `Chance`
- Dedicated pistol kit chance + minimum guaranteed CT option.

### Integrated InstaDefuse

- Standalone instadefuse dependency removed.
- Logic fused into `InstaDefuseManager` in this plugin.
- Scoped to active retake rounds outside warmup.
- Controlled via base config keys (`InstaDefuse*`).

### Hardening and Ops

- Runtime config parse path updates runtime feature state for consistency.
- Event payload consistency fixes for synthetic bomb events.
- Security maintenance: `Npgsql` patched to `8.0.3`.

## Release Policy

This repository follows a V3 release line:
- Tags: `v3.x.x`
- Release titles: `Release-3.x.x CS2-RETAKE`
