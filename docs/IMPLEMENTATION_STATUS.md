# Implementation Status

Mapped against [STORY_SPEC.md](STORY_SPEC.md). Last audit: 2026-10-28 (read-only; Unity was not run).

**Labels:** **[C]** confirmed in files · **[I]** inferred · **[T]** needs testing in Unity.
Any status other than Missing still needs a play-test.

## Session summary (2026-10-28)
**Completed and committed locally on `main`** (push blocked by a GitHub 403 for the stored user `alyona336`; see CLAUDE.md):
- **Safety:** full backup at `C:\Users\Alyona\Backups\ShtetlMemory_2026-10-28`; the Vytynanka WIP and docs committed.
- **M0 fixes:** build list → `MainScene`; saves store quests by `questID`; first-launch save and inventory init; fallback for a missing location.
- **`LocationCapture` tool:** overview and player-view tiles with parallax simulated; summary with layers, NPCs, issues and transition checks.
- **L7 Mikvah** in L3's left edge: natural pond, bank, shore rocks and reeds, the `Bank Stop` wall, the Fisherman's full line in accessible English.
- **Always-open L2↔L3 route** with non-parallaxed arrival points; pine markers blended per location.
- **Dev test saves** (`DevSaves/`). The installed save is currently `l2_street_end_post_intro`; your original save is backed up beside it as `saveData.backup-20261028-211459.json`.
- **Workflow and decisions** in CLAUDE.md and DEVELOPMENT_PLAN: English, accessible English, two scene workflows, visual integration rule.

**Runtime-confirmed by the owner:** the project runs; the first-launch save is written; L2↔L3 transitions work; the Fisherman's dialogue shows; the player can't walk on the water.

**Remaining targeted tests (no full regression needed):**
- the Bank Stop position: walk left past the Fisherman; you should stop on the bank top and still be able to talk to him
- the Fisherman's new 8-line dialogue
- quest restore after a full Unity restart (do it in the M2 regression pass)

**Open known issues:** see "Known bugs and risks" and "Remaining save and load risks" below. Main ones:
- a loaded Boy probably shows Girl visuals (code-inspection)
- a corrupt save file isn't handled
- the L1↔L2 arrival points move with parallax (old behaviour)
- pine waypoint markers are temporary
- the product name is still "Messing Around" (it's the save folder name; don't change it without moving the dev saves)

## Requirements coverage

| Requirement | Status | Current implementation / gap |
|---|---|---|
| **Prologue 1:** funeral; the habits (fasting, matzah, milk and meat apart, meatless borscht) | Partial | `Cinematic Sequence.asset` covers the habits [C]. No funeral setting. The borscht detail sits in Maria's dialogue |
| **Prologue 2:** box with photo (Ukrainian + Yiddish caption), candlestick, siddur, headscarf, letters, dreidel | Partial | The intro mentions the box and the photo "My home. Ladanivka." [C]. Missing: the Yiddish caption and the other objects |
| Hero is Yasha or Sonya | Partial | Boy/girl selection exists (`CharacterSelectionUI.cs`) [C]. The names are not confirmed [T] |
| Feiga's biography (1922, hiding, hospital, 1947 marriage, Inna) | Missing | — |
| **L1 House:** mini-quest 1, find the house from the photo | Implemented [T] | L2 Street, `FindHouse.asset` (ReachLocation) [C] |
| L1: Halyna's dialogue | Partial | `Halyna NPCDialogue Part0–2` on the `NPC Hisa Teacher` prefab. The content differs from the spec |
| L1: mini-quest 2, bundle of keys → open the chest of drawers | Partial | Key Choice mini-game + `FindKeys.asset`. The story context is a jewelry box |
| L1: finds (family photo with the unpainted square, school photo → School, invitation → Synagogue) | Missing | Could use Portal rewards |
| L1: house interior | Missing | — |
| **L2 Synagogue / House of Culture:** windows, mezuzah recesses | Missing | A Mezuzah journal topic exists, but it's tied to Halyna's door |
| L2: mini-quest 6, Vytynanka | Partial | Gameplay exists but is **uncommitted** [C]. On completion it only closes; no quest or reward hook [I] |
| L2: administrator dialogue | Missing | — |
| **Semen (Shimon)**: education, house of help, unpainted square, mikvah, donation list | Missing | The spec doesn't say where he appears (cemetery? [I]) |
| **L3 School (art college):** professor, reizele | Missing | — |
| **L4 Market:** mini-quest 4, 5 flashback stalls | Missing | Maria's dialogue already points to the market |
| **L5 Cemetery:** broken matzevot, stones | Partial | Built in the scene's `L3` root; `temp` placeholders; no entry transition |
| L5: mini-quest 3, symbols + Nathan's grave (stork) | Partial | 9 interactive graves (lion, deer, bird, candles, pitcher, bear, squirrel, stork) [C]. Nathan's name is missing; no "find the grave" quest; not gated by the donation list |
| **L6 House of help (library):** librarian, mini-quest 5 (donation book) | Missing | — |
| **L7 Mikvah** (pond on the way to the cemetery) | Partial: visuals and route done (2026-10-28) | Inside L3's left edge: natural pond with a sloped bank, waterline rocks, reeds and grass (no steps). An invisible `Bank Stop` wall (in the Mikvah group, parallaxed like the ground colliders) keeps the player on the bank top beside the Fisherman. Runtime-confirmed: L2↔L3 route works, Fisherman dialogue shows, the player can't walk on water. Still to check in Unity: the bank stop position. The Fisherman's lines are rewritten in accessible English (8 short lines). Built by `MikvahAreaBuilder` |
| **Epilogue:** diary of the biography, final monologue | Missing | The journal system and cinematic text can host it |
| Educational topics | Partial | Done: Kashrut and Mezuzah journal topics; matzevot symbols. Missing: Yom Kippur fast, Pesach, Shabbat/challah, Yiddish/schooling, synagogue/demographics, tzedakah/dowry, the unpainted square, mikvah, reizele, klezmer, "X ben Y" naming, the Holocaust/hiding |
| Main menu, ending, credits | Missing | Only tabs, pause and a Save button exist |

## Content that contradicts the spec
1. **The `L1 Forest` root isn't in the spec.** It holds Pavlo's wallet quest, Petro, Hisa's coin/portal prototype quest and the root-level coins.
2. **Halyna:**
   - the game says the family "never came back"; the spec says they were killed
   - the game has a flower quest and her husband's jewelry box; the spec has an old wardrobe or chest of drawers and a key she never used
   - the game gives the mezuzah mark; the spec's finds are the photo, school photo and invitation
3. **Mezuzah:** Pavlo and Halyna hint at it on house doorways; the spec puts the mezuzah recesses in the synagogue.
4. **`Cemetery Letter.asset`** (Feiga's letter, stork grave) isn't in the spec. It skips the chain: donation list → Nathan → stork. It also says Petro's family hid her; the spec says a basement, 3 years.
5. **The Fisherman** is in the cemetery; the spec puts the fishermen at the Mikvah.
6. **Maria's borscht dialogue** is extra content, not in the spec.
7. **Language:** in-game text is English; the spec is Ukrainian with Yiddish. Yiddish needs a font with Hebrew-script glyphs and right-to-left support in TextMesh Pro [I/T].
8. **Data glitch:** `CollectFlowers.asset` says "15 flowers", but its first objective is 5 red flowers.

## Known bugs and risks
- ✅ **Fixed 2026-10-28:** the build settings listed only the missing `ProtoScene`. `EditorBuildSettings.asset` now lists `MainScene` [C].
- ✅ **Save fixes (2026-10-28)**. Runtime-confirmed: the project compiles and runs with no regressions. Not yet runtime-tested: restoring a quest after restarting Unity (do it in the M1 regression pass). They fix the three code-inspection bugs below:
  - `QuestProgress.questID` is saved, and `QuestController.GetQuestByID` re-resolves quests on load (first the optional `questDatabase` list, then any `Quest` asset loaded with the scene). Unresolvable entries are skipped with a warning.
  - First launch picks a fallback location (the boundary containing the player, otherwise the lowest ID), initializes the inventory, then saves.
  - A missing saved location falls back the same way.
  - The save format only gained the `questID` field. Old saves still load: quest entries without an ID use their old reference and are dropped if it doesn't resolve.
- **First-run save crashes** (investigated 2026-10-28): with no save file, `SaveController.Awake → LoadGame → SaveGame()` dereferences `currentLocation`, which is still null (`SaveController.cs:38`). So no file is written, and the empty-inventory setup on line 85 is skipped [C in code]. Evidence: the data folder `%USERPROFILE%/AppData/LocalLow/DefaultCompany/Messing Around/` exists but holds no `saveData.json` [C]. The console error still needs confirming [T].
- **Quests are lost after a restart:** `QuestProgress.quest` (a ScriptableObject) is serialized by `JsonUtility` as a temporary in-memory ID. It resolves within one editor session but not after restarting Unity or in a build. Then `QuestController.Find(q => q.QuestID …)` throws on the null `quest` [C in code, T to observe].
- **Missing location on load:** `LoadGame` assumes `currentLocationID` matches a `Location`; there's no null guard (`SaveController.cs:61–69`) [C].
- **NPC dialogue stage is OK:** it's rebuilt on load from the saved `handinQuestIDs` (`NPC.ResolveCurrentDialogueData`) [C]. Not saved: `hasFinishedDialogueOnce`, so repeat lines restart (minor).
- Execution order (from the `.meta` files): ItemDictionary/TransitionDictionary 300, Inventory 400, Quest 500, ScreenFader 550, SaveController 600. So load runs after the managers' Awake [C].
- **Saving happens only** on first run, on the intro, on character selection, and from the menu Save button. No autosave [C].
- The Player Settings product name is still **"Messing Around"** (company "DefaultCompany"). Changing it moves the save folder.
- **Unsaved-work risk:** uncommitted changes in `MainScene.unity` and the untracked Vytynanka folder (plus its prefab and SFX).
- **Paid assets aren't in Git.**
- `ObjectiveType.TalkNPC` and `Custom` are declared but appear unhandled [I].

## Location capture findings (2026-10-28, from `Snapshots/`, runtime-rendered)
- The locations are **stacked vertically** in `MainScene`, each about 384 × 62 units: L1 at y≈-38..24, L2 at y≈-113..-51, L3 at y≈-186..-123 (L3 is 450 wide). The player camera view is 62 × 35 units, so there are 7–8 tiles per location.
- Sky and ground backgrounds are looping parallax layers placed near x≈0. They look correct in the player-view tiles; in the overview they appear as a central band only (expected).
- **L1 Forest:** the 8 prototype coins (`Collectable Items` root) are visible in L1. NPCs Hisa (x-13), Petro (x32), Pavlo (x94). The only transition is L1→L2 at x53.
- **L2 Street:** houses span the full width; it's the best-dressed location. NPCs Maria, Moly, Helio, Halyna (Hisa Teacher). The player's edit-mode start position is in L2. The only transition is L2→L1 at x-27.
- **L3 Cemetery:** its left-edge water block with a hard vertical edge was rebuilt as the Mikvah pond (see the coverage table). **Since 2026-10-28 it's reachable from L2** (always-open development route, see DEVELOPMENT_PLAN M1 item 8). The capture's automated checks pass: the arrivals are inside the target bounds and clear of all triggers. Not yet runtime-confirmed.
- **Waypoints are parallaxed** (`ParallaxController` 0.1 on each MapTransition object, sprite `treePine`; locked ones show `treePineSnow`). The capture report flags arrival points that move with parallax.
- Parallax factors: the `L3/Parallax` gameplay group uses 0.1 (no looping); the ground layers use 0.1 (looping); groundLayer2 0.5; the clouds 0.3 and 0.6. At L3's left camera limit, gameplay objects appear 16 units left of their edit-mode x.
- Objects named `temp` under L2 and L3 `Background/groundLayer0/1` use the `temp.jpg` ground sprite. **They're probably real ground pieces, not placeholders** (code-inspection; they render fine). `L3/Parallax/temp (1)`/`(2)` still need checking.
- No missing scripts, no empty sprites, and no renderers outside their boundary in L1–L3.

## Intro skip (checked 2026-10-28; already exists, no change needed)
- The intro plays once. After the memory-box mini-game, `CinematicSequenceEvents` calls `SaveController.MarkIntroSeen()`. On later launches, `CinematicTextController.Start` skips the intro when `HasSeenIntro` is true.
- During playback, click, Space or Enter fast-forwards the typing and advances the slides. There's no "skip the whole intro" button.
- Quitting before the box mini-game replays the intro. The box mini-game also gives the first quest (`CinematicSequenceEvents.quest`).

## Remaining save and load risks
- **Runtime-confirmed (2026-10-28):** the first-launch save is now written (`saveData.json` created at 20:22 with valid content).
- **Code-inspection: a loaded Boy may show Girl visuals.** `PlayerVisualSelector.Awake` (default execution order) applies `PlayerCharacterState` before `SaveController` (order 600) loads `selectedCharacter`, and nothing re-applies it after loading. A fresh selection in the intro works; a reload with Boy is likely wrong. Not fixed yet.
- Quests that nothing in the scene references (for example a future quest given only by code) must be added to `QuestController.questDatabase` in the Inspector, or they can't be restored.
- Two quest assets with the same `questID` (from duplicating an asset) would collide. Give each new quest a unique ID.
- A corrupt `saveData.json` still throws in `LoadGame`; it isn't handled.
- `FindFallbackLocation` relies on `PolygonCollider2D.OverlapPoint` during `Awake`. If the boundary colliders are disabled, it falls back to the lowest location ID ("1").

## Needs testing in Unity
- The scripts compile. This couldn't be verified outside Unity.
- `MainScene` opens with no missing scripts or sprites.
- The L1 → L2 path, the quest chain, and how L3 is reached today.
- Save, quit, load: do quests and NPC progress survive?
- Vytynanka: is it complete and playable?
