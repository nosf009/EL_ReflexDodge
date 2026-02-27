# Kiqqi Translation Library System - Complete Guide

## Overview

The **Kiqqi Translation Library System** enables you to:
- **Reuse QA-approved translations** across multiple games
- **Handle varying GameObject name prefixes** (pf, ca, te, af, etc.)
- **Smart prefix-tolerant matching** between scene items and library translations
- **Individual control** over each localization key assignment
- **Multi-language export** for all 4 languages (en, de, it, fr) at once
- **1:many key:value mapping** - multiple game-specific keys → same standard translation

---

## What's Included

### 📦 Tools

1. **Translation Library Tool** (`Kiqqi > Localization > Translation Library Tool`)
   - Smart matching between scene items and standard library
   - Individual and batch key remapping
   - Prefix detection and management
   - Visual confidence scoring

2. **Multi-Language Exporter** (`Kiqqi > Localization > Multi-Language Exporter`)
   - Generates all 4 language JSONs from current scene
   - Uses standard library translations where available
   - Falls back to current scene text for game-specific entries

### 📚 Standard Libraries

Located in `/Assets/Resources/ref-ml/`:
- `standard_library_en.json` - English QA-approved translations
- `standard_library_de.json` - German QA-approved translations
- `standard_library_it.json` - Italian QA-approved translations
- `standard_library_fr.json` - French QA-approved translations

Each contains **25 common UI terms** used across all Kiqqi games:
- Play, Exit, Tutorial buttons
- Time, Score, Level labels
- Pause menu items
- Results screen buttons
- etc.

---

## Core Concepts

### Prefix-Tolerant Matching

**Problem**: Different games use different prefixes for the same UI element.
- Color Action: `pfPlayBtnText`
- Action Focus: `afPlayBtnText`
- Test Game: `tePlayBtnText`

**Solution**: The tool strips prefixes and matches the **core key**:
- `pfplaybtntext` → strips `pf` → matches library entry `playbtntext`
- `afplaybtntext` → strips `af` → matches library entry `playbtntext`
- Both can map to the same standard translation!

### Smart Matching Score

The tool calculates a **confidence score (0-100%)** based on:
- **70%** - Exact core key match (after stripping prefix)
- **40%** - Partial key match (contains)
- **30%** - Similar key (Levenshtein distance ≤ 3)
- **30%** - Exact text match
- **15%** - Partial text match

Higher scores = more confident match.

### 1:Many Key Mapping

You can have **multiple keys pointing to the same translation**:

**Library** (prefix-free):
```json
{
  "key": "playbtntext",
  "text": "PLAY"
}
```

**Your Games** (with prefixes):
- `pfplaybtntext` → "PLAY"
- `afplaybtntext` → "PLAY"
- `caplaybtntext` → "PLAY"

All point to the same QA-approved translation in all 4 languages!

---

## Workflows

### Workflow 1: Using Standard Translations in New Game

**Scenario**: You're building a new game called "Color Action" (prefix: `pf`). You want to use standard translations.

**Steps**:

1. **Build your UI** in Unity with proper GameObject naming:
   - `pfPlayBtnText`
   - `pfExitBtnText`
   - `pfTimeLabelText`
   - etc.

2. **Add KiqqiLocalizedText components** using existing Localization Manager:
   - Open `Kiqqi > Localization > Kiqqi Localization Manager`
   - Scan scene
   - Add components (keys auto-generated from GameObject names)

3. **Open Translation Library Tool**:
   - `Kiqqi > Localization > Translation Library Tool`
   - Click "Scan Scene" (auto-detects prefix: `pf`)
   - Click "Quick: Load EN Library"

4. **Review Smart Matches**:
   - Tool shows matched items in **green**
   - Unmatched items in **orange**
   - Check match scores (adjust slider if needed)

5. **Individual Control** - For each item, you can:
   - **Use Library Key** - changes your key to match library exactly (`playbtntext`)
   - **Remap to Prefix** - creates prefixed version (`pfplaybtntext`)
   - **Manual Assignment** - pick any library key from dropdown
   - **Direct Edit** - type custom key directly

6. **Batch Apply** (if confident):
   - Set minimum match score threshold (e.g., 70%)
   - Click "Apply All Smart Matches" to use library keys, OR
   - Click "Remap All to 'pf' Prefix" to create prefixed versions

7. **Export All Languages**:
   - `Kiqqi > Localization > Multi-Language Exporter`
   - Click "Load Standard Libraries"
   - Click "Generate All Language Files for Current Scene"
   - Done! You now have `en.json`, `de.json`, `it.json`, `fr.json` in `/Assets/Resources/`

---

### Workflow 2: Adding Game-Specific Custom Terms

**Scenario**: Your game needs custom translations not in the standard library (e.g., "Flip Card", "Match Timer").

**Steps**:

1. Follow **Workflow 1** for standard UI elements

2. **For custom terms**, manually create entries:
   - Add `KiqqiLocalizedText` to custom GameObjects
   - Set custom keys (e.g., `pfflipcardtext`, `pfmatchtimerdyn`)
   - In scene, set the default English text

3. **Export with Multi-Language Exporter**:
   - Tool uses library for standard keys
   - Uses scene text for custom keys (falls back to English)
   
4. **Manually translate custom terms**:
   - Edit `de.json`, `it.json`, `fr.json`
   - Add translations for custom keys
   - Standard keys already populated from library!

---

### Workflow 3: Updating Existing Game to Use Standard Library

**Scenario**: You have an existing game with keys like `afplaybtntext`. You want to align with standards.

**Steps**:

1. **Backup** your current language JSONs

2. **Open Translation Library Tool**:
   - Scan scene (detects prefix: `af`)
   - Load standard library (EN)

3. **Choose your strategy**:

   **Option A: Keep existing keys, use library translations**
   - Click "Use Library Key" for matched items
   - Changes `afplaybtntext` → `playbtntext`
   - Now directly references standard library

   **Option B: Keep prefixed keys, remap to standards**
   - Click "Remap All to 'af' Prefix"
   - Keeps `afplaybtntext` but maps to standard translation
   - Maintains game-specific prefix for organization

4. **Export all languages** using Multi-Language Exporter

5. **Test** in-game to verify translations applied correctly

---

### Workflow 4: Creating New Prefixed Keys from Library

**Scenario**: You want `mlplaybtntext` (multilanguage prefix) to map to standard "PLAY" translation.

**Steps**:

1. **Open Translation Library Tool**
2. **Expand "CREATE NEW PREFIXED KEYS FROM LIBRARY"** section
3. **Select library key** from dropdown (e.g., `playbtntext`)
4. **Tool shows preview**: `ml` + `playbtntext` = `mlplaybtntext`
5. **Note the mapping**: This key will point to library translation in all 4 languages
6. **Manually add this key** to your language JSONs with the library translation text
7. **Assign to GameObject**: Set `KiqqiLocalizedText.localizationKey = "mlplaybtntext"`

This enables **remote capability** - you can change the prefix in JSONs without changing scene objects!

---

## Features Deep Dive

### 🎯 Smart Matching Algorithm

The tool analyzes:
1. **Prefix patterns** (2-3 char lowercase at start)
2. **Core key similarity** (strips prefix, compares)
3. **Text content similarity** (current UI text vs library text)
4. **Levenshtein distance** (typo tolerance)

Produces **confidence score** to help you decide which matches to trust.

### 🔍 Search & Filter

- **Search box**: Filter by key or text content
- **Unmatched Only**: Show items without library match
- **Matched Only**: Show items with library match
- **Min Score slider**: Hide low-confidence matches

### 📊 Statistics Dashboard

- **Total items** in scene
- **Matched count** (above score threshold)
- **Match rate percentage** with visual progress bar
- Helps track localization coverage

### 🔄 Undo Support

All key changes are recorded with Unity's **Undo system**:
- Individual changes: `Ctrl+Z` to revert
- Batch operations: One undo per operation
- Safe to experiment!

### 💾 Export Options

**Individual Export** (existing Localization Manager):
- Exports single language from scene
- Manual per-language workflow

**Multi-Language Export** (new tool):
- Exports all 4 languages in one click
- Intelligently merges library + scene content
- Saves to `/Assets/Resources/` ready to use

---

## Standard Library Management

### Adding New Standard Translations

When you identify a common term used across multiple games:

1. **Open** `/Assets/Resources/ref-ml/standard_library_en.json`

2. **Add new entry** (prefix-free):
```json
{
  "key": "newcommontermtext",
  "text": "NEW COMMON TERM",
  "isDynamic": false
}
```

3. **Translate** to other languages:
   - Edit `standard_library_de.json`, `it.json`, `fr.json`
   - Add same key with translated text

4. **Get QA approval** before committing to library

5. **Use in games** via Translation Library Tool

### Library File Structure

```json
{
  "metadata": {
    "language": "en",
    "generatedAt": "2025-12-02T20:00:00Z",
    "sceneCount": 0,
    "description": "Standard QA-approved translations"
  },
  "entries": [
    {
      "key": "playbtntext",      // Prefix-free key
      "text": "PLAY",              // Translated text
      "isDynamic": false           // Static or dynamic label
    }
  ]
}
```

**Key naming convention**:
- All lowercase
- No prefix (that's added per-game)
- Descriptive suffix: `text`, `labeltext`, `btntext`, `titletext`, `dyn`

---

## Best Practices

### ✅ DO

- **Use auto-detect prefix** - let tool find your pattern
- **Review matches before batch apply** - check confidence scores
- **Keep standard library clean** - only QA-approved common terms
- **Use descriptive GameObject names** - helps matching accuracy
- **Export all languages together** - ensures consistency
- **Version control standard libraries** - track changes over time

### ❌ DON'T

- **Don't batch apply with low min score** - verify matches first
- **Don't put game-specific terms in standard library** - defeats the purpose
- **Don't manually edit generated JSONs** - regenerate instead
- **Don't skip QA for library updates** - maintain quality standards
- **Don't mix prefixes in same scene** - keeps things organized

---

## Troubleshooting

### "No matches found"

**Causes**:
- Library not loaded
- Keys don't follow prefix pattern
- Min score threshold too high

**Solutions**:
- Click "Quick: Load EN Library"
- Check GameObject names follow pattern: `[2-3 char prefix][corekey]`
- Lower min score slider
- Use manual dropdown assignment

### "Wrong translations applied"

**Causes**:
- Matched wrong library entry
- Match score too low

**Solutions**:
- Use `Ctrl+Z` to undo
- Review individual matches before batch
- Increase min score threshold
- Manually assign correct key

### "Custom terms not translated"

**Expected behavior**:
- Multi-Language Exporter uses library for standard keys
- Falls back to scene text (English) for custom keys
- You must manually translate custom keys in de/it/fr JSONs

**Solution**:
- After export, open `de.json`, `it.json`, `fr.json`
- Find custom keys (not in library)
- Add translations manually

### "Prefix not detected"

**Causes**:
- Keys don't follow lowercase prefix pattern
- Mixed prefixes in scene
- Too few items to detect pattern

**Solutions**:
- Manually set prefix by editing `detectedPrefix` logic
- Standardize GameObject naming
- Use manual key assignment

---

## Technical Details

### Files Modified/Created

**New Files**:
- `/Assets/Plugins/KiqqiMl/Editor/KiqqiTranslationLibraryTool.cs`
- `/Assets/Plugins/KiqqiMl/Editor/KiqqiMultiLanguageExporter.cs`
- `/Assets/Resources/ref-ml/standard_library_en.json`
- `/Assets/Resources/ref-ml/standard_library_de.json`
- `/Assets/Resources/ref-ml/standard_library_it.json`
- `/Assets/Resources/ref-ml/standard_library_fr.json`

**Existing Files** (unchanged):
- `KiqqiLocalizationManager.cs` - still used for runtime
- `KiqqiLocalizedText.cs` - still used for components
- `KiqqiLocalizationEditorManager.cs` - still used for basic management

### Unity Compatibility

- **Unity 6 (6000.1)** - fully tested
- **Unity 2023+** - compatible (uses `FindObjectsByType`)
- **Unity 2022 and earlier** - compatible (falls back to `FindObjectsOfType`)

### Dependencies

- `UnityEngine.UI` - for Text component access
- `UnityEditor` - for editor tooling
- No external packages required

---

## Advanced Use Cases

### Remote Localization Updates

1. Keep standard library keys prefix-free in remote JSONs
2. Use "ml" prefix in scene objects
3. Update remote JSONs without touching Unity scenes
4. Runtime loads updated translations automatically

### Multi-Team Workflow

1. **QA Team**: Maintains standard library (ref-ml folder)
2. **Dev Team**: Uses Translation Library Tool per game
3. **Translation Team**: Updates library when new common terms added
4. **CI/CD**: Validates library format on commit

### A/B Testing Translations

1. Create variant library: `standard_library_en_variant.json`
2. Load different library in Translation Library Tool
3. Compare match results
4. Choose best translations for final library

---

## Quick Reference

### Menu Locations

```
Kiqqi
├── Localization
│   ├── Kiqqi Localization Manager      (existing tool)
│   ├── Translation Library Tool        (NEW - smart matching)
│   └── Multi-Language Exporter         (NEW - batch export)
```

### Keyboard Shortcuts

- `Ctrl+Z` - Undo key change
- `Ctrl+F` - Focus search box (standard Unity)

### File Locations

```
Assets
├── Plugins
│   └── KiqqiMl
│       ├── Editor
│       │   ├── KiqqiTranslationLibraryTool.cs      (NEW)
│       │   ├── KiqqiMultiLanguageExporter.cs       (NEW)
│       │   └── KiqqiLocalizationEditorManager.cs   (existing)
│       ├── Component
│       │   └── KiqqiLocalizedText.cs               (existing)
│       └── KiqqiLocalizationManager.cs             (existing)
└── Resources
    └── ref-ml                                       (NEW folder)
        ├── standard_library_en.json
        ├── standard_library_de.json
        ├── standard_library_it.json
        ├── standard_library_fr.json
        ├── en.json                    (existing game-specific)
        ├── de.json
        ├── it.json
        └── fr.json
```

---

## Support

For questions or issues:
1. Check this guide first
2. Review console logs (`[TranslationLibrary]` prefix)
3. Verify standard library JSON format
4. Test with clean scene
5. Contact dev team if issue persists

---

## Changelog

### v1.0 (2025-12-02)
- Initial release
- Smart prefix-tolerant matching
- Individual and batch key remapping
- Multi-language export tool
- Standard library for 4 languages (en, de, it, fr)
- 25 common QA-approved translations included

---

**You now have MEGA CONTROL over your localization workflow!** 🚀
