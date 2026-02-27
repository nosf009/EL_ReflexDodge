# Manual Translation Mapper - Usage Guide

## Overview

The **Manual Translation Mapper** gives you **complete manual control** over mapping library translation keys to scene GameObjects. This is the **robust, human-verified workflow** for production-ready localization.

---

## The Workflow

### 1. **Load Your Translation Library (Left Panel)**
- Click **"Quick: Load en.json"** to load your English translations, OR
- Click **"Load JSON"** to load any JSON file (standard library, de.json, etc.)
- Library entries appear on the left

### 2. **Click an Entry (Left Panel)**
- Click on any library entry
- Tool **automatically finds** best matching GameObject in scene
- GameObject is **selected in Unity Hierarchy**
- Details appear in right panel

### 3. **Review Details (Right Panel)**
See exactly what you're working with:
- **Library Entry**: Key and text from JSON
- **Matched GameObject**: Name, path, and current state
- **Key Comparison**:
  - Key from Canvas (what GameObject currently has)
  - Key from JSON (what library expects)
- **Text Comparison**:
  - Text in Canvas (current visual text)
  - Text in JSON (translation)

### 4. **Sync Keys (Right Panel)**
- If keys don't match → Click **"SYNC TO JSON"**
- GameObject key updates to match library key
- Change is recorded with Undo (Ctrl+Z to revert)
- GameObject marked dirty for saving

### 5. **Validate Multi-Language (Right Panel)**
- Click **"Load All 4 Languages for Validation"**
- Tool checks if all 4 JSONs (en, de, it, fr) have the same key
- Shows which languages have the translation
- Highlights missing translations

### 6. **Repeat for Each Entry**
Go through library 1-by-1, verifying and syncing as needed.

---

## Example Session

### Scenario
You have `en.json` with QA-approved translations. Scene has GameObjects with prefixed keys like `pfPlayBtnText`. You want to sync them to match `playbtntext` from library.

### Step-by-Step

**1. Open Tool**
```
Menu: Kiqqi > Localization > Manual Translation Mapper
```

**2. Scan Scene**
```
Toolbar: Click "Scan Scene"
Console: "Scanned scene: 15 localized components found"
```

**3. Load Library**
```
Toolbar: Click "Quick: Load en.json"
Left Panel: Shows 25 entries (playbtntext, exitbtntext, etc.)
```

**4. Select First Entry**
```
Left Panel: Click on "playbtntext"
```

**What Happens**:
- Tool finds GameObject with key `pfPlayBtnText` (best match)
- GameObject selected in Hierarchy (you see it highlighted)
- Right panel shows:
  ```
  Key from Canvas: pfplaybtntext
  Key from JSON: playbtntext
  Text from Canvas: PLAY
  Text from JSON: PLAY
  
  ⚠ Keys don't match
  ```

**5. Sync Keys**
```
Right Panel: Click "SYNC TO JSON"
```

**Result**:
- GameObject key changed: `pfPlayBtnText` → `playbtntext`
- Right panel now shows: ✓ Keys already match!
- Console: "Synced 'PlayBtn': 'pfplaybtntext' → 'playbtntext'"

**6. Validate Languages**
```
Right Panel: Click "Load All 4 Languages for Validation"
```

**Shows**:
```
✓ EN: PLAY
✓ DE: SPIELEN
✓ IT: GIOCA
✓ FR: JOUER

✓ All 4 languages have this key with same structure!
```

**7. Move to Next Entry**
```
Left Panel: Click "exitbtntext"
```

Repeat the process!

---

## Key Features

### **Automatic GameObject Matching**

When you click a library entry, the tool:
1. Searches all scene components for best match
2. Scores based on:
   - Exact key match (100 points)
   - Key after stripping prefix (80 points)
   - Partial key match (50 points)
   - Text similarity (up to 50 points)
3. Selects best match automatically
4. Highlights it in Unity Hierarchy

### **Manual Verification**

You always see:
- Current state (GameObject key & text)
- Target state (JSON key & text)
- Clear comparison before syncing
- Option to skip if match is wrong

### **Multi-Language Validation**

Ensures all 4 languages have:
- **Same key** (e.g., `playbtntext`)
- **Translation exists** in each file
- **No missing entries**

Perfect for QA checks!

### **Undo Support**

Every sync action is recorded:
- `Ctrl+Z` to undo key change
- Safe to experiment
- Revert mistakes easily

---

## Use Cases

### **Use Case 1: Initial Setup**

**Scenario**: New game, scene has prefixed keys, want to use library keys.

**Workflow**:
1. Load `en.json`
2. Click through each entry
3. Review matched GameObject
4. Sync to JSON if correct match
5. Done!

**Time**: 2-3 minutes for 25 entries

---

### **Use Case 2: Fixing Mismatched Keys**

**Scenario**: Keys don't align with QA-approved library.

**Workflow**:
1. Load `en.json`
2. Click entry → see mismatched keys
3. Sync to JSON to fix
4. Validate all languages have it
5. Next entry!

**Time**: 1 minute per entry

---

### **Use Case 3: Pre-Release QA Check**

**Scenario**: Before release, verify all translations exist in all languages.

**Workflow**:
1. Load `en.json`
2. For each entry:
   - Click entry
   - Load all 4 languages
   - Check for missing translations
   - Note any gaps
3. Send gaps to translation team
4. Re-import and verify

**Time**: 5 minutes for full validation

---

### **Use Case 4: Mixed Prefixed & Direct Keys**

**Scenario**: Some GameObjects use `pfplaybtntext`, others use `playbtntext` directly.

**Workflow**:
1. Click library entry
2. Tool finds match regardless of prefix
3. If prefixed: Sync to remove prefix
4. If direct: Already matches, skip
5. Result: All GameObjects use consistent library keys

---

## Best Practices

### ✅ **DO**

1. **Always scan scene first** - ensures fresh data
2. **Review each match** - verify GameObject is correct before syncing
3. **Use search filter** - narrow down entries quickly
4. **Validate languages before release** - catch missing translations early
5. **Commit after sync session** - save your work
6. **Use Undo if needed** - mistakes are reversible

### ❌ **DON'T**

1. **Don't sync blindly** - always verify matched GameObject is correct
2. **Don't skip validation** - multi-language check is critical
3. **Don't forget to scan** - scene changes won't show without re-scan
4. **Don't edit keys manually in scene** - use this tool for consistency

---

## Understanding the Match

### **How Matching Works**

The tool scores each GameObject against the library entry:

**Example**: Library entry `playbtntext`

| GameObject Key | Match Type | Score | Selected? |
|----------------|------------|-------|-----------|
| `playbtntext` | Exact | 100 | ✅ YES |
| `pfplaybtntext` | Prefix-stripped | 80 | If no exact |
| `playbutton` | Partial | 50 | If no better |
| `exitbtntext` | No match | 0 | ❌ NO |

**Best match is always selected automatically.**

### **What If Wrong Match?**

If the tool selects the wrong GameObject:
1. **Don't sync** - just skip to next entry
2. **Manually select correct GameObject** in Hierarchy
3. **Manually change key** in Inspector or use old tool
4. Tool is for **approximate matching** - not 100% perfect

**This is why it's called MANUAL mapper - YOU verify each one!**

---

## Keyboard & Navigation

### **Efficient Workflow**

1. Click entry (left panel)
2. Review details (right panel)
3. `Space` or click "SYNC TO JSON" if correct
4. Click next entry
5. Repeat

### **Tips**

- Use search filter to focus on specific entries
- GameObject is auto-selected - visible in Hierarchy
- Keep Inspector open to see component details
- Work top-to-bottom through library

---

## Comparison: Manual vs. Smart Matching

| Feature | Manual Mapper (NEW) | Smart Matching (OLD) |
|---------|---------------------|----------------------|
| Control | 100% human-verified | Batch with overrides |
| Speed | 2-3 min for 25 items | 10 sec for 25 items |
| Accuracy | You decide each | Algorithm decides |
| Workflow | 1-by-1 verification | Batch apply + review |
| Best For | Production/QA | Rapid prototyping |

**Use Manual Mapper when you need ROBUST, VERIFIED localization.**

**Use Smart Matching when you trust automated matching for speed.**

---

## Multi-Language Validation Details

### **What It Checks**

When you click "Load All 4 Languages for Validation":

1. **Finds all 4 JSON files** in same directory as loaded library
2. **Searches each** for the selected library key
3. **Reports status**:
   - ✓ Found with translation text
   - ⚠ Missing from that language

### **Example Output**

```
✓ EN: PLAY
✓ DE: SPIELEN
⚠ Missing in: it, fr
```

**Action**: Add `playbtntext` to `it.json` and `fr.json` with translations.

### **Validation Use Cases**

- **Pre-release check** - ensure all translations exist
- **After translation import** - verify completeness
- **QA pass** - catch missing entries
- **Consistency check** - same keys across all files

---

## Troubleshooting

### **"No matching GameObject found"**

**Cause**: Library key doesn't match any scene GameObject.

**Solutions**:
1. Check if GameObject exists in scene
2. Scan scene again (maybe new objects added)
3. Search filter might be hiding it
4. Key might be completely different (custom term)

**Action**: Skip this entry or manually create GameObject with this key.

---

### **"Wrong GameObject selected"**

**Cause**: Multiple similar keys, tool picked wrong one.

**Solutions**:
1. Don't sync - skip to next entry
2. Manually select correct GameObject in Hierarchy
3. Use old Localization Manager to change key manually
4. Improve GameObject naming for better matching

**Action**: This is expected - YOU verify each match!

---

### **"Keys synced but text still wrong in-game"**

**Cause**: Keys match, but translation missing from JSON.

**Solutions**:
1. Check JSON file has the key
2. Verify KiqqiLocalizationManager is loading correct JSON
3. Test with Multi-Language Validation feature
4. Reload game to refresh translations

**Action**: Validate all 4 languages have the key + translation.

---

### **"Validation shows missing languages"**

**Cause**: Some JSON files don't have this key yet.

**Solutions**:
1. Note missing keys (write them down)
2. Open each missing JSON file
3. Add entry with same key, translated text
4. Re-validate

**Action**: This is NORMAL - custom terms need manual translation!

---

## Pro Workflow

### **Daily Development**

```
Morning:
1. Build UI with new elements
2. Add KiqqiLocalizedText components

Midday:
1. Open Manual Mapper
2. Load en.json
3. Go through new entries
4. Sync keys for standard terms
5. Note custom terms for translation

Evening:
1. Validate all 4 languages
2. Send missing translations to team
3. Commit synced scene
```

### **Pre-Release QA**

```
1 Week Before:
1. Open Manual Mapper
2. Load en.json
3. Click through ALL entries
4. Validate each has all 4 languages
5. Export report of missing translations

3 Days Before:
1. Import translated terms
2. Re-validate with Manual Mapper
3. Verify 100% coverage
4. Test in-game

1 Day Before:
1. Final validation pass
2. Fix any last-minute issues
3. Build release
```

---

## Summary

**Manual Translation Mapper** = **Human-Verified, Production-Ready Localization**

- **Left Panel**: Library entries (your source of truth)
- **Right Panel**: Details & sync controls (verify before action)
- **Workflow**: Click → Review → Sync → Next
- **Validation**: All 4 languages checked
- **Safety**: Undo support, manual control

**You now have MEGA CONTROL + MEGA VERIFICATION!** 💪

Perfect for production workflows where accuracy > speed.
