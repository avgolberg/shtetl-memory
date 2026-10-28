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
- **Dev test saves:** `DevSaves/<preset>.json` + `DevSaves/Use-DevSave.ps1 <preset>` (backs up, then installs to `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Messing Around\saveData.json`). Build presets from the real schema (`Save/SaveData.cs`, `QuestProgress`). Post-intro state = `hasSeenIntro: true` + an active `Find_house_4f12c69d-…` quest (the intro's memory box grants no items). Presets: `l2_street_end_post_intro` (L2 at x 177, next to the L2→L3 waypoint).
- `NPC Hisa Teacher.prefab` picks up runtime sorting-order changes (`m_SortingOrder`); don't commit that noise.
- **Testing:** run static or automated checks yourself. Ask the owner to test only what a change affects, with a short checklist. Full regression only at milestone ends. Label issues runtime-confirmed or code-inspection.

## Where we are (handoff 2026-10-28)
- **Done:** M0 (backup, build list, save fixes); the capture tool; the L7 Mikvah inside L3's left edge; the always-open L2↔L3 route (runtime-confirmed); dev test saves. Details are in `docs/IMPLEMENTATION_STATUS.md` → "Session summary".
- **Git:** branch `main`, remote `origin` = `https://github.com/avgolberg/shtetl-memory.git`. Everything from today is committed and pushed.
  Known uncommitted noise, which stays uncommitted: `Assets/Fonts/veteran typewriter SDF.asset`, `Assets/Prefabs/Characters/NPC Hisa Teacher.prefab` (runtime sorting order), and `Thumbs.db` files.
- **Next task:** M2 item 9, **Location 1, the great-grandmother's house** (see DEVELOPMENT_PLAN → "Next task"). Don't start other locations first.
- **Editor tools** (`Assets/Editor/ShtetlTools/`): `LocationCapture` (read-only capture and transition checks), `MikvahAreaBuilder`, `LocationLinkBuilder`. Each builder has a menu item and a `BuildBatch` entry point and can be re-run safely. Follow the same pattern for new areas.
