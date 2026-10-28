# Development Plan

**Goal:** finish ShtetlMemory quickly and reliably, preserving [STORY_SPEC.md](STORY_SPEC.md) and reusing existing systems.

## Roles
- **Claude:** primary developer. Assembles locations, places environment assets, configures NPCs, quests, transitions and UI, and writes code.
- **Owner:** reviews visuals, play-tests, supplies or generates missing art, makes story decisions.

## Decisions made
- **The 7 story locations, prologue and epilogue in STORY_SPEC are fixed scope.**
- **English is the primary game language (2026-10-28).** All current in-game dialogue, quests, journal entries and UI text are written in English. `STORY_SPEC.md` stays in Ukrainian as the authoritative story reference; translate from it faithfully. Other languages come later (localization in M4).
- **Accessible English (2026-10-28):** the audience includes many non-native speakers. Write dialogue, quests, journal entries and UI in clear, natural, simple English: common words, short sentences, no idioms. Keep the historical and educational meaning of STORY_SPEC.
- **Transition visuals:** the pine-tree waypoints are development markers only (blended into each location's palette and sorting). The final transition design is still to be decided; the village art has only shop signs and a `fenceGate`.
- **Scene workflows (2026-10-28):** with Unity closed, Claude runs the builders in batch mode. With Unity open, the owner runs the **Shtetl → …** menu items and saves with Ctrl+S; the builders capture automatically, so Claude can check the result without a restart.
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
4. ⏸ Deferred by decision: environment prefabs only when a specific location needs them (no broad refactor).
5. ⏸ Deferred: a location template, built when the first new outdoor location needs one.
6. Generalize `InteractiveGrave` into an "inspect point" (info popup or cinematic + journal + quest notification) for stalls, windows and the square.
7. ✅ **L7 Mikvah** visuals (approved and polished, 2026-10-28). It lives **in L3's left edge**, on the way to the graves, not as a separate location. Built by `MikvahAreaBuilder`. Pattern for later areas: one small builder script per area that can be re-run, verified with LocationCapture in batch mode. 
8. ✅ **L2 ↔ L3 route** (2026-10-28; runtime-confirmed by the owner: transitions work, the Fisherman's dialogue shows, no walking on water). Built by `Assets/Editor/ShtetlTools/LocationLinkBuilder.cs`:
   - `L2->L3 Waypoint` appears at the street's right end (x≈185); the player arrives in L3 at x -160, on the bank right of the Fisherman.
   - `L3->L2 Return Waypoint` appears at x≈-146, past the fence; the player arrives in L2 at x 179.5.
   - **Development-stage decision: always open** (`isReturnTransition`). Replace with quest-based unlocking once the story order (D1) is implemented. Don't tie it to Halyna's current quest.
   - Arrival points are non-parallaxed objects, so arrival doesn't depend on the camera at departure. The existing L1↔L2 pair still uses parallaxed TeleportPoint children (unchanged).
   - Waypoint pines are tinted and layered per location (dev markers).

## Next task (start here on 2026-10-29): M2 item 9, Location 1 "Дім прабабусі" (great-grandmother's house)
Source: STORY_SPEC "Локація №1". The existing content lives in L2 Street (`NPC Hisa Teacher` prefab = Halyna; dialogues `Halyna NPCDialogue Part0–2`; quests `FindHouse`, `FindKeys`, `CollectFlowers`; Key Choice mini-game).
1. **Mini-quest 1** (find the house from the photo): already works (`FindHouse`, ReachLocation `grandma_house`). Keep it.
2. **Halyna (70+)**: rewrite her dialogue per the spec, in accessible English:
   - a Jewish family lived here; they were killed in the war
   - she doesn't know their surname
   - her grandmother said they shopped at the market every Friday
   - after the war the authorities let her large family move in (her father worked at a factory)
   - only an old wardrobe was left; she has a key but never opened it
   Remove what contradicts the spec: the flower quest, her husband's jewelry box, the mezuzah mark on her door. Decide whether to delete or just unlink the `CollectFlowers` asset.
3. **Mini-quest 2**: a bundle of keys → pick the right key → open the chest of drawers. Reuse the Key Choice mini-game; re-theme the `FindKeys` quest.
4. **Finds**:
   - family photo in the hallway (an unpainted square is visible on the wall)
   - school photo, which leads to the School
   - wedding invitation, which leads to the Synagogue
   Use items plus journal entries. Their unlock effects depend on D1, so build the items now and wire the unlocks after D1.
5. **House interior**: there's no interior art yet (D5). Block it out with existing art or placeholders and list the exact art needed.
Workflow: inspect the relevant assets and the scene section first, then build with a small builder script and check it with captures. Give the owner a short targeted test, using a dev save preset at Halyna's house.

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
