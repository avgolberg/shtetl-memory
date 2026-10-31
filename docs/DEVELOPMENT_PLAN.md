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
- **Story flexibility (2026-10-30):** STORY_SPEC is the approved foundation but still a draft. Claude may make small, sensible narrative improvements (clues, bridging lines) that keep historical accuracy and the key story facts; significant changes are discussed with the owner first. Example: the wedding invitation names the synagogue.
- **Custom art (2026-10-30):** the owner generates matching 2D art with ChatGPT. For each milestone Claude lists reusable assets and grouped art requests (what, where, type, size, transparency, layer, style references). Gameplay goes ahead with clearly named `PH_` placeholders, listed in IMPLEMENTATION_STATUS → "Art needed".
- **Game length and depth (2026-10-30):** the whole game targets about 60–90 minutes, with meaningful optional side quests and some nonlinearity, never padding. Reuse the owner's mechanics (hidden-object search, Key Choice, collecting, Vytynanka) to give each location a small investigation or challenge, not only dialogue.
- **NPC voices (2026-10-30):** each NPC gets a distinct voice through the `NPCDialogue` settings (typingSpeed, voicePitch, voiceVolume, autoProgressDelay, voiceSounds) and a speaking style, in accessible English. Halyna: typing 0.06, pitch 0.85, delay 2.5, warm ("dear"). The market belongs to **Maria** (her existing lines point to the market), not Halyna.
- **Leads are non-linear (2026-10-30):** the house finds open the School and the Synagogue as investigation leads; either can come first.
- **Dialogue UI fit:** every dialogue line must fit the dialogue box (about 630 × 78 px at size 36 in the 1920×1080 reference: 2 lines, roughly 60 characters). Choices are one line at size 26 (max button 650 px). Popup titles fit one line (600 px at size 70, about 15 characters); popup texts at most 5 lines. Measure new text against `Assets/Fonts/veteran typewriter.ttf`.

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

## M2.1 Location 1, the great-grandmother's house (✅ complete, runtime-confirmed by the owner 2026-10-30)
Built by `HouseInteriorBuilder` (menu **Shtetl → Build House Interior**); UI fixes by `UITextLayoutFixer` (**Shtetl → Fix UI Text Layout**); item numbering by `FlowerQuestFixer` (**Shtetl → Fix Flower Quest**). Details in IMPLEMENTATION_STATUS → "M2.1".
Flow (the owner's original mechanics restored):
1. Find the house (`FindHouse`; meeting Halyna in her garden also counts).
2. Halyna recognises it but is in a hurry: pick wildflowers for her friend's 70th birthday (`CollectFlowers`, 3 of each colour).
3. Handing in the flowers brings out her husband's old box. She tells the spec story; the key she never used must be in the box.
4. Hidden-object search in the box for the old keys (`FindKeys`); the box disappears once solved. Handing the keys in opens her door; she waits in the garden.
5. Inside: Key Choice on the chest of drawers (`OpenChest`) → 4 popup pages: drawer, family photo, school photo (starts `LeadSchool`), invitation (starts `LeadSynagogue`). Hand in to Halyna.
Test with the dev save `l2_halyna_house_post_intro`. Art: Batch A (IMPLEMENTATION_STATUS → "Art needed").
Open idea: Halyna mentions a wardrobe (spec), the keys open a chest of drawers (spec). Unifying them into one piece of furniture would remove a small inconsistency; ask the owner.

## M2.2 Former Synagogue / House of Culture (in progress, started 2026-10-31)
Location identity: "reading a building" (traces of its first purpose), the Vytynanka workshop, the administrator Oksana, a gradual lead to Semen. Blocks: B0 mini-game → B1a Market Square shell → B1b House of Culture interior shell → B2 traces → B3 Oksana and quest flow → B4 square dressing, polish and docs → B5 art (Batch C).
Owner decisions (2026-10-31):
- **D-a** This is M2.2. Autosave comes separately later.
- **D-b** ✅ Decided: a separate **Market Square** (Location 5), reached from L2's left end near Maria (Option B). L2 had no room for a public building; the square also holds the market (M3.3) and reserves a later School / Library connection. Don't move or replace L2 houses, Halyna's house, the Mikvah or the cemetery route.
- **N1** The square and the House of Culture are always open, also before the wedding invitation is found.
- **N2** Oksana reveals the building's history through the invitation **or** the architectural traces. The Vytynanka workshop is never a dialogue gate.
- **N5** The optional visitors' book clue is signed only "S.", with no date.
- **N6** Cautious wording for the former Torah ark area (e.g. "Old people say the holy cabinet stood here").
- **N7** The façade sign reads «Будинок культури» in Ukrainian (art); the location name in the UI is "Market Square".
- **N9** No generic historical reconstruction illustrations for now.
- **N10** Space is reserved for a later School / Library connection; nothing is implemented.
- Population figures (STORY_SPEC's 60–70%, "several synagogues, this the biggest") and any precise building history stay **provisional until verified**. No unapproved historical facts or new story revelations.
- **Semen lead:** the quest must work whether or not the player has already met him: an objective such as "Ask Semen about the old synagogue", not "Meet Semen".
- **House of Culture art:** the placeholder proportions are provisional. The final building needs a distinctive historic character (not a generic administrative block) and must stay comfortably inside the gameplay camera.
- **Before B1b:** Claude shows a functional interior layout proposal; the owner decides whether two connected views are really needed.
- **D-c** Keep the 1937 wedding. Don't state closure dates, wartime uses or Soviet rebuilding dates without historical support. The informal community help that paid for the wedding is a provisional solution.
- **D-d** Oksana may run the papercutting workshop if it fits her character.
- **D-e** Keep the optional visitors' book clue ("I came to see where my parents were married. — S.").
- **D-f** Keep the existing tree of life with birds; no added Jewish symbols (reyzele is revealed at the School).
- The workshop is **not** a gate: the player may talk to Oksana, explore and cut in any order.

**B0 Vytynanka mini-game: ✅ complete, tested in Unity and approved by the owner (2026-10-31).** Set up by `VytynankaSetup` (menu **Shtetl → Set Up Vytynanka**; it also bakes the silhouette). The owner's tracing content is kept (19 lines, 327 dots, the half template, the paper).
- Flow: paper colour → cutting → **Unfold** (the mirrored half swings open around the fold, the left edge of the half pattern, then moves to the centre; the cut-away paper falls away and the real silhouette remains) → backing paper (5 swatches, the one too close to the paper colour is off; the choice shows at once) → **Keep it**.
- **Continuous cutting (polish, after the owner's test showed problems at close dots and tight curves).** The dots are geometry only (hidden). Each line is a polyline cut by distance along it: the scissors count within 28 canvas units of the line, one step can advance at most 70 units along it (no shortcuts across narrow stems or leaf tips), fast strokes are followed in small steps, the drawn cut always follows the real line. A press continues the glowing line, or starts another line at its marker (within 45 units); a stroke never switches lines. After a line is done the nearest unfinished one glows next. Guides: the next 160 units glow, a pulsing red dot shows where to go on, small dots mark where the other lines start.
  **Second polish (owner's test: the glow jumped ahead where two parts of a line run close, e.g. the birds).** Cause: the cut took the closest point anywhere in the next 70 units, so the return side of a leaf tip or a bird's head could win (9% of test positions, up to 70 units). Now `Follow` is strictly sequential: the cut slides forward from where it is only while the line keeps getting closer to the scissors and stops at the first closest point (an 8-unit allowance for the polyline's corners), every point it passes is within the 28-unit tolerance, at most 80 units per slide; too far from the line, the cut waits. Tested on the real 19 lines (`scratchpad` simulations): 0 advances through any stretch outside the tolerance in 376,920 positions; smooth strokes with up to 18 units of wobble finish all lines without going back.
- **Silhouette (`PapercutSilhouetteBaker`, no new drawing).** The template drawing gives the curves (its lines split the sheet into regions); the traced lines decide which regions are paper (inside Path0, closed along the fold and the bottom edge, and outside every hole line); a drawn line between two openings stays paper (a thin bridge, as drawn: before this fix the petals of the top flower and the small flowers merged into one opening), a line between paper and an opening is split down the middle; a light smoothing that keeps the bridges; mirrored. Checked against the template and the 19 lines: every traced hole lands on its drawn region; 26 openings in the half (20 before). Drawn lines inside paper that no trace marks as a hole (feather and scroll lines) stay paper: a papercut can't show a line without cutting it, and no holes are invented. Output: `Sprites/Items/Vytynanka/Generated/papercut_silhouette.png` (1400 × 1500) and `papercut_icon_mask.png` (readable, for the item). The template is only 93 × 271 px, so the edges are a little soft; **a higher-resolution copy of the same template** (PNG or SVG, ≥ 2000 px tall, same drawing) on `TemplateGhostImage` + a re-run makes it crisp.
- **Workshop screen:** dimmed backdrop, centred board (the game's `panel_brown_damaged`), side column (title, subtitle, instruction, progress, swatches, `button_brown` action button, "Esc: close"), work surface on the right (940 × 1010), close button (`button_brown_close`). The canvas scales by height and draws over the HUD (sorting 25, above the screen fader's 20); on screens narrower than 4:3 the board shrinks to fit. Button labels fill their buttons (they had zero-size rects at the corner), keyboard navigation is off (Space/Enter can't re-press a button), the invisible swatch rows don't catch clicks, the chosen backing has a light frame (no scaling over its neighbours), a backing too close to the paper colour is greyed and crossed.
- **Final polish (owner's test 2026-10-31: the papercut, flowers, openings and mounted piece approved).**
  - Unfold / Keep it labels are dark brown on the light button.
  - An unavailable backing keeps its own colour, fully visible, with a thin (3-unit) diagonal strike-through, and isn't clickable. No fading (that made it look grey on the beige board), no grey tint, no ×.
  - The chosen backing has an even gold frame drawn just behind it (`SelectionFrame`, outside the row's layout), so every swatch keeps its own click area.
  - Paper scraps: when a line is finished, 4–6 flat chips in the paper colour drop 25–50 units, turn a little and fade in 0.4–0.7 s. They are drawn under the glow, so they don't hide the next cut, and are cleared on close.
  - Tracing is unchanged; occasional small jumps are accepted for now (owner).
- Closing (Close button or Esc) before everything is cut never completes it; the cut is kept until the next reload. Closing once everything is cut (also while unfolding or choosing the backing) still completes it, with the shown backing paper.
- First completion only: story flags `vytynanka_paper_<cream|blue|red|green>` + `vytynanka_mount_<ivory|darkred|navy|forest|black>` (`PapercutPalette`), the item **"My vytynanka"** (`Papercut.prefab`, ID 10; `PapercutItem` draws the mounted papercut in the saved colours for the inventory and the pickup popup), `vytynanka_made`, journal topic `vytynanka`, then a save. A full inventory undoes the colour flags, so it can be made again. **A later papercut replaces the kept one** (owner's test 2026-10-31: a green-on-navy papercut didn't change the item, because the save still held the first test's blue paper and, by the old design, later ones changed nothing): exactly one paper flag and one backing flag (`PapercutPalette.SaveChoice`), the item redraws itself (`PapercutItem.RefreshAll`), the popup shows the new piece, then a save; still one item and one journal entry.
- **For the School (M3.2), not implemented yet:** the professor recognises the papercut by `vytynanka_made` (or item ID 10) and talks about reyzelekh and vytynanka; without it, he gives a natural hint to go to the House of Culture workshop. The workshop itself never gates the House of Culture.
- `IMiniGame` (registry ID 5), so a `MiniGameItem` workshop table can open it in B3. It uses `PauseController` (no more `Time.timeScale = 0`).
- **Optional future polish (not blockers, owner 2026-10-31):** occasional small tracing jumps remain (accepted; see "Second polish" for the rule and its tests); a more realistic papercut animation (e.g. the paper lifting or curling as it unfolds, scraps with paper texture). A crisper silhouette needs the same template at ≥ 2000 px tall.
- **Story hooks for the School (M3.2; not implemented):**
  - `vytynanka_made`: set once the player has kept a papercut (the reliable check).
  - `vytynanka_paper_<cream|blue|red|green>` + `vytynanka_mount_<ivory|darkred|navy|forest|black>`: exactly one of each, the colours of the kept papercut (`PapercutPalette.SavedChoice()`), so the professor can name them.
  - Item **"My vytynanka"**, ItemDictionary ID **10** (`Papercut.prefab`, appended; never reorder the list). One item only; a newer papercut replaces its colours.
  - The professor's dialogue: with `vytynanka_made` (or item 10) he recognises "your vytynanka" and talks about Jewish reyzelekh and Ukrainian vytynanka (STORY_SPEC, Location 3); without it, a natural hint to visit the House of Culture workshop. The workshop never gates the House of Culture itself (owner).
- Temporary test entry, development builds and the editor only: **F9** opens it anywhere, **Shift+F9** resets the reward and colours (the journal entry stays). Remove or keep as a dev tool after B3.

**B1a Market Square shell: ✅ complete, runtime-confirmed and approved by the owner (2026-10-31).** Built by `MarketSquareBuilder` (menu **Shtetl → Build Market Square**; re-runnable, touches only the `Market Square` root and its own L2 waypoint pair, calls no other builder). Details in IMPLEMENTATION_STATUS → "M2.2 B1a".
- Location **5 "Market Square"**, root at (0, -330), 144 × 62 (about 2.3 camera views, 18 s to walk across; L2 takes 46 s), default zoom, L2's music.
- Layout, right to left (world x): arrival from L2 (58.8) · House of Culture (17..47, door 32) · plaza with well and notice board (0..7) · market with three stall frames (-54..-8, for M3.3) · reserved post for a later School / Library exit (-59, no transition). All `PH_` placeholders.
- Transitions (always open, development pines): L2 left end (pine appears at x -183.5 at the left wall, in front of the trees) → square arrival (58.8, -349); square pine (64.3) → L2 arrival (-178, -85.4).
- Next: **B1b** needs the owner's approval of an interior layout proposal first (one view or two connected views).

## Next tasks (in order)
1. **M2.2 Synagogue / House of Culture** (above): B1b interior layout proposal → B1b … B5. Completes `LeadSynagogue` (ReachLocation `synagogue`). Known risk for B1b: `NotifyLocationReached` only checks active quests, so a visit before the invitation would not count later (fix with a remembered flag, like Custom objectives).
2. **Autosave** at meaningful milestones (decided 2026-10-30): important discoveries, quest acceptance and completion, location transitions; keep the manual Save button. Separate task, when the owner asks.
3. **Prologue rework** (Batch B art).
4. **M3.2 School**: completes `LeadSchool` (ReachLocation `school`). The professor mentions "your vytynanka" (item "My vytynanka" / flag `vytynanka_made`): give him a line variant for players who come before the Synagogue (the leads are not linear).
5. M3.3 Market, M3.4 Semen + Library, M3.5 Cemetery finale, M4 Epilogue and delivery.
Proposed story order (D1): House → Synagogue → School → Market → Mikvah → Semen at the cemetery gate → Library → Cemetery (stork grave) → Epilogue.

### M2: Rework existing content to match the spec
8. Prologue: funeral, box objects, Yiddish caption.
9. ✅ (complete, runtime-confirmed 2026-10-30) L1 House: Halyna's dialogue per the spec, the key quest opening the chest of drawers, the 3 finds starting the School and Synagogue leads, the house interior (Batch A art integrated 2026-10-30; Play mode test pending).
10. L5 Cemetery: clean up, entry transition, the "find Nathan ben Shlomo" quest gated by the donation list, a decision on the letter.
11. Repurpose or cut the `L1 Forest` content (pending decision D2).

### M3: New locations (story order is pending decision D1)
12. L2 Synagogue / House of Culture: Vytynanka connected to quest, reward and journal; the administrator.
13. L3 School: the professor and reizele.
14. L4 Market: 5 flashback stalls, in the Market Square's reserved market space (built in M2.2 B1a; the plaza can hold the klezmer flashback).
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
- **D5** ~~Interior and flashback art~~ settled: the owner generates custom art (batches A–H).
- **D6** Target platform, audience age, and the grant deadline.
