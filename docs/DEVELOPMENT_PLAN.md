# Development Plan

**Goal:** finish ShtetlMemory quickly and reliably, preserving [STORY_SPEC.md](STORY_SPEC.md) and reusing existing systems.

## Roles
- **Claude:** primary developer. Assembles locations, places environment assets, configures NPCs, quests, transitions and UI, and writes code.
- **Owner:** reviews visuals, play-tests, supplies or generates missing art, makes story decisions.

## Decisions made
- **The 7 story locations, prologue and epilogue in STORY_SPEC are fixed scope.**
- **English is the primary game language (2026-10-28).** All current in-game dialogue, quests, journal entries and UI text are written in English. `STORY_SPEC.md` stays in Ukrainian as the authoritative story reference; translate from it faithfully. Other languages come later (localization in M4).
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

## Testing approach (agreed 2026-10-28)
- **Claude** runs static and automated checks where possible: diff review, GUID and reference checks, and later an Editor validation script.
- **Owner** tests only the functionality a change affects, using a short checklist that Claude provides.
- **Full playthrough and regression** happen only at milestone ends and for release preparation.
- Every reported issue is labeled **runtime-confirmed** (seen in Unity) or **code-inspection** (potential).

## Milestones (in order)

### M0: Safety and blockers
1. ✅ Full backup (2026-10-28): `C:\Users\Alyona\Backups\ShtetlMemory_2026-10-28`, on a different physical disk. Excludes Library and Temp. Verified: all files SHA-256-identical except one locked shader log; `git fsck` OK.
   ✅ Committed `b8dc7d7` (Vytynanka WIP, MainScene, docs). The paid folders `Cutscene` and `EasyChara Paid` are untracked by design. 280 `Village Assets` files were committed before the ignore rule and are still tracked.
2. ✅ Build settings now list `MainScene` (GUID `84235362192f67a469199eeee2de4987`) instead of the missing `ProtoScene`.
3. ✅ Save fixes committed (2026-10-28). The project compiles and runs in Unity; the quest-restore-after-restart path will be checked in the next milestone's regression pass:
   - give `QuestProgress` a serialized `questID`, and on load re-resolve `quest` from the quests the NPC dialogues reference
   - first-run fallback for `currentLocation`
   - null guard when the saved location is missing
   - keep the existing save format, adding fields only

### M1: Location assembly basics
0. ✅ Location capture tool (`Assets/Editor/ShtetlTools/LocationCapture.cs`), validated in batch mode on L1–L3 (2026-10-28). Decision: no broad environment-prefab refactor; make prefabs only where a specific new location needs them.
4. Turn the hand-placed environment objects (houses, trees, fences, graves) into prefabs in `Assets/Prefabs/Environment/`.
5. A location template based on L2's structure: `Location` + camera bounds, Background/Parallax layers, transition pair, empty NPC/Items groups.
6. Generalize `InteractiveGrave` into an "inspect point" (info popup or cinematic + journal + quest notification) for stalls, windows and the square.
7. ✅ **L7 Mikvah** visuals (approved and polished, 2026-10-28). It lives **in L3's left edge**, on the way to the graves, not as a separate location. Built by `MikvahAreaBuilder`. Pattern for later areas: one small builder script per area that can be re-run, verified with LocationCapture in batch mode. Remaining: an L3 entrance (next task, L2 → L3).

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
- **D4** ~~Primary language~~ settled: English. Still open: which languages come later, and whether Yiddish text (photo caption, donation list) appears on screen.
- **D5** Interior and flashback art: purchased, generated, or built from the existing kits?
- **D6** Target platform, audience age, and the grant deadline.
