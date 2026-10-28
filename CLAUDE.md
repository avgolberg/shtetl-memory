# ShtetlMemory — Claude Code entry point

A 2D educational Unity game (Unity 6000.2.13f1, URP) about a Jewish shtetl. Claude is the primary developer; the owner reviews visuals and play-tests.

## Read first
1. `docs/DEVELOPMENT_PLAN.md`: current milestone, decisions, open questions.
2. `docs/IMPLEMENTATION_STATUS.md`: what exists, what's missing, known bugs.
3. `docs/PROJECT_OVERVIEW.md`: architecture and where the files are (only when needed).
4. `docs/STORY_SPEC.md`: **the authoritative story (Ukrainian).** Never rewrite or shorten it.

## Working rules
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
- **Testing:** run static or automated checks yourself. Ask the owner to test only what a change affects, with a short checklist. Full regression only at milestone ends. Label issues runtime-confirmed or code-inspection.

## Current priority
M0 is done (backup, build list, save fixes). Next is **M1**, practical location assembly: environment prefabs, a location template, and the L7 Mikvah as the proof location.
