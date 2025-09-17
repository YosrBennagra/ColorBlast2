# 2025-09-16 — Rename to Shape Blaster

## Technical Notes

- Renamed code namespaces from `ColorBlast2.*` to `ShapeBlaster.*` across gameplay, UI, ads, and adventure systems.
- Updated asset type IDs in Adventure level ScriptableObjects to the new namespace so Unity can deserialize.
- Adjusted Unity project settings names:
  - `productName` already set to `Shape Blaster`.
  - `metroPackageName` → `ShapeBlaster`.
  - `metroApplicationDescription` → `Shape Blaster`.
  - `projectName` → `Shape Blaster`.
- Updated Editor menu paths and labels (Tools/Shape Blaster/…).
- Updated debug log strings mentioning the old name.

Key files modified (selection)

- Scripts: `Assets/_Project/Scripts/**` namespaces and references (`Adventure`, `Systems/UI/Core`, `Systems/Scoring`, `Systems/Ads`, `Gameplay`).
- Assets: `Assets/_Project/AdventureLevels/AdventureLevelLibrary.asset`, `Assets/Resources/Adventure/AdventureLevelLibrary.asset` (class identifier).
- Project: `ProjectSettings/ProjectSettings.asset` (names).

Notes

- Engine/core namespaces `ColorBlast.*` remain to avoid breaking framework code.
- Build caches and `.git` URLs were not changed.

## Player Notes

- The game is now titled “Shape Blaster”.
- All in-game menus and tools reflect the new name.

