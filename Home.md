# CS2-RETAKE V3 - Public Server Setup Guide

Welcome to the public setup guide for CS2-RETAKE V3.

## Step 1 - Prerequisites

- Counter-Strike 2 dedicated server
- CounterStrikeSharp installed and working
- Access to server files

## Step 2 - Download Release

Download from:
- https://github.com/NeuTroNBZh/CS2-RETAKE/releases

Recommended for first install:
- CS2-RETAKE-3.0.0.zip

## Step 3 - Install Files

Extract the `addons` folder into your server `csgo` directory.

## Step 4 - Start Server

Restart your server and confirm plugin loading.

## Step 5 - Configure Plugin

Base config path:
- addons/counterstrikesharp/configs/plugins/CS2Retake/CS2Retake.json

Allocator config path:
- addons/counterstrikesharp/configs/plugins/CS2Retake/CommandAllocator/CommandAllocator.json

## Step 6 - Validate Core Features

- `!retakeinfo` works
- `!guns` opens menus
- pistol rounds do not grant helmets
- native buy selection persists as expected
- AWP and InstaDefuse follow configuration

## Attribution

Based on work from:
- https://github.com/LordFetznschaedl/CS2Retake
- https://github.com/B3none/cs2-instadefuse

## Full Documentation

Repository documentation:
- https://github.com/NeuTroNBZh/CS2-RETAKE/blob/main/INSTALLATION.md
- https://github.com/NeuTroNBZh/CS2-RETAKE/blob/main/UPSTREAM_DIFFERENCES.md
