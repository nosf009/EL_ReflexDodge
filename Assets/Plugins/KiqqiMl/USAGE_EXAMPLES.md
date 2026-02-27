# Translation Library Tool - Usage Examples

## Example 1: New Game "Color Action" (Prefix: pf)

### Scenario
Building a new card game with standard UI + custom game elements.

### GameObject Hierarchy
```
Canvas
├── MainView
│   ├── pfTitleText           → "COLOR ACTION"
│   ├── pfSubtitleText        → "Match colors before time runs out!"  (custom)
│   ├── pfPlayBtnText         → "PLAY"                               (standard)
│   └── pfTutBtnText          → "HOW TO PLAY"                        (standard)
├── GameView
│   ├── pfTimeLabelText       → "Time"                               (standard)
│   ├── pfScoreLabelText      → "Score"                              (standard)
│   └── pfColorMatchText      → "Match the color!"                   (custom)
└── ResultsView
    ├── pfPlayAgainBtnText    → "Play On"                            (standard)
    └── pfContinueBtnText     → "Continue"                           (standard)
```

### Translation Library Tool Session

**Step 1: Scan Scene**
```
Detected Prefix: [pf]
Found: 9 localized items
```

**Step 2: Load EN Library**
```
Loaded: 25 entries from library
```

**Step 3: Review Matches**
```
✅ pfPlayBtnText        → playbtntext       [Match: 100%] ✓ PLAY
✅ pfTutBtnText         → tutbtntext        [Match: 100%] ✓ HOW TO PLAY
✅ pfTimeLabelText      → timelabeltext     [Match: 100%] ✓ Time
✅ pfScoreLabelText     → scorelabeltext    [Match: 100%] ✓ Score
✅ pfPlayAgainBtnText   → playagainbtntext  [Match: 100%] ✓ Play On
✅ pfContinueBtnText    → continuebtntext   [Match: 100%] ✓ Continue

⚠️ pfTitleText          → No match (custom)
⚠️ pfSubtitleText       → No match (custom)
⚠️ pfColorMatchText     → No match (custom)
```

**Step 4: Decision**

*Option A - Use Library Keys Directly:*
- Click "Apply All Smart Matches (Use Library Keys)"
- Changes `pfPlayBtnText` → `playbtntext`
- Directly references standard library (recommended for shared terms)

*Option B - Remap to Your Prefix:*
- Click "Remap All to 'pf' Prefix"
- Keeps `pfPlayBtnText` (no change)
- But now maps to standard translation
- Useful if you want prefix consistency in keys

**Step 5: Multi-Language Export**
```
Generated:
- en.json (9 entries: 6 from library, 3 from scene)
- de.json (9 entries: 6 from library, 3 English fallback)
- it.json (9 entries: 6 from library, 3 English fallback)
- fr.json (9 entries: 6 from library, 3 English fallback)
```

**Step 6: Translate Custom Terms**

Edit `de.json`:
```json
{
  "key": "pftitletext",
  "text": "FARBENAKTION"     // manually translated
},
{
  "key": "pfsubtitletext",
  "text": "Farben vor Ablauf der Zeit zuordnen!"  // manually translated
},
{
  "key": "pfcolormatchtext",
  "text": "Farbe zuordnen!"  // manually translated
}
```

Repeat for `it.json` and `fr.json`.

**Result**: 6 standard terms instantly in 4 languages, 3 custom terms need manual translation only.

---

## Example 2: Existing Game "Action Focus" (Prefix: af)

### Scenario
Game already has keys like `afplaybtntext`, `afexitbtntext`. Want to align with new standard library.

### Current Keys
```
afPlayBtnText     → currently "PLAY"
afExitBtnText     → currently "EXIT"
afScoreLabelText  → currently "Punkte"  (oops, German in EN file!)
afTutBtnText      → currently "Tutorial"
```

### Translation Library Tool Session

**Step 1: Load Library**
```
Loaded standard_library_en.json
```

**Step 2: Smart Matches**
```
✅ afPlayBtnText       → playbtntext      [Match: 100%] Library: "PLAY"
✅ afExitBtnText       → exitbtntext      [Match: 100%] Library: "EXIT"
✅ afScoreLabelText    → scorelabeltext   [Match: 85%]  Library: "Score"
⚠️ afTutBtnText        → tutbtntext       [Match: 60%]  Library: "HOW TO PLAY"
```

**Step 3: Review Individual Items**

*afScoreLabelText*:
- Current: "Punkte" (wrong language!)
- Library: "Score" (correct)
- Action: Click "Use Library Key" → fixes the issue!

*afTutBtnText*:
- Current: "Tutorial"
- Library: "HOW TO PLAY"
- Match score 60% because text differs
- Decision: You decide if you want consistent "HOW TO PLAY" or keep "Tutorial"
- Action: Manual choice

**Step 4: Apply Changes**
```
Fixed: afScoreLabelText now correctly shows "Score" in English
Updated: afTutBtnText → "HOW TO PLAY" for consistency
```

**Step 5: Re-export Languages**
```
All 4 JSONs now have QA-approved translations
```

**Result**: Fixed language mix-up, standardized UI text across platform.

---

## Example 3: Multi-Game Shared Keys (Advanced)

### Scenario
You want **one set of language JSONs** to work for multiple games via shared "ml" prefix.

### Setup

**Game 1 - Color Action:**
```
mlPlayBtnText    → "PLAY"
mlExitBtnText    → "EXIT"
caSpecialBtnText → "FLIP CARD" (game-specific)
```

**Game 2 - Action Focus:**
```
mlPlayBtnText    → "PLAY"
mlExitBtnText    → "EXIT"
afSpecialBtnText → "TAP NUMBER" (game-specific)
```

### Standard Library Keys (prefix-free)
```json
{
  "key": "mlplaybtntext",
  "text": "PLAY"
},
{
  "key": "mlexitbtntext",
  "text": "EXIT"
}
```

### Translation Library Tool Usage

**For Color Action:**
```
1. Scan scene → detects prefix: [ca] (game-specific prefix found)
2. Load library
3. Manually assign:
   - Select GameObject with "PlayBtnText"
   - Manual Assignment dropdown → pick "mlplaybtntext"
   - Key changed to "mlplaybtntext"
```

**For Action Focus:**
```
1. Same process
2. Both games now use "mlplaybtntext" and "mlexitbtntext"
3. Game-specific keys remain (caSpecialBtnText, afSpecialBtnText)
```

### Language JSON Structure
```json
// en.json (shared across both games)
{
  "entries": [
    { "key": "mlplaybtntext", "text": "PLAY" },
    { "key": "mlexitbtntext", "text": "EXIT" },
    { "key": "caspecialbtntext", "text": "FLIP CARD" },
    { "key": "afspecialbtntext", "text": "TAP NUMBER" }
  ]
}
```

### Result
- Shared keys: maintained centrally, used by both games
- Game-specific keys: unique per game
- **MEGA CONTROL**: Update "mlplaybtntext" in JSON → affects both games!

---

## Example 4: A/B Testing Translations

### Scenario
QA wants to test two different translations for "Continue" button.

### Setup

**Standard Library (Version A):**
```json
{
  "key": "continuebtntext",
  "text": "Continue"
}
```

**Variant Library (Version B):**
```json
{
  "key": "continuebtntext",
  "text": "Next"
}
```

### Testing Process

**Test Version A:**
```
1. Load standard_library_en.json
2. Apply to scene
3. Export → en.json has "Continue"
4. Build & test
```

**Test Version B:**
```
1. Load variant_library_en.json
2. Apply to scene
3. Export → en.json has "Next"
4. Build & test
```

**Compare Results:**
- Analytics: Which version has better conversion?
- User feedback: Which text is clearer?
- Final decision: Update standard library with winner

### Result
Data-driven translation decisions using the tool for rapid iteration.

---

## Example 5: Handling Dynamic Labels

### Scenario
Game has dynamic text like "Level {0}" that uses template formatting.

### GameObject Setup
```
pfLevelTextDyn    → isDynamic: true
pfScoreTextDyn    → isDynamic: true
```

### Standard Library Entry
```json
{
  "key": "leveltextdyn",
  "text": "Level {0}",
  "isDynamic": true
}
```

### Translation Library Tool

**Match:**
```
✅ pfLevelTextDyn → leveltextdyn [Match: 100%] "Level {0}"
```

**Apply:**
```
Click "Remap to 'pf' Prefix"
Key becomes: pfleveltextdyn
```

### Runtime Usage
```csharp
localizedText.SetKey("pfleveltextdyn");
localizedText.Apply(new { 0 = currentLevel });
// Output: "Level 5"
```

### German Translation
```json
{
  "key": "pfleveltextdyn",
  "text": "Stufe {0}",
  "isDynamic": true
}
```

Runtime output: "Stufe 5"

### Result
Dynamic text templates work seamlessly across languages with smart matching.

---

## Example 6: Bulk Key Renaming

### Scenario
Old game used prefix "old", want to update to "new" prefix.

### Before
```
oldPlayBtnText
oldExitBtnText
oldScoreLabelText
```

### Process

**Step 1: Scan with Library Tool**
```
Detected: [old]
```

**Step 2: Manual Prefix Override**

Edit tool code temporarily or use search-replace in scene file:
```
old → new
```

OR use Translation Library Tool:
```
1. For each item, manually change key:
   oldPlayBtnText → newPlayBtnText
2. Or use batch find-replace in scene YAML
```

**Step 3: Remap to Standards**
```
1. Detected prefix now: [new]
2. Load library
3. Remap all to "new" prefix
```

### Result
All keys updated from old prefix to new prefix, still mapped to standard library.

---

## Pro Workflow Examples

### Daily Development Workflow
```
Morning:
1. Build new UI elements
2. Add KiqqiLocalizedText components
3. Let auto-key generation handle naming

Before Lunch:
1. Open Translation Library Tool
2. Quick scan + apply matches
3. Export languages

Afternoon:
1. Test in-game
2. Identify missing translations
3. Quick fixes with manual assignment

End of Day:
1. Final multi-language export
2. Commit language files to repo
3. Done!
```

### Pre-Release Workflow
```
Week Before Release:
1. Load ALL standard libraries (en, de, it, fr)
2. Verify match coverage (aim for 100%)
3. Export all languages
4. Send custom terms to translation team

3 Days Before:
1. Import translated custom terms
2. Re-export with Multi-Language Exporter
3. Merge standard + custom
4. Build test

1 Day Before:
1. Final QA pass
2. Fix any typos using Translation Library Tool
3. Re-export
4. Build release candidate

Release Day:
1. Verify all 4 languages load correctly
2. Ship it! 🚀
```

---

## Troubleshooting Real Scenarios

### "Some keys matched wrong library entries"

**Example:**
```
pfExitBtnText → matched "exitbtntext" (correct)
pfExitGameText → ALSO matched "exitbtntext" (should be "Exit Game" not "Exit")
```

**Solution:**
```
1. Check match score - likely lower for wrong match
2. Use manual dropdown assignment
3. Create new library entry: "exitgamebtntext" for specificity
```

---

### "Custom term keeps showing as unmatched"

**Example:**
```
⚠️ pfFlipCardText → No match (expected behavior)
```

**Solution:**
```
1. This is CORRECT - it's custom, not in library
2. After Multi-Language Export, manually translate in de/it/fr
3. OR add to standard library if it becomes common across games
```

---

### "Batch apply changed wrong keys"

**Example:**
```
Applied "continuebtntext" to both:
- pfContinueBtnText ✓ (correct)
- pfContinuePlayingText ✗ (wrong - should be game-specific)
```

**Solution:**
```
1. Ctrl+Z to undo batch
2. Lower min score threshold
3. Review matches individually
4. Apply only high-confidence matches in batch
5. Handle edge cases manually
```

---

## Tips from the Trenches

1. **Always review before batch apply** - 30 seconds can save 30 minutes of fixes
2. **Use search filter** - narrow focus when scene has 100+ items
3. **Check isDynamic flag** - dynamic labels need template syntax
4. **Keep library updated** - add common patterns as you discover them
5. **Version control libraries** - track what changed and when
6. **Test with different languages** - catch layout issues early

---

**These examples cover 95% of real-world scenarios. You're all set!** 💯
