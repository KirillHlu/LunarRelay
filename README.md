# Lunar Relay

**Build a Junior Astronaut Mission Trainer — NASA Space Apps Challenge project.**

A Unity first-person educational game about keeping a lunar outpost alive: restore air cleaning, balance electrical power and prepare a crew shelter. Includes hands-on equipment, inventory, finite shifts and endless mode, task credits, a decoration shop, English/Russian localization and Android touch controls.

## Demo

[Open the seven-slide presentation](Presentation/Lunar_Relay_Ready.pdf)

[Download the game for Android or Windows](https://lunarrelay1.pythonanywhere.com/)

## Open the Unity project

1. Install Unity **6000.3.25f1**, including Android Build Support to build the APK.
2. Open this repository in Unity Hub. Allow the packages in `Packages/manifest.json` to resolve.
3. Open `Assets/Scenes/LunarRelay.unity` and enter Play mode.

The scene creates the outpost at runtime. Model data under `Assets/Resources/Models` and its importer are included; local Downloads paths are not needed at runtime.

Build entry points: `LunarRelay.Editor.BuildTools.WindowsCreditReadability` and `LunarRelay.Editor.BuildTools.AndroidPhone`. These build methods change target-specific project settings; keep changes isolated when switching platforms.

## Controls

Windows: WASD/mouse; E interact; I inventory; Tab base tablet and shop; Esc pause; Ctrl/C crouch; Space jump; R store; Q release; F lamp.

Android: left joystick; right-side drag to look; two camera fingers pinch to adjust FOV; circular action, inventory, tablet and pause buttons. Decoration mode has place, rotate and cancel actions. On-device Android validation remains incomplete; see `Docs/AndroidControls.md`.

## Source layout

- `Assets/LunarRelay` — gameplay, simulation, UI, mobile controls and runtime checks.
- `Assets/Editor` — importers, builds and numerical validation.
- `GeneratedModels`, `Tools` — generated model assets and authoring scripts.
- `Website` — English Flask download website, without packaged game binaries.
- `Presentation` — project demonstration PDF, seven slides.
- `Docs` — implementation notes and controls.

## Verification

The latest automated desktop regression passed229 assertions, including193 existing gameplay checks and36 synthetic touch/control checks. Seven native GUI touch checks were skipped in hidden/batch mode. Numerical validation previously passed213 checks. These are automated checks, not a complete physical-device playthrough.

Run a test Windows build with `--smoke` to produce runtime reports. `--smoke --mobile-only` runs focused synthetic touch checks. Numerical validation: `LunarRelay.Editor.EngineeringValidation.Run`.

## Science and attribution

CO2 removal, oxygen generation, water recovery and power allocation are distinct linked systems. Time, equipment ratings and scenarios are adapted for education. NASA references and scientific assumptions are documented in the game and presentation; this is an independent project, not an official NASA product.

PT Sans font license: `Assets/Resources/Fonts/OFL.txt`. Other asset and model provenance is described in the source and project documentation.
