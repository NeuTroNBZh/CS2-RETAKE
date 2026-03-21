<div align="center">

# CS2 Retake V3

**A feature-rich CS2 retake plugin for CounterStrikeSharp**

[![Release](https://img.shields.io/github/v/release/NeuTroNBZh/CS2-RETAKE?style=flat-square&label=Release&color=brightgreen)](https://github.com/NeuTroNBZh/CS2-RETAKE/releases/latest)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg?style=flat-square)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple?style=flat-square)](https://dotnet.microsoft.com/)
[![CounterStrikeSharp](https://img.shields.io/badge/CounterStrikeSharp-1.0.228-orange?style=flat-square)](https://github.com/roflmuffin/CounterStrikeSharp)
[![CS2](https://img.shields.io/badge/Game-CS2-yellow?style=flat-square)](https://www.counter-strike.net/)

*Built on the solid foundation of [LordFetznschaedl/CS2Retake](https://github.com/LordFetznschaedl/CS2Retake) — extended with a full weapon selection system, AWP restrictions, InstaDefuse fusion, and more.*

</div>

---

## ✨ Features

### 🔫 Weapon System
| Feature | Description |
|---------|-------------|
| **`!guns` weapon menu** | In-game ChatMenu to choose your weapon per team and round type |
| **Persistent weapon choices** | Selections saved per player × team × round type via SQLite or PostgreSQL, surviving map changes and server restarts |
| **3 round types** | Independent weapon pools for **Pistol**, **Mid**, and **Full Buy** rounds |
| **AWP restrictions** | AWP only available with 5+ active players · max 1 AWP per team · configurable chance (default 30%) · toggle ON/OFF via `!guns` |
| **Helmet removal on pistol rounds** | Helmets are automatically stripped at the start of every pistol round (kevlar kept) |

### 💣 Retake Gameplay
| Feature | Description |
|---------|-------------|
| **Auto Plant** | Bomb is automatically planted at round start by a designated T player |
| **Fast Plant** | Alternative instant plant mode |
| **Spot Announcer** | Optional center text announcement of the bombsite |
| **Spawn system** | Per-map JSON spawn files for CT and T positions on each bombsite |
| **8 pre-configured maps** | de_dust2, de_mirage, de_inferno, de_ancient, de_anubis, de_nuke, de_overpass, de_vertigo |

### 🛡️ CT Kit Distribution
Three configurable modes for defuse kit assignment:
- **All** — every CT receives a kit (default)
- **Quota** — exactly N kits distributed per round
- **Chance** — per-player probability-based distribution

Pistol round has its own dedicated probability setting with optional guaranteed minimum.

### ⚡ InstaDefuse (built-in)
Fully integrated, no separate DLL required. Based on [B3none/cs2-instadefuse](https://github.com/B3none/cs2-instadefuse).
- Instant defuse when no Terrorists are alive (configurable)
- Blocked by HE grenade, Molotov, Inferno (configurable per type)
- Configurable inferno proximity distance
- Forced explosion if defuse time would be insufficient
- Chat notifications (English / French)

### 👥 Team & Queue Management
| Feature | Description |
|---------|-------------|
| **Player queue** | Automatic queue when server is full (`MaxPlayers` cap) |
| **Team balancing** | Configurable T/CT ratio (default 45% T) |
| **Auto scramble** | Teams scrambled after N consecutive CT or T wins |
| **Round-win switch** | Winners rotate out to keep games fair |

### ⚙️ Quality of Life
- **No-drop fix** — weapon selection is declarative; no weapons hit the floor during selection
- **Configurable round type sequences** — Pistol → Mid → FullBuy, fully customizable
- **Zeus support** — optional random Zeus distribution
- **Language support** — English & French chat messages
- **Debug mode** — verbose server console logging

---

## 📋 Requirements

| Dependency | Version | Link |
|-----------|---------|------|
| **CounterStrikeSharp** | ≥ 1.0.228 | [github.com/roflmuffin/CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp/releases) |
| **Metamod:Source** | latest dev | [sourcemm.net](https://www.sourcemm.net/downloads.php/?branch=master) |
| **.NET Runtime** | 8.0 | Bundled with CounterStrikeSharp |

---

## 🚀 Installation

### Step 1 — Install dependencies
Make sure **Metamod:Source** and **CounterStrikeSharp** are already installed on your server.

### Step 2 — Download the plugin
Go to the [**Releases**](https://github.com/NeuTroNBZh/CS2-RETAKE/releases/latest) page and download the latest `CS2Retake-v*.zip`.

### Step 3 — Upload to your server
Extract the archive and copy the **entire contents** into your server's `game/csgo/` directory:

```
game/csgo/
└── addons/
    └── counterstrikesharp/
        └── plugins/
            └── CS2Retake/
                ├── CS2Retake.dll
                ├── spawns/
                │   ├── de_dust2.json
                │   ├── de_mirage.json
                │   └── ... (all maps included)
                └── data/
                    └── CommandAllocator/  (auto-created by plugin)
```

### Step 4 — Start the server
Start or restart your CS2 server. The plugin will automatically generate its configuration files in:

```
addons/counterstrikesharp/configs/plugins/CS2Retake/
├── CS2RetakeConfig.json          ← Main plugin config
└── CommandAllocator/
    ├── CommandAllocatorConfig.json
    ├── FullBuyConfig.json
    ├── MidConfig.json
    └── PistolConfig.json
```

### Step 5 — Configure
Edit the generated JSON files to your preference (see [Configuration](#-configuration) below), then use `css_retake_reloadconfig` or restart the plugin to apply changes.

---

## ⚙️ Configuration

### Main Config — `CS2RetakeConfig.json`

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `PlantType` | enum | `AutoPlant` | `AutoPlant` or `FastPlant` |
| `RoundTypeMode` | enum | `Sequence` | `Sequence`, `Random`, or `Specific` |
| `RoundTypeSequence` | array | `[Pistol×3, Mid×3, FullBuy×∞]` | Sequence of round types with repetition counts (`-1` = infinite) |
| `RoundTypeSpecific` | enum | `FullBuy` | Fixed round type when `RoundTypeMode` is `Specific` |
| `Allocator` | enum | `Command` | Weapon allocator to use (currently `Command`) |
| `SecondsUntilBombPlantedCheck` | float | `5.0` | Seconds after round start before checking bomb plant status |
| `SpotAnnouncerEnabled` | bool | `false` | Show bombsite name in center text |
| `EnableQueue` | bool | `true` | Enable player queue system |
| `EnableScramble` | bool | `true` | Enable automatic team scramble |
| `EnableSwitchOnRoundWin` | bool | `true` | Rotate winning team out after round |
| `ScrambleAfterSubsequentTerroristRoundWins` | int | `5` | Consecutive T wins before scramble |
| `MaxPlayers` | int | `9` | Maximum simultaneous retake players |
| `TeamBalanceRatio` | float | `0.499` | Target ratio of T players (e.g. `0.45` = 45% T) |
| `EnableThankYouMessage` | bool | `false` | Display thank-you message to players |
| `MessageLanguage` | enum | `English` | Chat message language: `English` or `French` |
| `EnableDebug` | bool | `false` | Verbose console logging |

#### InstaDefuse Sub-Config

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `InstaDefuseEnabled` | bool | `true` | Enable the InstaDefuse feature |
| `InstaDefuseRequireNoTAlive` | bool | `true` | Require 0 living Terrorists to trigger instant defuse |
| `InstaDefuseBlockOnHe` | bool | `true` | Block instant defuse when a HE grenade was thrown near bomb |
| `InstaDefuseBlockOnMolotov` | bool | `true` | Block when a Molotov was thrown near bomb |
| `InstaDefuseBlockOnInferno` | bool | `true` | Block while an active Inferno is within range |
| `InstaDefuseInfernoDistance` | float | `250.0` | Max distance (units) from bomb for Inferno to block defuse |
| `InstaDefuseForceExplodeIfNoTime` | bool | `true` | Force explosion if remaining time < defuse duration |
| `InstaDefuseChatNotification` | bool | `true` | Send chat message when instant defuse is triggered or blocked |

---

### CommandAllocator Config — `CommandAllocatorConfig.json`

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `EnableRoundTypePistolMenu` | bool | `true` | Enable `!guns` menu for Pistol rounds |
| `EnableRoundTypeMidMenu` | bool | `true` | Enable `!guns` menu for Mid rounds |
| `EnableRoundTypeFullBuyMenu` | bool | `true` | Enable `!guns` menu for Full Buy rounds |
| `DefuseKitMode` | enum | `All` | Kit distribution: `All`, `Quota`, or `Chance` |
| `DefuseKitQuota` | int | `1` | Number of kits when mode is `Quota` |
| `DefuseKitChance` | double | `100.0` | Per-CT kit probability (%) when mode is `Chance` |
| `PistolDefuseKitChance` | double | `34.44` | Per-CT kit probability (%) on pistol rounds |
| `PistolDefuseKitGuaranteeMinimum` | bool | `true` | Guarantee at least 1 CT gets a kit on pistol rounds |
| `EnableZeus` | bool | `false` | Randomly distribute Zeus tasers |
| `ZeusChance` | int | `20` | Chance (%) each player receives a Zeus |
| `DatabaseType` | enum | `SQLite` | Weapon persistence backend: `SQLite` or `PostgreSql` |
| `ConnectionString` | string | *(template)* | PostgreSQL connection string (only used when `DatabaseType` is `PostgreSql`) |
| `HowToMessageDelayInMinutes` | float | `3.5` | Minutes between periodic `!guns` reminder messages |
| `HowToMessage` | string | `"Customize your weapons by using !guns"` | The reminder message text |

---

### Weapon Configs — `FullBuyConfig.json` / `MidConfig.json` / `PistolConfig.json`

Each file controls which weapons are available for selection in the corresponding round type. Weapons can be restricted by team (`CT`, `T`, or both).

**Full Buy — Primaries (CT):** M4A4, M4A1-S, FAMAS, AUG, MP9, SCAR-20, MAG-7  
**Full Buy — Primaries (T):** AK-47, Galil AR, SG553, Mac-10, G3SG1, Sawed-Off  
**Full Buy — Primaries (both):** MP7, MP5-SD, UMP-45, P90, PP-Bizon, SSG 08, Nova, XM1014, M249, Negev  
**Full Buy — Secondaries:** Deagle, P250, CZ75-Auto, Dual Berettas + team-specific pistols  
**Full Buy — AWP:** configurable chance per team (default 30%), max 1 per team, requires 5+ active players  

**Mid — Primaries (CT):** MP9, FAMAS, AUG  
**Mid — Primaries (T):** Mac-10, Galil AR, SG553  
**Mid — Primaries (both):** P90, MP5-SD, UMP-45, PP-Bizon, MP7  

**Pistol — Secondaries:** Deagle, P250, CZ75-Auto, Dual Berettas + team-specific pistols (no primary, no AWP)

---

## 🎮 Commands

### Player Commands
| Command | Description |
|---------|-------------|
| `!guns` | Open the weapon selection menu for your current team and round type |
| `!gun` | Alias for `!guns` |
| `!weapon` | Alias for `!guns` |
| `!weapons` | Alias for `!guns` |

### Admin Commands
| Command | Permission | Description |
|---------|-----------|-------------|
| `css_retake_reloadconfig` | `@css/root` | Reload the plugin configuration without restarting |

---

## 🗺️ Map Spawns

Spawn files are stored as JSON in the `spawns/` directory. The following maps ship with pre-configured spawns:

| Map | File |
|-----|------|
| de_dust2 | `spawns/de_dust2.json` |
| de_mirage | `spawns/de_mirage.json` |
| de_inferno | `spawns/de_inferno.json` |
| de_ancient | `spawns/de_ancient.json` |
| de_anubis | `spawns/de_anubis.json` |
| de_nuke | `spawns/de_nuke.json` |
| de_overpass | `spawns/de_overpass.json` |
| de_vertigo | `spawns/de_vertigo.json` |

To add custom spawns for another map, create `spawns/<mapname>.json` following the same format.

---

## 🗄️ Database

Weapon selections are persisted automatically. By default the plugin uses **SQLite** (no setup required — the database file is created at `plugins/CS2Retake/data/CommandAllocator/`).

To switch to **PostgreSQL**, set `DatabaseType` to `PostgreSql` and fill in the `ConnectionString` field in `CommandAllocatorConfig.json`:

```json
"DatabaseType": "PostgreSql",
"ConnectionString": "Server=localhost;Port=5432;Database=cs2retake;Userid=user;Password=pass"
```

> The plugin will fall back to in-memory cache if the database connection fails, so gameplay is never interrupted by a DB outage.

---

## 🔧 Troubleshooting

**Plugin does not load**
→ Verify CounterStrikeSharp ≥ 1.0.228 is installed and the `CS2Retake.dll` is in the correct `plugins/CS2Retake/` directory.

**Players spawn in wrong positions**
→ Check that the spawn JSON file for the current map exists in `spawns/`. If missing, players will use the game's default spawns.

**Weapon menu does not open**
→ Make sure `!guns` (and aliases like `!gun`, `!weapon`) are not blocked by another plugin and that the player has chat command access.

**AWP always unavailable**
→ AWP requires **strictly more than 4 active players** (T + CT combined). With ≤ 4 players the AWP option is locked regardless of the toggle.

**Weapon selection not persisted across maps**
→ Confirm the database file or PostgreSQL connection is accessible and `DatabaseType` matches your setup.

Enable `"EnableDebug": true` in the main config for verbose console output to diagnose any issue.

---

## 🏗️ Building from Source

```bash
# Clone the repository
git clone https://github.com/NeuTroNBZh/CS2-RETAKE.git
cd CS2-RETAKE

# Build (Release)
dotnet build CS2Retake/CS2Retake.csproj -c Release
```

Output artifacts are placed in `CS2Retake/bin/Release/net8.0/`.

The build script (`BuildScripts/Sync-PluginArtifacts.ps1`) automatically assembles the release package under `plugin/` on Windows.

---

## 🙏 Credits & Acknowledgements

| Project | Author | Role |
|---------|--------|------|
| [CS2Retake](https://github.com/LordFetznschaedl/CS2Retake) | [LordFetznschaedl](https://github.com/LordFetznschaedl) | **Base plugin** — core retake gameplay loop, spawn system, team management, allocator architecture |
| [cs2-instadefuse](https://github.com/B3none/cs2-instadefuse) | [B3none](https://github.com/B3none) | **InstaDefuse logic** — integrated natively as `InstaDefuseManager` |
| [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) | [roflmuffin](https://github.com/roflmuffin) | Plugin framework |
| [CSZoneNet.Plugin.CS2BaseAllocator](https://github.com/LordFetznschaedl/CS2BaseAllocator) | [LordFetznschaedl](https://github.com/LordFetznschaedl) | Allocator base library |

---

## 📄 License

This project is licensed under the **GNU General Public License v3.0** — see the [LICENSE](LICENSE) file for details.

Original base plugin © [LordFetznschaedl](https://github.com/LordFetznschaedl) — GPL-3.0  
InstaDefuse logic © [B3none](https://github.com/B3none) — MIT License (compatible with GPL-3.0)
