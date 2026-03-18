# CS2-RETAKE V3 - Server Installation Guide

This guide explains how to install and run CS2-RETAKE V3 on a Counter-Strike 2 dedicated server using CounterStrikeSharp.

## 1. Prerequisites

- A working CS2 dedicated server.
- CounterStrikeSharp installed and loading plugins correctly.
- Access to your server files and restart rights.

## 2. Download Release Assets

Open the latest release page and download the package matching your deployment style:

- Full package (with templates): includes plugin binaries and config templates.
- No-config package: binaries only (for existing installations).

Release page:
- https://github.com/NeuTroNBZh/CS2-RETAKE/releases

## 3. Copy Files to Server

Extract the archive, then copy the included `addons` folder into your server `csgo` directory.

Expected target roots:
- `csgo/addons/counterstrikesharp/plugins/CS2Retake/`
- `csgo/addons/counterstrikesharp/configs/plugins/CS2Retake/`

## 4. First Start

1. Start or restart the server.
2. Confirm plugin load in server logs.
3. Validate command availability in-game (`!retakeinfo`, `!guns`).

## 5. Configure Base Plugin

Edit:
- `addons/counterstrikesharp/configs/plugins/CS2Retake/CS2Retake.json`

Key options:
- `PlantType`
- `RoundTypeMode`
- `RoundTypeSequence`
- `MessageLanguage`
- `InstaDefuse*` flags

## 6. Configure Allocator

Edit:
- `addons/counterstrikesharp/configs/plugins/CS2Retake/CommandAllocator/CommandAllocator.json`

Key options:
- `DefuseKitMode`
- `DefuseKitQuota`
- `DefuseKitChance`
- `PistolDefuseKitChance`
- `PistolDefuseKitGuaranteeMinimum`
- `DatabaseType` and DB connection settings

## 7. Verify Gameplay Features

Run a quick smoke test:

1. Pistol round: no helmet should be assigned.
2. CT kit distribution: follows configured mode.
3. Native buy: weapon clicks update selection persistence.
4. AWP restrictions: require more than 4 active players and one winner per team.
5. InstaDefuse: only active outside warmup and according to config flags.

## 8. Upgrade from Older Setup

If you previously used a separate instadefuse plugin:

1. Disable/remove standalone instadefuse DLL.
2. Keep only CS2-RETAKE V3 plugin.
3. Recheck `InstaDefuse*` keys in base config.

## 9. Troubleshooting

- Plugin not loading:
  - Check CounterStrikeSharp installation and API compatibility.
  - Ensure DLL exists under plugin path.
- Config changes not visible:
  - Restart server or trigger your standard plugin reload workflow.
- No persistence:
  - Validate DB settings in allocator config.

## 10. Support

Please report issues with:
- server environment details
- plugin version (`v3.x.x`)
- relevant logs and reproduction steps
