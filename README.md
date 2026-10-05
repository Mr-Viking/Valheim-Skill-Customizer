# Valheim-Skill-Customizer

A lightweight, completely dynamic progression utility that lets you customize your experience gain rates and death penalties on the fly using standard configuration sliders.

## 🛠️ Features
* **Master Toggle**: Instantly enable or disable all mod features on the fly without needing to restart your game client.
* **0.5 Snapping Multipliers**: Sliders automatically round and snap to clean `0.5` increments, ranging from `0.0x` (locked progression) to `10.0x` (ultra fast).
* **Global & Dynamic Settings**: Set a baseline modifier globally, or adjust every vanilla skill (Running, Jumping, Woodcutting, Axes, etc.) individually.
* **Custom Death Penalties**: Choose between four distinct settings via an easy dropdown interface:
  * **No Skill Drain** (Lose 0% on death)
  * **Half Skill Drain** (Lose 2.5% on death)
  * **Normal Skill Drain** (Vanilla default 5% loss)
  * **Double Skill Drain** (Lose 10% on death)
* **Uses ServerSync to lock settings and sync settings to all clients**
  
## ⚙️ Requirements
* **BepInEx pack for Valheim**
* **Official BepInEx Configuration Manager** (Highly recommended to gain access to the interactive UI sliders inside the game via the `F1` or `Pause` hotkey).

## 🚀 Installation
1. Move the `ValheimSkillCustomizer.dll` file directly into your game's `Valheim/BepInEx/plugins/` folder.
2. Launch the game and access the settings panel via your configuration manager hotkey.
