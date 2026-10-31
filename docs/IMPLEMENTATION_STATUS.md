# Implementation Status

Mapped against [STORY_SPEC.md](STORY_SPEC.md). Last audit: 2026-10-28 (read-only; Unity was not run).

**Labels:** **[C]** confirmed in files · **[I]** inferred · **[T]** needs testing in Unity.
Any status other than Missing still needs a play-test.

## Session summary (2026-10-28)
**Completed and committed on `main`** (the push was blocked by a GitHub 403 that day; fixed since, everything is on GitHub):
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

## M2.2 B1a: Market Square shell (2026-10-31; complete, runtime-confirmed by the owner)
Built by `Assets/Editor/ShtetlTools/MarketSquareBuilder.cs` (menu **Shtetl → Build Market Square**, batch `MarketSquareBuilder.BuildBatch`). Re-runnable: it replaces only the `Market Square` root and its own L2 objects (`L2->Market Square Waypoint` + `… Arrival`), and calls no other builder.
- **Location 5 "Market Square"**: root at (0, -330), below the house interior; boundary x -72..72, y -377..-315 (144 × 62, the same height frame as L2); end walls like L2's (inner faces at x ±64.5); default zoom 17.5; L2's music.
- **Background:** a copy of `L2/Background`. Sky, clouds and the far town layer (`groundLayer2`, distant houses and trees) keep their parallax. The walking ground (`groundLayer0` with its colliders, `groundLayer1`) stands still (parallax 0, no looping), so the square's gameplay objects are plain world positions with no parallax compensation. The copied outer ground pieces (x ±128, ±192) are not created.
- **Placeholders (`PH_`, `Ground1`, with labels), right to left:** House of Culture 30 × 18 + roof at x 17..47 (provisional proportions: one row of 4 tall windows for the double-height hall, door at x 32, mezuzah recess on the right doorpost, sign board "Budynok kultury"); plaza: well (x 0) and notice board (x 7); market: 3 stall frames at x -46, -34, -22 (space -54..-8, for M3.3); reserved post at x -59 for a later School / Library exit (no transition). Bases are placed on the ground by edit-mode raycasts (lowest point under each piece).
- **Transitions (always open; development pines):**
  - `L2->Market Square Waypoint`: a copy of the L2 pine (parallax 0.1), edit x -167.48 → appears at x -183.5 at L2's left wall when the camera is at the street's left limit. Sorted `Ground1` order 3, in front of the street's trees (orders 1–2; behind them the cypress hid it). Its sprite is lowered 2.94 onto the ground and its trigger offset raised by the same amount (trigger unchanged in the world). Arrives in the square at (58.8, -349).
  - `Market Square->L2 Waypoint` at x 64.3 (no parallax) → L2 arrival (-178, -85.4), right of the pine.
- **Finding:** L2's outer ground pieces (`groundLayer0/temp`, `groundLayer1/temp`) are **inactive** in the scene; in play the looping ground moves the active pieces under the player. Edit-mode ground checks on L2 must sample the same point within the active centre piece (the pieces repeat every 64).
- **`LocationCapture`:** a location without NPCs now frames its tiles from the arrival points that lead into it (before: the transition markers' height, which cut off the square's ground).
- **Dev saves:** `l2_left_end_post_intro` (L2 at x -170, next to the new pine), `market_square_post_intro` (in the square at its arrival point).
- **Runtime-confirmed by the owner (2026-10-31):** L2 ↔ Market Square both ways, no transition loops; the player lands and stays on the ground; camera limits and walls; the House of Culture fits the gameplay camera; music continues; save and load inside the square; starting directly in the square; the L2 pine stands on the ground.
- **Capture note:** a faint vertical cloud seam (a tile edge of the copied L2 cloud layer) shows in the square's captures; not reported in Play mode.

## M2.2 B1b: House of Culture interior shell (2026-10-31; complete, runtime-confirmed by the owner)
Built by `Assets/Editor/ShtetlTools/HouseOfCultureBuilder.cs` (menu **Shtetl → Build House of Culture**, batch `HouseOfCultureBuilder.BuildBatch`). It owns the `House of Culture` root and the `Market Square/House of Culture Entrance` group (façade door + arrival). Re-runnable; calls no other builder.
- **Location 6 "House of Culture"**: root (0, -420), below the square. The hall is **71.1 × 20** (two views wide, one view high; west wall x -35.6, east wall x 35.6), floor top y -426 (the paintings' floor line, 80% down), end walls, the square's music at 60%.
- **Camera:** `cameraSize` **10** (view 35.6 × 20; house 8.75, streets 17.5). The boundary is **71.1 × 20.1** (y -430.05..-409.95): a boundary exactly as tall as the view pins Cinemachine's Confiner2D to the shape's midpoint (`ConfinerOven.GetBakedSolution`: `frustumHeight >= theoreticalMaxFrustumHeight`), which stopped the scrolling in the first test. With 0.1 to spare the camera scrolls sideways and stays at the lowest height (the player is on the floor); placeholders bleed 0.3 past the edges. The shared confiner (Oversize Window off) is unchanged; the house interior, an exact fit, stays still on purpose.
- **Transitions (always open):** `House of Culture Door` (square, on `PH_Door`, x 32) → hall arrival (-29.56, -424), about 6 units in from the west wall; `House of Culture Exit` (x -34.5, the west doorway) → square arrival (36, -351.5), right of the door.
- **Ownership on rebuilds:** `MarketSquareBuilder` now keeps the square root and its `Location` (rebuilding only the contents) and the `House of Culture Entrance` group, then calls `HouseOfCultureBuilder.AlignEntrance` so the door trigger and arrival follow the façade door. Run order after a square change: either builder alone keeps everything linked.
- **Façade fix (B1b test):** the ground under the façade varies by about 1.7; the door, windows, sign and roof now stand on the ground at the door (about 1 higher than before), the wall reaches down to the lowest ground under it.
- **Placeholder zones (from the west wall, units):** exit doorway 0–2.4 (6.5 tall); balcony over 0–17.8 (floor 40% down, underside clears the doorways); Oksana + desk 8.5–12.1; visitors' book 13.2–15.6; inner arch 17–19.5 (opening 7.2 tall); workshop nook 23.5–29.2 under the first tall window (x 27.7); hall windows at 42 (lower third bricked up) and 50; chairs 39–55; stage from 57 (1.4 high) with curtain (7 high); plastered arch centred at 64, 6 wide, 8–32% down from the ceiling; pilaster (painting seam) at 35.6. Positions are provisional; the owner adjusts final positions in the Editor.
- **Measured player scale:** sprite 4.54 units tall (4.16 wide), collider 4.4. At camera size 10 a 1672 × 941 painting has 47 px per unit: the player is **about 213 px** tall, doorways 6.5 units ≈ **306 px** (1.43 × the player).
- **Dev save:** `hoc_entry_no_invitation` (in the hall at its arrival point).
- **Runtime-confirmed by the owner (2026-10-31):** square ↔ hall both ways; the camera follows the player through the hall, stops at both walls, no vertical movement; zoom 10 feels right; save and load inside; the façade sits on the ground.

## M2.1 rework after the owner's play-test (2026-10-30; complete, runtime-confirmed by the owner)
Runtime-confirmed by the owner (first version): the main sequence works. Reported: too linear, text overflow, Halyna and the door competing for E, a slow "float" after transitions, the market reminder never shown.
- **Sequence:** the owner's flower quest and the hidden-object key search in the husband's box are back (see DEVELOPMENT_PLAN → M2.1). `FindKeys` is the key search again; the chest of drawers has its own quest `OpenChest`. Halyna has 5 stages (`Part0`–`Part4`). `FindHouse` gives no reward; `FindKeys` opens the door. Flowers: 3 of each colour (18 are on the street). Key items are named "Key with a tag" and "Old key" (inventory and the box's target list).
- **Halyna vs the door:** the cause was her patrol (x 55.6 ↔ 63.0) crossing the door at 58.3. She now paces in her garden left of the door, and E picks the **nearest** interactable (`InteractionDetector`). A `QuestLocationTrigger` (`grandma_house`) on her talk zone completes `FindHouse` when you meet her there.
- **Market reminder:** it was unreachable. `NPC` chose its stage only in `Start`, so after a hand-in the next talk repeated the old stage until a reload. Now `StartDialogue` re-resolves the stage (affects every NPC with quest stages). The market belongs to Maria; Halyna's last stage is personal.
- **Transitions:** the player is placed on the ground at the arrival point (`MapTransition.PlaceOnGround`). Before, they arrived above the ground and sank slowly, because pausing zeroes velocity every frame.
- **Dialogue typing:** the whole line is laid out first and revealed with `maxVisibleCharacters` (no words jumping between lines, auto-size stable). Short pauses after punctuation (`NPC.PauseAfter`).
- **UI text layout (`UITextLayoutFixer`):** dialogue text uses the panel area right of the portrait (679 × 101, auto-size 36→26, 3 lines max). Name, choices, info popup title and description, notifications (410 wide, so they no longer overlap the dialogue panel; text 2–3 lines, auto-size 30→20), quest and journal rows (heights follow the wrapped text), Key Choice row (buttons share the width), key and target labels (wrap and auto-size). Every NPC line in the game was measured with the real font: all fit at size 36 in at most 2 lines.
- **Flower quest fix (`FlowerQuestFixer`, menu Shtetl → Fix Flower Quest; play-test 2026-10-30):** the purple flower was listed twice in `ItemDictionary`, so at runtime it got ID 8 instead of 7 and never counted (the wallet also got 9 instead of 8, and the box's wallet gave a purple flower). The duplicate is removed; prefab IDs and hidden-object IDs now match the list (1 Coin, 2 Item, 3 Key with a tag, 4 Old key, 5 Red, 6 Blue, 7 Purple, 8 Wallet, 9 Photo). All 18 flowers (6 of each colour, 3 needed) lie on the street ground. `ItemDictionary` now logs an error for a duplicated entry instead of silently renumbering.
- **Key box removed when solved:** `MiniGameItem.removeOnComplete` (on `Box.prefab`) destroys the box after the hidden-object search, and `SaveController.LoadSpawnedItems` doesn't respawn a completed box. It uses the existing saved mini-game state (`isCompleted`), no new save data. Not yet runtime-checked: the box stays gone after save and reload. **Fixed 2026-10-30 after the owner saw the box stay:** closing the search window during the 1.5 s pause after the last find stopped the completion coroutine, and reopening reset the count while the found keys stayed hidden, so the box was never marked solved. Now closing in that pause still completes it, found items stay found on reopen, and `MiniGameItem.goneAfterQuest` (`FindKeys` on `Box.prefab`) removes the box once its quest is done, which also clears saves that are already stuck.
- **Dialogue controls (`DialogueController` + `NPC`):** Space shows the whole typing line, the next press goes to the next line (one step per press; a pending auto progress is cancelled so it can't skip a second line); choices still need a click. Revealing a line early now also runs its journal update and quest hand-in (before, it skipped them). Development only (`UNITY_EDITOR || DEVELOPMENT_BUILD`, not in release builds): **F8** or the top-right "DEV: Skip dialogue" button plays the rest of the conversation through the same line logic and stops at the next choice or the end.

## M2.1: Location 1, the house (first version, 2026-10-30; where it differs, the rework above applies)
- **Scene** (`HouseInteriorBuilder`, re-runnable; doesn't touch the Mikvah): new location **"House Interior"** (ID `4`, root at (0, -240), one still camera view 62.2 × 35, floor top y -250). Street door `L2/Parallax/Houses/house/Halyna House Door` (child of the house, so it moves with its parallax 0.1; locked until the `FindHouse` Portal reward, transition reward ID 3); return arrival right of the door. Interior exit is always open. `Chest of Drawers` = `MiniGameItem` (Key Choice, ID 2) + `Drawer Contents` (`InspectPoint`, 4 pages).
- **Story data:** Halyna `Part0` (spec story; hands in `FindHouse` → 1 tagged + 3 plain keys + door), `Part1` (`FindKeys`, re-themed "The old chest of drawers": Custom objectives `open_chest_of_drawers`, `house_finds_seen`), `Part2` (repeat lines, market hint). New quests `LeadSchool`, `LeadSynagogue` (ReachLocation `school` / `synagogue`; completed in M3). New journal topics `family_house`, `family_photo`, `school_photo`, `wedding_invitation`. `CollectFlowers`, the hidden-object key box and the `mezuzah` topic are unlinked from Halyna, not deleted.
- **The unpainted square** appears only *inside* the old family photo (owner decision); nobody explains it yet (Semen does in M3.4).
- **Code:**
  - `SaveData.storyFlags` + `SaveController.SetFlag/HasFlag`: completed Custom objectives and unlocked transitions. Old saves load (the field is new).
  - `QuestController.CompleteObjective(id)`: Custom objectives; a flag counts for quests accepted later.
  - `MapTransition.Unlock()` (Portal rewards) is remembered; restored in `Start`.
  - `InspectPoint` (new): popup pages with optional journal stage, Custom objective and quest start; closing early still applies the unseen pages.
  - `GraveInfoUI`: optional action label ("Next"; E turns the page), close callback, ignores the key press that opened it, `IsOpen`.
  - `MiniGameItem`: optional `questObjectiveId` and `revealOnComplete`; scene-placed items no longer count as "spawned" (they were duplicated on load).
  - `KeyChoiceGameController`: the last key can be the correct one; old buttons are cleared on reopen.
  - `InteractionDetector`: acts once per press (Interact is a Hold action, so a tap sent started + canceled); keeps every interactable in range and falls back to the previous one.
- **UI fit (measured with the real font):** every new dialogue line is at most 2 lines in the dialogue box; the longest choice is about 560 + 40 px (max 650); popup titles are one line; page texts are at most 3 of 5 lines.
- **Runtime-confirmed by the owner (2026-10-30, after the rework):** the full house sequence, from the photo to the finds and leads. Halyna's auto-advance delay is 2.5 s for non-native readers.

## Art needed
Placeholders are named `PH_` in the scene. Shared style brief, references and scale: DEVELOPMENT_PLAN → "Custom art"; the plan of 2026-10-30, section "Art workflow".

**Batch A: Halyna's house interior (M2.1): integrated 2026-10-30** (code-inspection and capture checked; Play mode test pending)
- A1–A6 are in `Assets/Sprites/Interiors/Halyna House/` (the inbox originals stay in `ArtInbox/Batch_A_House/`). Placed by `HouseInteriorBuilder.ApplyArt` (menu **Shtetl → Apply House Art**: art only, no rebuild); the full builder uses it too. All `PH_` objects in the house are gone.
- **The room is drawn closer than the streets** (1672 × 941 painting; its doorway is about 3 players tall at the street zoom). So the house zooms the camera: new `Location.cameraSize` (8.75 here; 0 = the default 17.5 elsewhere), applied by `MapTransition` and on load. The room is one still view, 31.1 × 17.5 units. Floor (feet) at painting y 748 = world y -245.16; the exit is the painted open doorway on the left.
- The chest of drawers (Key Choice) is one sprite: closed, then `chest_open` via `MiniGameItem.updatedSprite`, also after a reload. It moved, so `MiniGameItem.fixedSaveId` keeps its old save ID (`Chest of Drawers_17_-248,7`; IDs are name + position). Since the owner's review (2026-10-30) the wardrobe stands by the door and the chest under the big framed picture, so the player walks into the room to reach it.
- Popup pages: the family photo, school photo and invitation each have their own image (their black scan surround was made transparent). "Old drawer" uses `chest_open`. The family photo shows the family at the table, so the page text and journal line now say that. The family photo text only raises the question of the unfinished square (*amah al amah*, explained later by Semen). The invitation shows the agreed wording on the card (Rivka and David, Ladanivka Synagogue, 1937; STORY_SPEC) through `InspectPoint.Page.imageText` and `GraveInfoUI.imageOverlayText`. `GraveInfoUI` now fits the panel to its content (image by its aspect, description by its text, panel centred); the panel is 94% opaque (was 79%).
- **Still optional:** A7 (open drawer seen from above, 4:3) would suit the "Old drawer" page better than the furniture sprite. A8 (Halyna) only if needed.

**Batch C: Market Square and House of Culture (M2.2): placeholders in place since B1a/B1b (2026-10-31); art not requested yet.** Flat-colour, stylised 2D like the L2 village and Halyna's house; warm muted palette, soft daylight; no heavy outlines, no people; present-day Ukrainian House of Culture details (folk costumes on a rail, an embroidered rushnyk), no Russian motifs; **no readable text in the art** (simple shapes or pictograms on posters; text is laid over in Unity). Sizes are recommendations.
- **C1 House of Culture façade** (`PH_House of Culture`): transparent PNG, about 2048 px wide. A distinctive historic former-synagogue building, not a generic administrative block; tall arched windows matching the interior (some re-glazed or partly bricked up), steps, door, a small slanted groove (mezuzah recess) on the right doorpost, sign «Будинок культури». Must stay comfortably inside the gameplay camera (placeholder: 30 × 18 units + roof; L2 houses are about 14 tall).
- **C7 Square props** (`PH_Well`, `PH_Notice Board`; later a workshop poster): transparent, about 512–1024 px each.
- **C8 Transition signpost or archway** for L2 ↔ Market Square (now development pines): transparent, about 512 × 768.
- **C9 Market stalls** (`PH_Market Stall` ×3): later, with M3.3.
- **Interior scale (measured in B1b):** each painting is **16:9, 1672 × 941**, exactly one camera view (35.6 × 20 units at camera size 10, 47 px per unit). **Floor line at 80% of the height (y 753 px)** in both. A standing adult is **about 205–215 px** tall (the player measures 213), doorways **about 300–310 px**. The paintings are scaled up 0.5% in Unity to cover the boundary's 0.1 margin (nothing important within about 5 px of the top or bottom edge).
- **C2 Painting 1, west half (entrance and foyer)** (percent from the left): 0–6% the open exit doorway (daylight); a deep **balcony** over 0–50% with a wooden railing, its **floor at 40% of the height** (y ≈ 376 px), stacked chairs and old poster tubes on it; under it posters (shapes only), a coat rack, a rail of folk costumes, **empty floor at 24–34% for Oksana and her desk**, **empty floor at 37–44% for the book stand**; 48–55% a wide inner archway into the tall hall (opening ≥ 330 px tall) with a small slanted groove on its right jamb at shoulder height; 55–100% the hall at full height: the first tall arched window at 72–84% on the back wall with **an empty nook at 66–82%** below it for the workshop table and a string of drying paper cutouts above; 96–100% a plain full-height wall **pilaster (the seam)**.
- **C3 Painting 2, east half (main hall and stage)**, made **from Painting 1 as the reference image** ("continue this room to the right; same floor line, ceiling, light, colours"): 0–4% the same pilaster (the two paintings overlap about 64 px and are blended there); 5–55% two more tall arched windows, one with its lower third bricked up and plastered over; low rows of wooden chairs facing right, not hiding the walking line; 60–100% a raised stage (about one third of the player's height, ≈ 66 px) with steps on its left and a heavy curtain; **above the curtain, on the end wall, a large arched niche filled in with plaster** whose outline faintly shows, centred over the stage, between 8% and 32% from the top, about 280 px wide (subtle but readable: the key clue); 100% the end wall.
- **Baked into the paintings:** walls, floor, ceiling; the balcony, railing and stacked chairs; the windows (also the bricked one); the inner archway and its groove; posters, coat rack, costumes, drying papercuts; chairs, stage, curtain, the plastered arch.
- **C4 separate transparent sprites** (standing on their bottom edge): C4a Oksana's desk with a phone, kettle and papers (about 600 × 400, drawn in front of Oksana); C4b the workshop table with coloured paper, scissors and a finished papercut (about 700 × 420; interactive); C4c the visitors' book on a wooden stand (about 300 × 420; interactive).
- **C5 popup close-ups** (4:3, about 1200 × 900, no text): C5a the façade doorpost with the slanted groove; C5b the plastered arch, raking light showing its outline; C5c the open visitors' book: several illegible handwritten entries and one empty ruled line near the middle (the "— S." line is laid over in Unity).
- **Oksana:** body probably from the existing character kit (check in B4); a matching dialogue portrait (about 512 × 512) may be needed.

## Requirements coverage

| Requirement | Status | Current implementation / gap |
|---|---|---|
| **Prologue 1:** funeral; the habits (fasting, matzah, milk and meat apart, meatless borscht) | Partial | `Cinematic Sequence.asset` covers the habits [C]. No funeral setting. The borscht detail sits in Maria's dialogue |
| **Prologue 2:** box with photo (Ukrainian + Yiddish caption), candlestick, siddur, headscarf, letters, dreidel | Partial | The intro mentions the box and the photo "My home. Ladanivka." [C]. Missing: the Yiddish caption and the other objects |
| Hero is Yasha or Sonya | Partial | Boy/girl selection exists (`CharacterSelectionUI.cs`) [C]. The names are not confirmed [T] |
| Feiga's biography (1922, hiding, hospital, 1947 marriage, Inna) | Missing | — |
| **L1 House:** mini-quest 1, find the house from the photo | Implemented [T] | L2 Street, `FindHouse.asset` (ReachLocation) [C] |
| L1: Halyna's dialogue | Implemented [T] | Rewritten per the spec (M2.1) |
| L1: mini-quest 2, bundle of keys → open the chest of drawers | Implemented [T] | The box search gives the keys (`FindKeys`); Key Choice on the `Chest of Drawers` (`OpenChest`) (M2.1) |
| L1: finds (family photo with the unpainted square, school photo → School, invitation → Synagogue) | Implemented [T] | `InspectPoint` pages + journal + the `LeadSchool` / `LeadSynagogue` quests; the locations come in M3 |
| L1: house interior | Implemented with Batch A art [T] | `House Interior` location (camera zoom 8.75) |
| **L2 Synagogue / House of Culture:** windows, mezuzah recesses | Partial: Market Square and interior shells (B1a, B1b; runtime-confirmed 2026-10-31) | Market Square (Location 5) with a `PH_` façade, reached from L2's left end; the House of Culture hall (Location 6) with placeholder zones. Missing: the workshop table (B2), the traces (B3), Oksana and the reveal (B4), art (B5). The `mezuzah` journal topic exists (stage 0 from the case on an L2 house door); B3 extends it |
| L2: mini-quest 6, Vytynanka | ✅ Implemented and tested in Unity (B0, 2026-10-31) | Workshop screen; paper colour, continuous cutting along the traced lines, unfold, baked silhouette, backing paper, item "My vytynanka" in the chosen colours, journal, save; opened by F9 for testing until the workshop table exists (B3). Flags `vytynanka_made`, `vytynanka_paper_*`, `vytynanka_mount_*`; item ID 10 (DEVELOPMENT_PLAN → M2.2 → B0) |
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
- ✅ **Unsaved-work risk** (2026-10-28): resolved; the Vytynanka work and `MainScene` were committed in `b8dc7d7`.
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
