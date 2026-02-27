using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Kiqqi.Framework
{
    public class Jar
    {
        public int id;
        public List<int> items;
        public int capacity;

        public bool IsEmpty => items.Count == 0;
        public bool IsFull => items.Count >= capacity;
        public bool IsSolved => items.Count > 0 && items.Distinct().Count() == 1 && items.Count == capacity;
        public int TopItem => items.Count > 0 ? items[items.Count - 1] : -1;

        public Jar(int id, int capacity)
        {
            this.id = id;
            this.capacity = capacity;
            this.items = new List<int>();
        }

        public void AddItem(int itemType)
        {
            if (!IsFull)
            {
                items.Add(itemType);
            }
        }

        public int RemoveTopItem()
        {
            if (IsEmpty) return -1;

            int topItem = items[items.Count - 1];
            items.RemoveAt(items.Count - 1);
            return topItem;
        }

        public Jar Clone()
        {
            Jar clone = new Jar(id, capacity);
            clone.items = new List<int>(items);
            return clone;
        }
    }

    public class KiqqiJarSortingManager : KiqqiMiniGameManagerBase
    {
        #region INSPECTOR

        [Header("Core References")]
        [SerializeField] private KiqqiJarSortingLevelManager levelLogic;

        [Header("Game Settings")]
        [SerializeField] private Color[] itemColors;

        [Header("Visual Settings")]
        [SerializeField] private Vector2 itemSize = new Vector2(128, 128);
        [SerializeField] private float itemSpacing = 10f;

        #endregion

        #region STATE

        private JarSortingDifficultyConfig currentConfig;
        private List<Jar> jars;
        private int selectedJarIndex = -1;
        private bool sessionRunning = false;
        private int moveCount = 0;

        protected KiqqiJarSortingView view;

        #endregion

        #region INITIALIZATION

        public override System.Type GetAssociatedViewType() => typeof(KiqqiJarSortingView);

        public override void Initialize(KiqqiAppManager context)
        {
            base.Initialize(context);

            view = context.UI.GetView<KiqqiJarSortingView>();

            if (view != null)
            {
                view.SetManager(this);
            }

            Debug.Log("[KiqqiJarSortingManager] Initialized.");
        }

        #endregion

        #region GAMEPLAY LIFECYCLE

        public override void StartMiniGame()
        {
            base.StartMiniGame();

            if (view == null)
            {
                Debug.LogError("[JarSorting] View reference is null!");
                return;
            }

            if (levelLogic == null)
            {
                Debug.LogError("[JarSorting] LevelLogic reference is null! Assign it in the inspector.");
                return;
            }

            int currentLevel = app.Levels.currentLevel;
            Debug.Log($"[JarSorting] Current level from app.Levels: {currentLevel}");

            currentConfig = levelLogic.GetDifficultyConfig(currentLevel);
            
            int totalJars = currentConfig.objectTypeCount + currentConfig.emptyJarCount;
            Debug.Log($"[JarSorting] Config loaded - {currentConfig.objectTypeCount} types, {currentConfig.emptyJarCount} empty, {totalJars} total jars, {currentConfig.itemsPerJar} items/jar");

            moveCount = 0;
            sessionRunning = false;

            Debug.Log($"[JarSorting] StartMiniGame() - Level {currentLevel}");
        }

        public void OnCountdownFinished()
        {
            sessionRunning = true;
            GeneratePuzzle();
            view.UpdateJars(jars);
            view.UpdateMoves(moveCount);
        }

        public override void ResetMiniGame()
        {
            base.ResetMiniGame();

            moveCount = 0;
            selectedJarIndex = -1;
            sessionRunning = false;

            if (jars != null)
            {
                jars.Clear();
            }

            if (view != null)
            {
                view.ClearJars();
                view.ClearSelection();
            }

            Debug.Log("[KiqqiJarSortingManager] ResetMiniGame - state cleared");
        }

        public void ResumeFromPause(KiqqiJarSortingView v)
        {
            view = v ?? view;
            if (view.pauseButton) view.pauseButton.interactable = true;

            isActive = true;
            isComplete = false;
            sessionRunning = true;

            view?.UpdateScore(sessionScore);
            view?.UpdateMoves(moveCount);

            Debug.Log("[KiqqiJarSortingManager] Resumed from pause");
        }

        public override void OnMiniGameExit()
        {
            base.OnMiniGameExit();

            sessionRunning = false;
            isActive = false;
            isComplete = true;

            if (view != null)
            {
                view.ClearJars();
            }

            if (jars != null)
            {
                jars.Clear();
            }

            Debug.Log("[KiqqiJarSortingManager] OnMiniGameExit -> cleaned up.");
        }

        #endregion

        #region GAME LOOP

        public override void TickMiniGame()
        {
            if (!sessionRunning) return;

            if (view.RemainingTime <= 0f)
            {
                sessionRunning = false;
                EndSession();
            }
        }

        #endregion

        #region PUZZLE GENERATION

        private void GeneratePuzzle()
        {
            jars = new List<Jar>();

            int totalJars = currentConfig.objectTypeCount + currentConfig.emptyJarCount;

            for (int i = 0; i < totalJars; i++)
            {
                jars.Add(new Jar(i, currentConfig.itemsPerJar));
            }

            for (int typeId = 0; typeId < currentConfig.objectTypeCount; typeId++)
            {
                for (int count = 0; count < currentConfig.itemsPerJar; count++)
                {
                    jars[typeId].AddItem(typeId);
                }
            }

            Debug.Log($"[GeneratePuzzle] Created solved state: {currentConfig.objectTypeCount} jars with {currentConfig.itemsPerJar} items each, {currentConfig.emptyJarCount} empty jars, total {totalJars} jars");
            PrintJarState();

            ShufflePuzzle(currentConfig.shuffleMoves);

            Debug.Log($"[GeneratePuzzle] After shuffling with {currentConfig.shuffleMoves} moves:");
            PrintJarState();
        }

        private void ShufflePuzzle(int moveCount)
        {
            int attempts = 0;
            int maxAttempts = 10;

            while (attempts < maxAttempts)
            {
                List<Jar> backupJars = new List<Jar>();
                foreach (var jar in jars)
                {
                    backupJars.Add(jar.Clone());
                }

                int actualMoves = 0;

                for (int i = 0; i < moveCount; i++)
                {
                    List<int> validFromJars = new List<int>();
                    for (int j = 0; j < jars.Count; j++)
                    {
                        if (!jars[j].IsEmpty)
                        {
                            validFromJars.Add(j);
                        }
                    }

                    if (validFromJars.Count == 0) break;

                    int fromIndex = validFromJars[Random.Range(0, validFromJars.Count)];

                    List<int> validToJars = new List<int>();
                    for (int j = 0; j < jars.Count; j++)
                    {
                        if (j != fromIndex && !jars[j].IsFull)
                        {
                            validToJars.Add(j);
                        }
                    }

                    if (validToJars.Count == 0) continue;

                    int toIndex = validToJars[Random.Range(0, validToJars.Count)];

                    int item = jars[fromIndex].RemoveTopItem();
                    jars[toIndex].AddItem(item);

                    actualMoves++;
                }

                if (IsProperlyShuffled())
                {
                    Debug.Log($"[ShufflePuzzle] Valid shuffle found after {attempts + 1} attempts with {actualMoves} moves");
                    return;
                }

                jars.Clear();
                foreach (var backup in backupJars)
                {
                    jars.Add(backup.Clone());
                }

                attempts++;
            }

            Debug.LogWarning($"[ShufflePuzzle] Could not find valid shuffle after {maxAttempts} attempts, using last attempt");
        }

        private bool IsProperlyShuffled()
        {
            int solvedJars = 0;
            int mixedJars = 0;

            foreach (var jar in jars)
            {
                if (jar.IsEmpty) continue;

                if (jar.IsSolved)
                {
                    solvedJars++;
                }
                else if (jar.items.Distinct().Count() > 1)
                {
                    mixedJars++;
                }
            }

            bool hasEnoughMixing = mixedJars >= Mathf.Min(2, currentConfig.objectTypeCount);
            bool notTooManySolved = solvedJars < currentConfig.objectTypeCount;

            Debug.Log($"[IsProperlyShuffled] Mixed jars: {mixedJars}, Solved jars: {solvedJars}/{currentConfig.objectTypeCount}, Valid: {hasEnoughMixing && notTooManySolved}");

            return hasEnoughMixing && notTooManySolved;
        }

        private void PrintJarState()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("[JarState]");
            for (int i = 0; i < jars.Count; i++)
            {
                sb.Append($"  Jar {i}: ");
                if (jars[i].IsEmpty)
                {
                    sb.AppendLine("EMPTY");
                }
                else
                {
                    sb.AppendLine($"[{string.Join(", ", jars[i].items)}] (top={jars[i].TopItem})");
                }
            }
            Debug.Log(sb.ToString());
        }

        #endregion

        #region GAME LOGIC

        public void OnJarClicked(int jarIndex)
        {
            Debug.Log($"[OnJarClicked] Jar {jarIndex} clicked. SessionRunning: {sessionRunning}");
            
            if (!sessionRunning) return;

            if (selectedJarIndex == -1)
            {
                if (!jars[jarIndex].IsEmpty)
                {
                    selectedJarIndex = jarIndex;
                    view.ShowJarSelected(jarIndex);
                    Debug.Log($"[JarClicked] Selected jar {jarIndex}");
                }
                else
                {
                    Debug.Log($"[JarClicked] Jar {jarIndex} is empty, cannot select");
                }
            }
            else
            {
                if (jarIndex == selectedJarIndex)
                {
                    selectedJarIndex = -1;
                    view.ClearSelection();
                    Debug.Log($"[JarClicked] Deselected jar");
                }
                else
                {
                    if (CanPour(selectedJarIndex, jarIndex))
                    {
                        Debug.Log($"[JarClicked] Valid pour from {selectedJarIndex} to {jarIndex}");
                        TryPour(selectedJarIndex, jarIndex);
                    }
                    else
                    {
                        Debug.Log($"[JarClicked] Invalid pour from {selectedJarIndex} to {jarIndex} - ignoring click");
                    }
                }
            }
        }

        private void TryPour(int fromIndex, int toIndex)
        {
            int item = jars[fromIndex].RemoveTopItem();
            jars[toIndex].AddItem(item);

            moveCount++;
            sessionScore += currentConfig.moveScore;

            bool willWin = CheckWinCondition();

            view.AnimatePour(fromIndex, toIndex, item, () =>
            {
                view.UpdateScore(sessionScore);
                view.UpdateMoves(moveCount);

                if (willWin)
                {
                    sessionRunning = false;
                    OnPuzzleSolved();
                }
            });

            selectedJarIndex = -1;

            Debug.Log($"[Pour] Jar {fromIndex} → Jar {toIndex}, item type {item}");
        }

        private bool CanPour(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= jars.Count) return false;
            if (toIndex < 0 || toIndex >= jars.Count) return false;
            if (fromIndex == toIndex) return false;

            Jar fromJar = jars[fromIndex];
            Jar toJar = jars[toIndex];

            if (fromJar.IsEmpty) return false;
            if (toJar.IsFull) return false;

            if (toJar.IsEmpty) return true;

            return fromJar.TopItem == toJar.TopItem;
        }

        private bool CheckWinCondition()
        {
            int solvedCount = 0;
            int emptyCount = 0;

            foreach (var jar in jars)
            {
                if (jar.IsSolved)
                {
                    solvedCount++;
                }
                else if (jar.IsEmpty)
                {
                    emptyCount++;
                }
            }

            bool allSolved = (solvedCount == currentConfig.objectTypeCount) && (emptyCount == currentConfig.emptyJarCount);

            Debug.Log($"[CheckWin] Solved: {solvedCount}/{currentConfig.objectTypeCount}, Empty: {emptyCount}/{currentConfig.emptyJarCount}, Win: {allSolved}");

            return allSolved;
        }

        private void OnPuzzleSolved()
        {
            sessionScore += currentConfig.solveScore;
            view.UpdateScore(sessionScore);

            Debug.Log($"[PuzzleSolved] Score: {sessionScore}, Moves: {moveCount}");

            if (view.RemainingTime > 2f)
            {
                StartCoroutine(TransitionToNextPuzzle());
            }
            else
            {
                sessionRunning = false;
                EndSession();
            }
        }

        private IEnumerator TransitionToNextPuzzle()
        {
            view.ShowPuzzleComplete();
            
            yield return new WaitForSeconds(0.5f);
            
            yield return view.FadeOutJars();
            
            KiqqiAppManager.Instance.Levels.NextLevel();
            
            int newLevel = app.Levels.currentLevel;
            currentConfig = levelLogic.GetDifficultyConfig(newLevel);
            
            view.ClearJars();
            
            GeneratePuzzle();
            view.UpdateJars(jars);
            
            yield return view.FadeInJars();
            
            sessionRunning = true;
            
            Debug.Log($"[PuzzleSolved] Loaded next puzzle - Level {newLevel}");
        }

        #endregion

        #region SESSION END

        private void EndSession()
        {
            view.ClearJars();
            
            view.ShowResults(sessionScore, moveCount);

            CompleteMiniGame(sessionScore, true);

            Debug.Log($"[EndSession] Time up. Final score: {sessionScore}, Moves: {moveCount}");
        }

        #endregion

        #region HELPERS

        public Color GetItemColor(int itemType)
        {
            if (itemColors == null || itemColors.Length == 0)
            {
                return Color.white;
            }

            return itemColors[itemType % itemColors.Length];
        }

        public Vector2 GetItemSize() => itemSize;
        public float GetItemSpacing() => itemSpacing;

        #endregion
    }
}
