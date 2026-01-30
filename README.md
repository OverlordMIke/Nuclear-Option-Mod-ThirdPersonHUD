# Orbit HUD for Nuclear Option

**A client-side mod that improves the Orbit camera experience in Nuclear Option.**

![Game Banner or Screenshot](preview_screenshot.png)  

## About

This mod fixes a common annoyance in **Orbit camera mode** (external view):
- The in-cockpit **HUD** is now properly visible while flying in Orbit camera.
- The **horizon line** and **pitch increment lines** are automatically hidden to reduce screen clutter and improve visibility.

Once installed, the mod is **enabled by default**. You can toggle or fine-tune it anytime via the auto-generated config file.

## Features

- **HUD visibility in Orbit camera** — See your full situational awareness HUD (velocity vector, AoA, G-load, radar contacts, etc.) even when using external Orbit view.
- **Reduced clutter** — Horizon line + pitch ladder automatically disabled in Orbit mode for a cleaner screen.

## Requirements

- [Nuclear Option](https://store.steampowered.com/app/2168680/Nuclear_Option/) (Steam)
- [BepInEx](https://github.com/BepInEx/BepInEx/releases) (latest pack for Unity Mono games — usually drop the BepInEx folder into your game directory)

## Installation

1. Install **BepInEx** if you haven't already:
   - Download the appropriate pack from the [BepInEx GitHub releases](https://github.com/BepInEx/BepInEx/releases).
   - Extract it so `BepInEx` folder is directly inside your Nuclear Option install directory (e.g. `C:\Program Files (x86)\Steam\steamapps\common\Nuclear Option\`).
   - Launch the game once — BepInEx will generate its folders.

2. Download the latest release of this mod from the [Releases page](https://github.com/YourGitHubUsername/YourModName/releases).

3. Extract the contents of the zip file into the `BepInEx/plugins` folder.

4. **Important for Nuclear Option**:
   - Open the BepInEx configuration file located at `Nuclear Option/BepInEx/config/BepInEx.cfg`  
   - Find the `[Chainloader]` section (or add it if missing) and make sure this line is present and set correctly:

      `HideManagerGameObject = true`

   - Save the file.
   
*Why is this needed?* Some Unity games (including Nuclear Option) perform aggressive cleanup of certain scene objects or `DontDestroyOnLoad` GameObjects during scene changes, camera mode switches, or other events. This can destroy BepInEx's internal manager object too early, breaking plugin loading/execution. Setting `HideManagerGameObject = true` hides the BepInEx manager from normal Unity hierarchy inspection/cleanup, preventing it from being destroyed prematurely and ensuring mods like this one function correctly.

## Disclaimer

> **Client-side mod.** Tested exclusively in **single-player** and on **private friend servers**.  
> 
> I am **not responsible** for kicks, bans, or any penalties on public servers.  
> **Always check each server's rules** before playing. Use online **at your own risk**.
