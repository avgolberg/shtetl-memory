# ShtetlMemory — Claude Code entry point

A 2D educational Unity game (Unity 6000.2.13f1, URP) about a Jewish shtetl. Claude is the primary developer; the owner reviews visuals and play-tests.

## Read first
1. `docs/DEVELOPMENT_PLAN.md`: current milestone, decisions, open questions.
2. `docs/IMPLEMENTATION_STATUS.md`: what exists, what's missing, known bugs.
3. `docs/PROJECT_OVERVIEW.md`: architecture and where the files are (only when needed).
4. `docs/STORY_SPEC.md`: **the authoritative story (Ukrainian).** Never rewrite or shorten it.

## Working rules
- **Language:** all in-game text (dialogue, quests, journal, UI) is written in **English**. `docs/STORY_SPEC.md` stays in Ukrainian as the source of truth; translate from it faithfully.
- **Accessible English:** many players aren't native English speakers. Use clear, natural, simple English: common words, short sentences (about 12 words or fewer per dialogue line), no idioms or rare vocabulary. Keep the historical and educational meaning of STORY_SPEC.
- **Two scene workflows:**
  - **Unity closed:** run the builders and the capture yourself in batch mode (`-executeMethod <Builder>.BuildBatch`; they save the scene and capture).
  - **Unity open:** don't restart it. Ask the owner to exit Play mode, run the menu item (**Shtetl → Build …**; it refuses in Play mode or outside MainScene, and captures automatically), then press **Ctrl+S**. Then read `Snapshots/` yourself.
  - Check with `tasklist //FI "IMAGENAME eq Unity.exe"` before choosing. Batch mode aborts if the project is open.
- **New environment objects** must match the local palette (tint like neighbouring props), sorting layer and order (behind or in front of the right props), and scale. Check them in a zoomed capture crop before showing the owner.
- Keep the single-scene architecture: `Assets/Scenes/MainScene.unity`, with locations as root objects that have a `Location` component.
- Reuse existing systems (dialogue, quests, journal, rewards, transitions, mini-games) rather than rebuilding them.
- Edit Unity YAML directly only for simple, safe changes. Use Editor scripts in `Assets/Editor/` for repetitive or many-reference changes.
- Preserve GUIDs, fileIDs, prefab overrides and working content. Never touch `.meta` GUIDs.
- Before editing a scene or prefab file, ask the owner to save and close it in Unity, or confirm it isn't open with unsaved changes.
- Never search `Library/`, `Temp/`, `obj/`, `Logs/`, `Assets/_Recovery/` or the binary art broadly. `MainScene.unity` is about 3 MB: grep it, don't read it whole.
- Git on drive D needs `git -c safe.directory=* …` (don't change the global config). Commit only when the owner asks.
- Paid assets are gitignored (`Assets/Cutscene`, `Assets/Sprites/Village Assets`, `.../EasyChara Paid`). Don't delete or move them.
- Mark claims as confirmed, inferred or needs-Unity-testing. Update `docs/IMPLEMENTATION_STATUS.md` and `docs/DEVELOPMENT_PLAN.md` when a milestone item is done.
- Keep token use low: targeted greps, no parallel agents unless asked.
- **Visual check:** `Assets/Editor/ShtetlTools/LocationCapture.cs` (menu **Shtetl → Capture Locations**) is read-only. Per location it writes `Snapshots/<id_name>/overview.png` (editor layout), `tile_XX.png` (player view at walking level, parallax simulated) and `Snapshots/summary.txt` (layers, transitions, NPCs, issues). `Snapshots/` is gitignored.
  When Unity is closed, run it yourself (about 15 s; the exit code is non-zero on failure):
  `"/d/Unity/6000.2.13f1/Editor/Unity.exe" -batchmode -projectPath "D:/Unity Projects/ShtetlMemory" -executeMethod LocationCapture.CaptureAllBatch -logFile <scratch log>`
  The Unity editor lives at `D:\Unity\6000.2.13f1`; the `E:\` path in the `.csproj` files is stale. View only the tiles you need, since images cost tokens.
- **Dev test saves:** `DevSaves/<preset>.json` + `DevSaves/Use-DevSave.ps1 <preset>` (backs up, then installs to `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Messing Around\saveData.json`). Build presets from the real schema (`Save/SaveData.cs`, `QuestProgress`). Post-intro state = `hasSeenIntro: true` + an active `Find_house_4f12c69d-…` quest (the intro's memory box grants no items). Presets: `l2_street_end_post_intro` (L2 at x 177, next to the L2→L3 waypoint), `l2_halyna_house_post_intro` (L2 at x 48, left of Halyna's house), `l2_left_end_post_intro` (L2 at x -170, next to the L2→Market Square waypoint), `market_square_post_intro` (in the Market Square at its arrival point), `hoc_entry_no_invitation` (in the House of Culture hall at its arrival point). Before installing one, have the owner copy their `saveData.json` to a named backup (each install backs up whatever is installed at that moment).
- `NPC Hisa Teacher.prefab` picks up runtime sorting-order changes (`m_SortingOrder`); don't commit that noise.
- **Testing:** run static or automated checks yourself. Ask the owner to test only what a change affects, with a short checklist. Full regression only at milestone ends. Label issues runtime-confirmed or code-inspection.

## Where we are (handoff 2026-10-28)
- **Done:** M0 (backup, build list, save fixes); the capture tool; the L7 Mikvah inside L3's left edge; the always-open L2↔L3 route (runtime-confirmed); dev test saves. Details are in `docs/IMPLEMENTATION_STATUS.md` → "Session summary".
- **Git:** branch `main`, remote `origin` = `https://github.com/avgolberg/shtetl-memory.git`. Pushing works (the 2026-10-28 403 was fixed with repo-local credentials for `avgolberg`; everything up to `325405a` is on GitHub). Commit and push only when the owner approves. Never force-push.
  Known uncommitted noise, which stays uncommitted: `Assets/Fonts/veteran typewriter SDF.asset`, `Assets/Prefabs/Characters/NPC Hisa Teacher.prefab` (runtime sorting order), `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset` (dynamic atlas), `Thumbs.db` files and `ArtInbox/` (art sources). `MainScene.unity` also picks up `m_SortingOrder` churn after Play mode (on `SortingGroup`s and as prefab-instance overrides): stage the scene without it (compare objects against `HEAD`, restore those values, stage the cleaned copy with `git hash-object -w` + `git update-index --cacheinfo`).
- **2026-10-30:** the owner's manual Mikvah adjustments are committed (`f6b266e`). **Never re-run `MikvahAreaBuilder` or `LocationLinkBuilder`**: they would reset those hand-tuned areas. M2.1 (the house) is complete and runtime-confirmed. **M2.2 is the Synagogue / House of Culture** (owner decision 2026-10-31; autosave comes separately later). Block B0, the Vytynanka mini-game, is complete and tested in Unity (2026-10-31). **B1a (Market Square, Location 5, reached from L2's left end) and B1b (House of Culture hall, Location 6, camera size 10, horizontal scrolling) are complete and runtime-confirmed (2026-10-31).** Next: B2, the workshop corner (see DEVELOPMENT_PLAN → "M2.2" for the owner's decisions and the planned extensions X1–X5). **The owner adjusts final visual positions of buildings, sprites and props in the Editor:** once they have, don't re-run `MarketSquareBuilder` or `HouseOfCultureBuilder` without asking (a rebuild resets their contents); add later content with targeted builders or setup scripts that touch only new objects. Dialogue testing: Space shows the whole line, then goes to the next line; **F8** (development builds and the editor only) skips to the next choice or the end.
- **Editor tools** (`Assets/Editor/ShtetlTools/`): `LocationCapture` (read-only capture and transition checks), `MikvahAreaBuilder`, `LocationLinkBuilder`, `HouseInteriorBuilder`, `MarketSquareBuilder` (re-runnable; only its own root's contents and L2 waypoint pair; keeps the root, its Location and the House of Culture entrance), `HouseOfCultureBuilder` (re-runnable; its root and the square's entrance group; logs the measured player scale), plus the one-off fixers `UITextLayoutFixer` and `FlowerQuestFixer` (both idempotent), and `VytynankaSetup` (wires the mini-game; idempotent). Each builder has a menu item and a `BuildBatch` entry point. Follow the same pattern for new areas.
- **Art:** the owner generates custom art; keep `PH_` placeholders and the "Art needed" table in IMPLEMENTATION_STATUS current.
- **Dialogue text** must fit the UI: see DEVELOPMENT_PLAN → "Dialogue UI fit".
