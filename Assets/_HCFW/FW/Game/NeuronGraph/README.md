# Neuron Graph - Game Documentation

## Game Overview

**Genre:** Logic Puzzle / Brain Training  
**Format:** Puzzle Rush (solve multiple puzzles within time limit)  
**Core Mechanic:** Color toggle puzzle inspired by "Lights Out" games

## How to Play

1. Network of colored nodes appears on screen
2. Goal: Make all nodes the same target color
3. Tap any node → that node and all connected neighbors toggle colors
4. Solve puzzle → new puzzle appears immediately
5. Continue solving until time runs out
6. Score increases with each solved puzzle
7. Combo multipliers reward consecutive solves

## Core Mechanics

### Color Toggle System
- Each node has a color (2-4 colors depending on difficulty)
- Tapping a node toggles its color AND all neighbor colors
- Colors cycle: Blue → Red → Green → Yellow → Blue
- Number of active colors scales with difficulty

### Network Topology
Nodes are arranged in different graph structures:
- **Line:** Simple A—B—C chain
- **Triangle:** 3 nodes, fully connected
- **Square:** 4 nodes in a loop
- **Star:** Center node connected to all others
- **Pentagon:** 5 nodes in a ring
- **Hexagon:** 6 nodes in a ring

### Puzzle Generation (Reverse Engineering)
**Critical Design:** All puzzles are guaranteed solvable because:
1. Start with solved state (all nodes = target color)
2. Perform N random taps (shuffle moves)
3. Present scrambled state to player
4. Player must reverse the scramble

This approach:
- ✅ Guarantees solvability
- ✅ Enables infinite procedural generation
- ✅ No pre-made puzzle database needed
- ✅ Difficulty controlled by shuffle count

### Shuffle Optimization
To avoid trivial puzzles:
- Never tap the same node twice in a row (cancels out)
- Track last 2 taps to prevent immediate reversals
- Ensures puzzles use full complexity

## Difficulty Progression (5 Tiers)

### Beginner (Levels 1-5)
- **Nodes:** 3
- **Colors:** 2 (Blue/Red)
- **Shuffles:** 2
- **Time per puzzle:** ~10 seconds
- **Total time:** 40 seconds
- **Score:** 100 per solve
- **Penalty:** 0
- **Topology:** Line

### Easy (Levels 6-12)
- **Nodes:** 4
- **Colors:** 2
- **Shuffles:** 4
- **Time per puzzle:** ~8 seconds
- **Total time:** 45 seconds
- **Score:** 125 per solve
- **Penalty:** -25
- **Topology:** Triangle

### Medium (Levels 13-30)
- **Nodes:** 5
- **Colors:** 3 (Blue/Red/Green)
- **Shuffles:** 6
- **Time per puzzle:** ~7 seconds
- **Total time:** 50 seconds
- **Score:** 150 per solve
- **Penalty:** -50
- **Topology:** Square

### Advanced (Levels 31-45)
- **Nodes:** 7
- **Colors:** 3
- **Shuffles:** 10
- **Time per puzzle:** ~6 seconds
- **Total time:** 55 seconds
- **Score:** 200 per solve
- **Penalty:** -75
- **Topology:** Pentagon

### Hard (Levels 46-60)
- **Nodes:** 9
- **Colors:** 4 (Blue/Red/Green/Yellow)
- **Shuffles:** 15
- **Time per puzzle:** ~5 seconds
- **Total time:** 60 seconds
- **Score:** 250 per solve
- **Penalty:** -100
- **Topology:** Hexagon

## Scoring System

### Base Score
- Each solved puzzle awards base points (tier-dependent)
- Incorrect taps apply penalty (tier-dependent)
- Beginner has no penalties to reduce frustration

### Combo System
- Track consecutive puzzle solves
- After threshold reached (3-7 depending on tier):
  - Apply multiplier (1.5x - 2.5x)
- Combo resets on wrong action (future enhancement)
- Encourages consistent performance

### Expected Performance
- **Beginner:** ~5-8 puzzles per round
- **Easy:** ~4-6 puzzles per round
- **Medium:** ~3-5 puzzles per round
- **Advanced:** ~2-4 puzzles per round
- **Hard:** ~2-3 puzzles per round

## Technical Implementation

### Architecture
Follows Kiqqi Framework 5-script pattern:
1. **KiqqiNeuronGraphManager** - Game logic
2. **KiqqiNeuronGraphLevelManager** - Difficulty config
3. **KiqqiNeuronGraphView** - UI presentation
4. **KiqqiNeuronGraphTutorialManager** - (Phase 2)
5. **KiqqiNeuronGraphTutorialView** - (Phase 2)

### Key Classes

#### NeuronNode
```csharp
- int id
- NodeColor color
- GameObject instance
- Image nodeImage
- Button button
- Vector2 position
- List<int> neighbors
- bool isActive
```

#### NeuronGraphDifficultyConfig
```csharp
- int nodeCount
- int colorCount
- int shuffleMoves
- float timeLimit
- int solveScore
- int wrongPenalty
- int comboThreshold
- float comboMultiplier
- NetworkTopology topology
```

### Main Game Loop
1. `StartMiniGame()` - Initialize session
2. `OnCountdownFinished()` - Start generating puzzles
3. `GenerateNewPuzzle()`:
   - Create network topology
   - Set all to target color
   - Shuffle N times
   - Update visuals
4. `OnNodeClicked()` - Toggle colors
5. `CheckPuzzleSolved()` - Win condition
6. `OnPuzzleSolved()` - Award score, generate next
7. `OnTimeUp()` - End session

### Puzzle Generation Flow
```
CreateNetworkTopology()
  ↓
SetAllNodesToTargetColor()
  ↓
ShuffleNetwork(N times)
  ↓
UpdateVisuals()
  ↓
Present to Player
```

## Design Considerations

### Why Puzzle Rush Format?
- **Replayability:** Never the same puzzle twice
- **Engagement:** Constant flow, no downtime
- **Progression:** Clear skill improvement over time
- **Fits Framework:** Time-based scoring model

### Why Reverse Engineering?
- **Mathematical Guarantee:** All puzzles solvable
- **Infinite Content:** No manual level design needed
- **Scalable Difficulty:** Simply increase shuffle count
- **Implementation Speed:** Single algorithm vs. database

### Why 5 Difficulty Tiers?
- **Micro-Progression:** Frequent sense of advancement
- **Fine-Tuned Challenge:** Better difficulty curve
- **Framework Standard:** Consistent across all Kiqqi games
- **Player Retention:** More goals to achieve

## Future Enhancements

### Phase 1 (Current - Main Game)
- ✅ Core mechanics
- ✅ Puzzle generation
- ✅ Difficulty tiers
- ⏳ Art and visual polish
- ⏳ Sound design
- ⏳ Particle effects
- ⏳ Animations

### Phase 2 (Tutorial)
- Tutorial manager/view
- Step-by-step instructions
- Skip functionality
- First-launch detection

### Phase 3 (Desktop Port)
- 4:3 aspect ratio layout
- Desktop-optimized UI
- Mouse interaction polish

### Phase 4 (Localization)
- Multi-language support via KiqqiMl
- Only after game is 100% complete

### Potential Additions (Post-Launch)
- Different graph types (Random, Ladder, Mesh)
- Visual themes (Neurons, Circuit Boards, Constellations)
- Power-ups (Hint, Skip, Time Extend)
- Daily challenges
- Leaderboards integration

## Cognitive Benefits

This game trains:
- **Planning:** Thinking ahead multiple moves
- **Spatial Reasoning:** Understanding network connections
- **Working Memory:** Tracking node states and sequences
- **Visual Perception:** Pattern recognition in color states
- **Problem Solving:** Finding optimal solution paths

## Development Notes

### Naming Conventions
- Prefix: `ng` (Neuron Graph)
- GameObjects: `ngNodePrefab`, `ngNodesContainer`
- Scripts: `KiqqiNeuronGraph*`

### Testing Checklist
- [ ] Nodes spawn correctly for all topologies
- [ ] Color toggle works (node + neighbors)
- [ ] Puzzle solves when all colors match
- [ ] New puzzle generates after solve
- [ ] Score increases correctly
- [ ] Combo multiplier activates
- [ ] Timer counts down
- [ ] Game ends at time expiry
- [ ] All 5 difficulty tiers work
- [ ] Pause/Resume functionality
- [ ] Results screen shows correct score
- [ ] Play Again continues progression

### Known Limitations (Prototype)
- Basic visuals (no neuron theme yet)
- No animations/transitions
- Simple color palette
- No particle effects
- No sound variety

## References

Inspiration: CogniFit's "Synaptix" game
Similar games: Lights Out, Brain Dots, Flow Free
Framework: Kiqqi Framework v1.0
Unity Version: Unity 6 (6000.17)
