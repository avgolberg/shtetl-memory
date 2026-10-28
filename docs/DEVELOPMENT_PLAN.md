# Development Plan

**Goal:** finish ShtetlMemory quickly and reliably, preserving [STORY_SPEC.md](STORY_SPEC.md) and reusing existing systems.

## Roles
- **Claude:** primary developer. Assembles locations, places environment assets, configures NPCs, quests, transitions and UI, and writes code.
- **Owner:** reviews visuals, play-tests, supplies or generates missing art, makes story decisions.

## Decisions made
- **The 7 story locations, prologue and epilogue in STORY_SPEC are fixed scope.**
- **Keep the single-scene architecture** (`MainScene`). Each location is a root object with a `Location` component. Locations don't need separate scenes.
- **Hybrid editing:**
  - edit Unity YAML (scenes, prefabs, assets) directly when the change is simple and safe
  - use Editor scripts (`Assets/Editor/`) for repetitive assembly or for changes involving many references
  - always preserve GUIDs, fileIDs, references, prefab overrides and working content
- **No big generic toolkit.** Only add a tool when it clearly saves time, for example environment prefab extraction or a builder for repeated layout.
- **MCP is optional, not now.**
  - Its main benefit would be letting Claude trigger editor actions, read the console and grab screenshots without the owner's help.
  - Its costs: setup, third-party compatibility with Unity 6000.2, and its tool list using up context on every turn.
  - Reconsider if console or screenshot round trips become a bottleneck.
- **Visual check:** an Editor menu item renders a location to PNG for review when needed. The owner does the play-tests.

## Milestones (in order)

### M0: Safety and blockers
1. Commit the current work (Vytynanka, MainScene changes). Back up the gitignored paid asset folders.
2. Fix the build settings: replace the missing `ProtoScene` with `MainScene`.
3. Fix saving: store quest progress by `questID` and look the `Quest` up on load; save each NPC's dialogue stage.

### M1: Location assembly basics
4. Turn the hand-placed environment objects (houses, trees, fences, graves) into prefabs in `Assets/Prefabs/Environment/`.
5. A location template based on L2's structure: `Location` + camera bounds, Background/Parallax layers, transition pair, empty NPC/Items groups.
6. Generalize `InteractiveGrave` into an "inspect point" (info popup or cinematic + journal + quest notification) for stalls, windows and the square.
7. Proof: build **L7 Mikvah** from existing forest and foliage art, move the Fisherman there, and link Road → Mikvah → Cemetery.

### M2: Rework existing content to match the spec
8. Prologue: funeral, box objects, Yiddish caption.
9. L1 House: Halyna's dialogue per the spec, the key quest opening the chest of drawers, the 3 finds unlocking School and Synagogue, the house interior.
10. L5 Cemetery: clean up, entry transition, the "find Nathan ben Shlomo" quest gated by the donation list, a decision on the letter.
11. Repurpose or cut the `L1 Forest` content (pending decision D2).

### M3: New locations (story order is pending decision D1)
12. L2 Synagogue / House of Culture: Vytynanka connected to quest, reward and journal; the administrator.
13. L3 School: the professor and reizele.
14. L4 Market: 5 flashback stalls.
15. L6 Library: the librarian and the donation-book mini-quest.
16. Semen's dialogue chain, plus the educational journal topics.

### M4: Ending and delivery
17. Epilogue: a Feiga biography journal topic, the final monologue, the end screen and credits.
18. Main menu, localization, full build test.

## Dependencies
- The save fix (M0) must land before the longer quest chains (M2–M3).
- Environment prefabs and the template (M1) must exist before the new locations (M3).
- The art for interiors and flashbacks (owner) is needed before M2 item 9 and M3 can be finished. The layouts can be blocked out first with placeholders.

## Open decisions (owner)
- **D1** Story order of the locations, and where Semen appears. Inferred: House → Synagogue → School → Market → Library → Mikvah → Cemetery.
- **D2** Existing extra content: cut it or keep it as flavor? Covers Forest, Pavlo, Petro, Hisa and coins, Maria, the art-club kids.
- **D3** Feiga's letter: keep it at the stork grave, or move it into the epilogue diary?
- **D4** Shipping languages, and whether Yiddish text appears on screen.
- **D5** Interior and flashback art: purchased, generated, or built from the existing kits?
- **D6** Target platform, audience age, and the grant deadline.
