# Kiqqi Framework — AI Context Guide

## Overview
Kiqqi Framework is a modular Unity architecture for small "brain training" and casual mini-games built under **Eleverse Logic Games (ELG_FW)**.

All Kiqqi projects share:
- A single 'KiqqiAppManager' root (bootstraps everything).
- Manager-based system for logic separation.
- UI handled through 'KiqqiUIManager' + 'KiqqiUIView' subclasses.
- Optional tutorial flows per game.
- Optional localization, scoring, and analytics modules.

The framework is *fully loaded* in the project; AI tools must **not recreate or duplicate core files.**
All new code should integrate with existing managers, not by on-fly referencing and such.

---

## Core Managers (Do NOT recreate)

| File | Role |
|------|------|
| **KiqqiAppManager.cs** | Root bootstrap, initializes all managers. Singleton ('Instance'). |
| **KiqqiDataManager.cs** | PlayerPrefs-like persistent data wrapper. |
| **KiqqiGameManager.cs** | Central gameplay state controller. Starts mini-games and tutorials. |
| **KiqqiMiniGameManagerBase.cs** | Base class for all mini-game managers. Handles lifecycle and reporting. |
| **KiqqiInputController.cs** | Universal touch/mouse input (tap, hold, swipe, grid). |
| **KiqqiLevelManager.cs** | Level tracking, difficulty management, and progression. |
| **KiqqiAudioManager.cs** | Background music, SFX, and mute control. |
| **KiqqiScoringApi.cs** | Posts score to remote server (e.g., flowly.com endpoint). |
| **KiqqiUIManager.cs** | Controls view transitions, handles 'KiqqiUIView' registration. |

---

## UI View System

All UI screens inherit from **'KiqqiUIView'**:
- Have fade transitions.
- Are registered in 'KiqqiUIManager'.
- Typically correspond to one scene panel.

Common views:
- 'KiqqiMainMenuView'
- 'KiqqiLevelSelectView'
- 'KiqqiPauseView'
- 'KiqqiResultsView'
- 'KiqqiTutorialGameView'
- 'KiqqiTutorialEndView'

---

## Creating a New Mini-Game

**Follow this pattern:**

1. Create a new manager derived from 'KiqqiMiniGameManagerBase', e.g.:
   public class KiqqiShapeMatchManager : KiqqiMiniGameManagerBase
   {
       // Implement Initialize, StartMiniGame, and game-specific logic
   }


## AI Editing Guidelines

When editing or generating code:
- Keep **one class per file** following Unity conventions.
- Place new scripts inside a matching folder, e.g. '/MiniGames/MyGame/'.
- Do not rename existing managers, namespaces, or core file names.
- If a new script interacts with UI:
  - Register it in the 'Canvas' hierarchy.
  - Inherit from 'KiqqiUIView'.
  - Access other views through 'KiqqiAppManager.Instance.UI'.
- If localization is needed, use the existing 'Kiqqi.Localization' system and reference JSON via 'https://kiqqi.com/cscml/'.
- If a tutorial is required, subclass the main manager as '[GameName]TutorialManager' and stop after a fixed number of moves, then show 'KiqqiTutorialEndView'.

## Mini-Game Lifecycle Quick Reference

1. **App Boot** > 'KiqqiAppManager.Awake()' initializes all core managers.
2. **Main Menu** > 'KiqqiMainMenuView' opens first.
3. **Start Game** > 'KiqqiGameManager.StartMainGame()' > launches a 'KiqqiMiniGameManagerBase' subclass.
4. **Gameplay Active** > Mini-game runs logic, updates score, etc.
5. **End Game** > Call 'CompleteMiniGame(finalScore, playerWon)' > triggers:
   - 'KiqqiGameManager.EndGame()'
   - Score post via 'KiqqiScoringApi'
   - Transition to 'KiqqiResultsView'
6. **Results Screen** → Player can restart or return to menu.
7. **Tutorial Flow (optional)** > 'KiqqiGameManager.StartTutorial()' > runs tutorial manager > ends with 'KiqqiTutorialEndView'.

## Project Philosophy

Kiqqi games are designed to feel:
- **Simple**, **bright**, and **rewarding** — never overly complex.
- Educational or cognitive in tone ("brain training", "casual focus").
- Always **playable on web** (WebGL optimized).
- Consistent across all modules, with unified UI and friendly language.
