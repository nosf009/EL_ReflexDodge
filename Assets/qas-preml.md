BEZI QA / Standards CHECKLIST  
for "Kiqqi Mini-Game Framework" based games

# GENERAL INSTRUCTION #

Follow the tasks in each section, 2 by 2 in batch, reporting back shortly on results after you analyze, plan out, and complete them. Ask for manual confirmation via chat to continue to the next 2 tasks.

If there is less than 2 remaining at any time in a given section, complete that last task in the final iteration.

Never move from section to section without explicitly notifying that the current section is complete and the next one is in order. At the very end, provide a short highlight summary only.

---

## SCENE STRUCTURE CONTEXT

In Kiqqi FW, production scenes follow this pattern:

- Only one scene is open during QA sprint
- Scene naming:
  - <SceneName>_m or _m_web = mobile (9:16–9:20)
- Mobile scene is always built first and fully completed (including multilanguage)
- Desktop scene is a structural duplicate of mobile with aspect differences only

Hierarchy, GameObject names, and manager wiring should match between scenes.
IMPORTANT:
In this specific case, we're doing this when there's only _m scene existing. We do not bother with _w.

---

## BEFORE YOU START

Before executing anything further:

1. Ask for:
   - Game name
   - gameId
   - prefix

IMPORTANT: WE do not deal with tutorial in this QA run. At all. Focus only on prototypes view/manager.
Keep this information in memory for all later steps, as multiple tasks depend on it.

---

## A) GAME MECHANICS  
Negative score, duplicate score, scoring API

1. Analyze whether the score for the current level is duplicated at the end of a level.
   - If confirmed, fix it.

2. Analyze whether score can become negative due to bonuses and penalties.
   - Score must never go below 0.
   - Fix if needed.

---

## B) GAME FLOW  
Pause, resume, reset transitions

### 1. PAUSE FLOW ANALYSIS

Pause behavior must be carefully analyzed per game.
Even, this may be already fully correct and bug free, so analyze well first.
Ask for confirmation before deciding anything.

#### 1a. TimeScale usage
- Preferred solution is using Time.timeScale = very small value (e.g. 0.0001f)
- Avoid micro-managing coroutines
- UGUI input must remain functional
- Blocking taps can be handled via invisible panel in pause view

#### 1b. If TimeScale is NOT used
- Analyze current implementation
- Summarize issues
- Ask for confirmation whether to rework

#### 1c. If rework is approved
- Implement simplified pause
- Provide short highlight summary before moving on

---

## C) VARIOUS ANALYSIS ITEMS

### 1. MUTE / UNMUTE ISSUE

Observed behavior:
- Muting and unmuting works during the same session
- After mute → unmute:
  - Sound effects return
  - Background music does not

Task:
- Analyze KiqqiAudioManager
- Fix so background music resumes correctly after unmute
- Ensure behavior is consistent across sessions

---

## D) UGUI LABEL ADJUSTING / VALIDATION

### 1. MOBILE SCENE ONLY (_m)

Do NOT modify _w / _web scenes.

Only modify RectTransform values explicitly listed below.

- Remove KiqqiLocalizedText from:
  - MainView/TitleText
  - ResultsEndView/TitleText
- Set label text to uppercase GAME NAME

---

### A) MAIN MENU VIEW

- Validate TitleText:
  - No KiqqiLocalizedText attached
  - Text is uppercase GAME NAME

---

### B) GAMEPLAY VIEW

- TimePanel:
  - Width: 260
  - PosX: -425
  - PosY: -20
  - Height: 90

- Inside TimePanel:
  - TimeLabelText PosX: -38
  - TimeValueText PosX: 30

- ScorePanel:
  - Width: 260
  - PosX: -150
  - PosY: -20
  - Height: 90  

- Inside ScorePanel:
  - ScoreLabelText PosX: -38
  - ScoreValueText PosX: 30

- SkipTutBtn:
  - Width: 260
  - PosX: 150
  - PosY: -20
  - Height: 90  

---

### 2. PAUSE VIEW

Inside pause view:

- Find pvBtnsPanelTitleText:
  - Remove KiqqiLocalizedText
  - Set text explicitly to "PAUSE"

- Find LevelSelectBtn:
  - SetActive(false)

---

## E) VARIOUS

### 1. SCORE LABEL FORMATTING

Find all occurrences of: gameManager.CurrentScore.ToString("00000") in the code of the current game. Problem is - 5 digits is too much, we need to find all these and format them to 4 digits ("0000"). This is for the score label in game view.
