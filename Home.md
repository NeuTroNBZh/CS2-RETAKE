# CS2-RETAKE V3 - Public Server Setup Guide

This page provides a short step-by-step deployment path.

## 1. Requirements

- CS2 dedicated server
- CounterStrikeSharp installed
- Access to server files

## 2. Download

Use the latest release:
- https://github.com/NeuTroNBZh/CS2-RETAKE/releases

For first install, prefer the full package with config templates.

## 3. Install

Extract release archive and copy the `addons` folder into your server `csgo` directory.

## 4. Configure

- Base config:
  - addons/counterstrikesharp/configs/plugins/CS2Retake/CS2Retake.json
- Allocator config:
  - addons/counterstrikesharp/configs/plugins/CS2Retake/CommandAllocator/CommandAllocator.json

## 5. Validate

- `!retakeinfo`
- `!guns`
- pistol rounds: no helmet
- native buy selection persistence
- AWP and InstaDefuse rules as configured

## 6. Upstream Credits

- https://github.com/LordFetznschaedl/CS2Retake
- https://github.com/B3none/cs2-instadefuse

## More Documentation

- https://github.com/NeuTroNBZh/CS2-RETAKE/blob/main/INSTALLATION.md
- https://github.com/NeuTroNBZh/CS2-RETAKE/blob/main/UPSTREAM_DIFFERENCES.md
