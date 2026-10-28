# Project Overview

**ShtetlMemory** is a grant-funded 2D educational adventure game about Jewish shtetl life in Ukraine. The player (Yasha or Sonya) traces the hidden past of their great-grandmother, Feiga Shapiro, through 7 locations in the town of Ladanivka. The authoritative story is [STORY_SPEC.md](STORY_SPEC.md).

## Tech
- Unity **6000.2.13f1**, 2D, URP 17.2. Cinemachine 3.1.4, Input System 1.14, Tilemap/SpriteShape, TextMesh Pro, Timeline, Hot Reload.
- Git: `main` branch, with an origin remote; 41 commits (Jan 31 – May 30, 2026).
- Plain `git` fails on drive D because of an ownership check. Use `git -c safe.directory=* …` and don't change the global config.
- **Gitignored paid/large assets** (not in version control, so back them up separately): `Assets/Cutscene`, `Assets/Sprites/Village Assets`, `Assets/Sprites/Characters Assets/EasyChara Paid`. `Assets/_Recovery/` (26 auto-recovery scene copies) is ignored too.

## Architecture
- **Single scene:** `Assets/Scenes/MainScene.unity` (about 3 MB, 939 GameObjects). Nothing in the code calls `SceneManager`.
- Locations are root objects (`L1`, `L2`, `L3`) placed side by side in the scene. Each has a `Location` component with an ID, a camera-bounds polygon (`mapBoundary`) and music. Each also has `Background` children (ground, cloud and background layers) and `Parallax` children (houses, trees, items, NPCs).
- **Travel:** `MapTransition` (an interactable waypoint) fades the screen, teleports the player to `teleportTargetPosition`, swaps the Cinemachine confiner (camera bounds), changes music, and records the current location for saving. A transition only works when `isReturnTransition` is set or after `ChangeSprite()`, which a Portal quest reward calls.
- **Managers:** singletons on the `GameController` object:
  - Dialogue, Inventory, Quest, Journal, Rewards, SoundEffectManager, Cinematic
  - lookup tables: ItemDictionary, TransitionDictionary, MiniGameItemDictionary
  - Save (`SaveController`), ItemPickupUI, GraveInfoUI and the Vytynanka game are singletons too
- **Data (ScriptableObjects)** in `Assets/Scripts/Scriptable Objects/`:
  - NPC Dialogues (10)
  - Quests (5)
  - Journal topics (2)
  - Cinematic Text (2)
- **UI** sits under the scene's `UI` root: dialogue, menu (tabs), item popup, cinematic, character selection, fade, the mini-game canvases, and the grave info popup.

## Key systems (scripts are in `Assets/Scripts/`)
| System | Files |
|---|---|
| Player movement, interaction | `Player/PlayerMovement.cs`, `Global/InteractionDetector.cs`, `Global/IInteractable.cs` |
| Character choice (boy/girl) | `Controllers/Cinematic/CharacterSelectionUI.cs`, `Player/PlayerVisualSelector.cs` |
| Inventory, items, chests | `Controllers/InventoryController.cs`, `Intreactable Items/Item.cs`, `Chest.cs`, `ItemDragHandler.cs` |
| NPC dialogue (stages, choices, voice, journal triggers) | `NPC/NPC.cs`, `NPC/NPCDialogue.cs`, `Controllers/DialogueController.cs` |
| NPC walking, offscreen freeze | `NPC/WaypointMover.cs`, `NPC/FreezeWhenOffscreen.cs` |
| Quests (CollectItem, ReachLocation), rewards (Item, Portal, MiniGame) | `Controllers/Quests/*` |
| Journal (topics → stages) | `Controllers/Journal/*` |
| Cinematic text sequences | `Controllers/Cinematic/CinematicTextController.cs`, `CinematicSequenceSO.cs` |
| Mini-games: Hidden Object, Key Choice, Vytynanka | `Intreactable Items/MiniGame/*` |
| Matzevot (gravestones), letter | `Intreactable Items/InteractiveGrave.cs`, `GraveInfoUI.cs` |
| Locations, travel | `Controllers/Transitions/*` |
| Save (JSON file in `persistentDataPath`) | `Save/SaveController.cs`, `Save/SaveData.cs` |
| Sound, music per location | `Controllers/Sounds/*` |

## Existing art
- **Environment:**
  - `Sprites/Village Assets/` (2D village pack, platformer buildings with tile assets)
  - `Sprites/Background/` (background-elements-redux, foliage-pack)
- **Characters:** EasyChara (free and paid), Boy, Girl.
- **Items:** GenericItems, 17 grave sprites, Vytynanka.
- **UI:** ui-pack-adventure.
- **Prefabs** (`Assets/Prefabs/`, 23 total):
  - items, 2 NPCs, `Waypoint`, mini-game pieces, UI
  - **no environment prefabs:** houses, trees, fences and graves are hand-placed objects in the scene
- **No interior art** was found.

## Current content in the scene
- **L1 Forest:** NPCs Petro, Pavlo (wallet hunt in the Hidden Object game) and Hisa (prototype coin quest). Transition L1→L2.
- **L2 Street:** 25 houses. NPCs Halyna (3 dialogue parts), Maria (borscht) and 2 kids. 18 collectables, a chest. Transition L2→L1.
- **L3 Cemetery:** 50 graves, 9 of them interactive with symbols (one is a stork grave holding Feiga's letter). The Fisherman stands here. **No transition leads into L3.** Placeholder `temp` objects remain.
