# Implementation Status

Mapped against [STORY_SPEC.md](STORY_SPEC.md). Last audit: 2026-10-28 (read-only; Unity was not run).

**Labels:** **[C]** confirmed in files · **[I]** inferred · **[T]** needs testing in Unity.
Any status other than Missing still needs a play-test.

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
| **L7 Mikvah** (pond on the way to the cemetery) | Missing | The Fisherman's dialogue has half of the spec's line but stands in the cemetery |
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
- **Build settings** list only `Assets/Scenes/ProtoScene.unity`, which doesn't exist. `MainScene` is not in the build [C].
- **Quests likely don't load after a restart.** `SaveData.questProgressData` serializes `QuestProgress.quest` (a ScriptableObject reference) through `JsonUtility` [I/T].
- **NPC dialogue stage isn't saved:** `NPC.currentStageIndex` is missing from `SaveData` [C].
- **Saving happens only** on first run, on the intro, on character selection, and from the menu Save button. No autosave [C].
- **Unsaved-work risk:** uncommitted changes in `MainScene.unity` and the untracked Vytynanka folder (plus its prefab and SFX).
- **Paid assets aren't in Git.**
- `ObjectiveType.TalkNPC` and `Custom` are declared but appear unhandled [I].

## Needs testing in Unity
- `MainScene` opens with no missing scripts or sprites.
- The L1 → L2 path, the quest chain, and how L3 is reached today.
- Save, quit, load: do quests and NPC progress survive?
- Vytynanka: is it complete and playable?
