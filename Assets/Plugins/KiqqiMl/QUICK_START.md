# Translation Library Tool - QUICK START

## 🚀 5-Minute Setup

### Step 1: Build Your UI
Create UI with proper GameObject naming:
- `pfPlayBtnText` (your game prefix + descriptive name)
- `pfExitBtnText`
- `pfScoreLabelText`

### Step 2: Add Localization Components
```
Menu: Kiqqi > Localization > Kiqqi Localization Manager
1. Click "Scan Scene"
2. Click "Add Static" for all unlocalized items
```

### Step 3: Smart Match & Remap
```
Menu: Kiqqi > Localization > Translation Library Tool
1. Click "Scan Scene"
2. Click "Quick: Load EN Library"
3. Review green matches (good) vs orange unmatched
4. Click "Remap All to 'pf' Prefix" for batch apply
   OR manually assign each item for full control
```

### Step 4: Export All Languages
```
Menu: Kiqqi > Localization > Multi-Language Exporter
1. Click "Load Standard Libraries"
2. Click "Generate All Language Files"
```

### Done! ✅
You now have 4 language files (`en.json`, `de.json`, `it.json`, `fr.json`) in `/Assets/Resources/` ready to use!

---

## 🎮 What Each Tool Does

### Translation Library Tool
**Purpose**: Match scene items to standard translations + remap keys

**When to use**:
- New game using standard UI terms
- Want to align existing keys with standards
- Need individual control over key assignments

**Key Features**:
- Auto-detects your prefix (`pf`, `ca`, etc.)
- Smart matching with confidence scores
- Batch OR individual key remapping
- Live preview of matches

### Multi-Language Exporter
**Purpose**: Generate all 4 language JSONs in one click

**When to use**:
- Ready to export final translations
- Want standard + custom terms combined
- Need all languages at once

**Key Features**:
- Uses library for standard keys
- Falls back to scene text for custom keys
- One-click export for all 4 languages

---

## 💡 Common Scenarios

### "I have standard UI + custom game terms"
1. Use **Translation Library Tool** for standard UI
2. Export with **Multi-Language Exporter**
3. Manually edit `de.json`, `it.json`, `fr.json` for custom terms

### "I want same translation for multiple games"
1. Load **standard library** in Translation Library Tool
2. Remap your prefix (`pf`, `ca`, etc.)
3. All games now reference same QA-approved translations!

### "I want total control over each key"
1. In **Translation Library Tool**, use **Manual Assignment dropdown**
2. OR directly edit key in text field
3. Each item = individual control

### "I want to change keys in JSON, not in scene"
1. Keep scene keys generic (e.g., `mlplaybtntext`)
2. Map different prefixes in JSON files
3. Runtime resolves dynamically

---

## 📊 Understanding Match Scores

| Score | Meaning | Action |
|-------|---------|--------|
| **90-100%** | Exact match | ✅ Safe to batch apply |
| **70-89%** | Very likely match | ✅ Review & apply |
| **50-69%** | Possible match | ⚠️ Manual verify |
| **< 50%** | Unlikely match | ❌ Manual assign |

Adjust **Min Score slider** to filter displayed items.

---

## 🔧 Typical Workflow

```
1. Build UI in Unity
   ↓
2. Add KiqqiLocalizedText components
   ↓
3. Smart match with Translation Library Tool
   ↓
4. Remap keys (batch or individual)
   ↓
5. Export all 4 languages with Multi-Language Exporter
   ↓
6. Test in-game
   ↓
7. Manually translate custom terms if needed
```

---

## ⚙️ Settings Quick Reference

### Translation Library Tool
- **Auto-Detect Prefix**: ON (recommended)
- **Min Match Score**: 70% (default)
- **Show Only Matched/Unmatched**: Use for focused work

### Multi-Language Exporter
- **Library Base Path**: `Assets/Resources/ref-ml/standard_library`
- **Output Path**: `Assets/Resources`

---

## 🎯 Pro Tips

1. **Always scan scene first** - gets latest state
2. **Check match scores before batch apply** - avoid mistakes
3. **Use Ctrl+Z if something goes wrong** - all changes undoable
4. **Keep standard library clean** - only common terms
5. **Export all languages together** - ensures consistency

---

## 📁 Standard Library Location

```
Assets/Resources/ref-ml/
├── standard_library_en.json    (25 common terms)
├── standard_library_de.json
├── standard_library_it.json
└── standard_library_fr.json
```

Edit these files to add/update QA-approved standard translations.

---

## 🆘 Troubleshooting

| Problem | Solution |
|---------|----------|
| No matches found | Load library first ("Quick: Load EN Library") |
| Wrong matches | Lower batch apply, use individual assignment |
| Prefix not detected | Check GameObject naming follows pattern |
| Custom terms not translated | Edit de/it/fr JSONs manually after export |

---

**Read full guide**: `/Assets/Plugins/KiqqiMl/TRANSLATION_LIBRARY_GUIDE.md`

**You got this! 💪**
