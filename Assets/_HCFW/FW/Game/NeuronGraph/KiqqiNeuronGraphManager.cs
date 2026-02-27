using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kiqqi.Framework
{
    public enum NodeColor
    {
        Blue,
        Red,
        Green,
        Yellow
    }

    public class NeuronNode
    {
        public int id;
        public NodeColor color;
        public GameObject instance;
        public UnityEngine.UI.Image nodeImage;
        public UnityEngine.UI.Button button;
        public Vector2 position;
        public List<int> neighbors = new();
        public bool isActive;
    }

    public class KiqqiNeuronGraphManager : KiqqiMiniGameManagerBase
    {
        #region INSPECTOR CONFIGURATION

        [Header("Core References")]
        [SerializeField] private KiqqiNeuronGraphLevelManager levelLogic;

        [Header("Connection Settings")]
        [SerializeField] private UnityEngine.UI.Image connectionLinePrefab;
        [SerializeField] private float connectionLineWidth = 4f;
        [SerializeField] private Color connectionColor = new Color(0.3f, 0.6f, 0.8f, 0.5f);
        [SerializeField] private Color connectionFlashColor = new Color(1f, 1f, 1f, 0.9f);
        [SerializeField] private float connectionFlashDuration = 0.2f;

        [Header("Colors")]
        [SerializeField] private Color blueColor = new Color(0.2f, 0.6f, 1f);
        [SerializeField] private Color redColor = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private Color greenColor = new Color(0.3f, 1f, 0.4f);
        [SerializeField] private Color yellowColor = new Color(1f, 0.9f, 0.2f);

        #endregion

        #region RUNTIME STATE

        private bool sessionRunning = false;
        private NeuronGraphDifficultyConfig currentConfig;
        private NodeColor targetColor = NodeColor.Blue;

        protected KiqqiNeuronGraphView view;
        protected KiqqiInputController input;

        private List<NeuronNode> nodes = new();
        private List<UnityEngine.UI.Image> connectionLines = new();
        private Dictionary<string, UnityEngine.UI.Image> connectionMap = new();
        private int currentComboStreak = 0;
        private int puzzlesSolved = 0;

        private KiqqiNeuronGraphLayoutData currentLayoutInstance;

        #endregion

        #region INITIALIZATION

        public override System.Type GetAssociatedViewType()
        {
            return typeof(KiqqiNeuronGraphView);
        }

        public override void Initialize(KiqqiAppManager context)
        {
            base.Initialize(context);
            input = context.Input;
        }

        #endregion

        #region GAME LIFECYCLE

        public override void StartMiniGame()
        {
            Debug.Log("[NeuronGraph] StartMiniGame() called - BEGIN");
            base.StartMiniGame();

            view = app.UI.GetView<KiqqiNeuronGraphView>();
            if (view == null)
            {
                Debug.LogError("[NeuronGraph] View not found!");
                return;
            }

            sessionRunning = false;
            sessionScore = 0;
            currentComboStreak = 0;
            puzzlesSolved = 0;

            if (levelLogic == null)
            {
                Debug.LogError("[NeuronGraph] LevelLogic is null! Make sure it's assigned in Inspector.");
                return;
            }

            int currentLevel = app.Levels.currentLevel;
            currentConfig = levelLogic.GetDifficultyConfig(currentLevel);

            Debug.Log($"[NeuronGraph] StartMiniGame() - Assigned currentConfig: Nodes={currentConfig.nodeCount}, Colors={currentConfig.colorCount}, Shuffles={currentConfig.shuffleMoves}");
        }

        public void OnCountdownFinished()
        {
            sessionRunning = true;
            GenerateNewPuzzle();
        }

        public override void TickMiniGame()
        {
            if (!sessionRunning || !isActive) return;
        }

        public void OnTimeUp()
        {
            sessionRunning = false;
            ClearPuzzle();
            CompleteMiniGame(sessionScore, sessionScore > 0);
        }

        public override void ResetMiniGame()
        {
            base.ResetMiniGame();
            ClearPuzzle();
            sessionRunning = false;
            currentComboStreak = 0;
            puzzlesSolved = 0;
        }

        #endregion

        #region PUZZLE GENERATION

        private void GenerateNewPuzzle()
        {
            ClearPuzzle();

            if (levelLogic != null)
            {
                int currentLevel = app.Levels.currentLevel;
                currentConfig = levelLogic.GetDifficultyConfig(currentLevel);
                Debug.Log($"[NeuronGraph] Refreshed config for Level {currentLevel} - Nodes: {currentConfig.nodeCount}, Colors: {currentConfig.colorCount}");
            }

            targetColor = (NodeColor)Random.Range(0, currentConfig.colorCount);

            Debug.Log($"[NeuronGraph] GenerateNewPuzzle - Config nodeCount: {currentConfig.nodeCount}, colorCount: {currentConfig.colorCount}, shuffles: {currentConfig.shuffleMoves}");

            LoadLayoutFromPrefab();
            SetAllNodesToTargetColor();
            ShuffleNetwork();
            UpdateVisuals();

            Debug.Log($"[NeuronGraph] Generated puzzle - Target: {targetColor}, Nodes: {nodes.Count}");
        }

        private void LoadLayoutFromPrefab()
        {
            ClearCurrentLayout();

            KiqqiNeuronGraphLayoutData layoutSource = currentConfig.selectedLayout;

            if (layoutSource == null)
            {
                Debug.LogError($"[NeuronGraph] No layout assigned in config for {currentConfig.nodeCount} nodes! Check Level Manager layout arrays.");
                return;
            }

            if (layoutSource.gameObject.scene.IsValid())
            {
                currentLayoutInstance = layoutSource;
                currentLayoutInstance.gameObject.SetActive(true);
                Debug.Log($"[LoadLayout] Using scene-based layout (enabled): {currentLayoutInstance.gameObject.name}");
            }
            else
            {
                currentLayoutInstance = Instantiate(layoutSource, view.transform);
                currentLayoutInstance.gameObject.SetActive(true);
                Debug.Log($"[LoadLayout] Instantiated layout from prefab: {currentLayoutInstance.gameObject.name}");
            }

            if (!currentLayoutInstance.ValidateLayout())
            {
                Debug.LogError($"[NeuronGraph] Layout validation failed for {currentConfig.nodeCount} nodes!");
                return;
            }

            BuildNodesFromLayout();
            BuildConnectionsFromLayout();
            DrawConnections();

            Debug.Log($"[LoadLayout] Loaded layout with {nodes.Count} nodes and {connectionLines.Count} connections");
        }

        private void BuildNodesFromLayout()
        {
            nodes.Clear();

            List<KiqqiNeuronNodeData> layoutNodes = currentLayoutInstance.GetAllNodes();

            for (int i = 0; i < layoutNodes.Count; i++)
            {
                KiqqiNeuronNodeData nodeData = layoutNodes[i];
                nodeData.NodeId = i;

                NeuronNode node = new NeuronNode
                {
                    id = i,
                    color = targetColor,
                    isActive = true,
                    instance = nodeData.gameObject,
                    nodeImage = nodeData.GetComponent<UnityEngine.UI.Image>(),
                    button = nodeData.GetComponent<UnityEngine.UI.Button>(),
                    neighbors = new List<int>()
                };

                RectTransform rect = nodeData.GetComponent<RectTransform>();
                if (rect != null)
                {
                    node.position = rect.anchoredPosition;
                }

                int nodeId = i;
                if (node.button != null)
                {
                    node.button.onClick.RemoveAllListeners();
                    node.button.onClick.AddListener(() => OnNodeClicked(nodeId));
                }

                nodes.Add(node);
            }

            Debug.Log($"[BuildNodes] Created {nodes.Count} nodes from layout");
        }

        private void BuildConnectionsFromLayout()
        {
            List<KiqqiNeuronNodeData> layoutNodes = currentLayoutInstance.GetAllNodes();

            for (int i = 0; i < layoutNodes.Count; i++)
            {
                NeuronNode node = nodes[i];
                KiqqiNeuronNodeData nodeData = layoutNodes[i];

                foreach (KiqqiNeuronNodeData neighborData in nodeData.neighbors)
                {
                    if (neighborData == null) continue;

                    int neighborId = neighborData.NodeId;

                    if (neighborId >= 0 && neighborId < nodes.Count)
                    {
                        if (!node.neighbors.Contains(neighborId))
                        {
                            node.neighbors.Add(neighborId);
                        }
                    }
                }
            }

            Debug.Log("[BuildConnections] Built neighbor lists from layout data");
            for (int i = 0; i < nodes.Count; i++)
            {
                Debug.Log($"  Node {i}: neighbors = [{string.Join(", ", nodes[i].neighbors)}]");
            }
        }

        private void ClearCurrentLayout()
        {
            if (currentLayoutInstance != null)
            {
                if (currentLayoutInstance.gameObject.scene.IsValid())
                {
                    currentLayoutInstance.gameObject.SetActive(false);
                    Debug.Log($"[ClearLayout] Disabled scene layout: {currentLayoutInstance.gameObject.name}");
                }
                else
                {
                    Destroy(currentLayoutInstance.gameObject);
                    Debug.Log($"[ClearLayout] Destroyed instantiated layout");
                }
                currentLayoutInstance = null;
            }

            nodes.Clear();
        }

        #endregion

        #region DRAWING

        private void DrawConnections()
        {
            if (currentLayoutInstance == null || currentLayoutInstance.connectionsContainer == null)
            {
                Debug.LogError("[DrawConnections] Layout instance or connections container is null!");
                return;
            }

            Transform connectionsParent = currentLayoutInstance.connectionsContainer;

            foreach (var line in connectionLines)
            {
                if (line != null)
                    Destroy(line.gameObject);
            }
            connectionLines.Clear();
            connectionMap.Clear();

            HashSet<string> drawnConnections = new();

            Debug.Log($"[DrawConnections] Starting to draw connections for {nodes.Count} nodes");

            foreach (var node in nodes)
            {
                foreach (int neighborId in node.neighbors)
                {
                    string connectionKey = GetConnectionKey(node.id, neighborId);
                    if (drawnConnections.Contains(connectionKey))
                        continue;

                    Debug.Log($"  Drawing line: {node.id} → {neighborId} (key: {connectionKey})");

                    UnityEngine.UI.Image line = Instantiate(connectionLinePrefab, connectionsParent);
                    line.gameObject.SetActive(true);
                    line.gameObject.name = $"Line_{node.id}_{neighborId}";

                    RectTransform lineRect = line.rectTransform;
                    RectTransform startRect = nodes[node.id].instance.GetComponent<RectTransform>();
                    RectTransform endRect = nodes[neighborId].instance.GetComponent<RectTransform>();

                    Vector2 startPos = startRect.anchoredPosition;
                    Vector2 endPos = endRect.anchoredPosition;

                    Vector2 direction = endPos - startPos;
                    float distance = direction.magnitude;
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

                    lineRect.anchoredPosition = startPos;
                    lineRect.sizeDelta = new Vector2(distance, connectionLineWidth);
                    lineRect.localRotation = Quaternion.Euler(0, 0, angle);
                    lineRect.pivot = new Vector2(0, 0.5f);

                    line.color = connectionColor;
                    line.raycastTarget = false;

                    Debug.Log($"    Line pos: {startPos} → {endPos}, distance: {distance:F1}, angle: {angle:F1}°, color: {line.color}");

                    connectionLines.Add(line);
                    connectionMap[connectionKey] = line;
                    drawnConnections.Add(connectionKey);
                }
            }
            
            Debug.Log($"[DrawConnections] Drew {connectionLines.Count} total lines");
        }

        private string GetConnectionKey(int a, int b)
        {
            int min = Mathf.Min(a, b);
            int max = Mathf.Max(a, b);
            return $"{min}-{max}";
        }

        #endregion

        #region PUZZLE SOLVING

        private void SetAllNodesToTargetColor()
        {
            foreach (var node in nodes)
            {
                node.color = targetColor;
            }
        }

        private void ShuffleNetwork()
        {
            int lastNodeTapped = -1;
            int secondLastNodeTapped = -1;

            for (int i = 0; i < currentConfig.shuffleMoves; i++)
            {
                int randomNodeId = Random.Range(0, nodes.Count);

                if (randomNodeId == lastNodeTapped || randomNodeId == secondLastNodeTapped)
                {
                    randomNodeId = (randomNodeId + 1) % nodes.Count;
                }

                ToggleNodeAndNeighbors(randomNodeId);

                secondLastNodeTapped = lastNodeTapped;
                lastNodeTapped = randomNodeId;
            }
        }

        private void FlashConnectedLines(int nodeId)
        {
            if (nodeId < 0 || nodeId >= nodes.Count) return;

            foreach (int neighborId in nodes[nodeId].neighbors)
            {
                string connectionKey = GetConnectionKey(nodeId, neighborId);
                if (connectionMap.TryGetValue(connectionKey, out UnityEngine.UI.Image line))
                {
                    StartCoroutine(FlashLineCoroutine(line));
                }
            }
        }

        private IEnumerator FlashLineCoroutine(UnityEngine.UI.Image line)
        {
            if (line == null) yield break;

            Color originalColor = line.color;
            line.color = connectionFlashColor;

            yield return new WaitForSeconds(connectionFlashDuration);

            if (line != null)
            {
                line.color = originalColor;
            }
        }

        private void OnNodeClicked(int nodeId)
        {
            if (!sessionRunning) return;

            FlashConnectedLines(nodeId);
            ToggleNodeAndNeighbors(nodeId);
            UpdateVisuals();

            KiqqiAppManager.Instance.Audio.PlaySfx("tap");

            if (CheckPuzzleSolved())
            {
                OnPuzzleSolved();
            }
        }

        private void ToggleNodeAndNeighbors(int nodeId)
        {
            if (nodeId < 0 || nodeId >= nodes.Count) return;

            ToggleNodeColor(nodes[nodeId]);

            foreach (int neighborId in nodes[nodeId].neighbors)
            {
                ToggleNodeColor(nodes[neighborId]);
            }
        }

        private void ToggleNodeColor(NeuronNode node)
        {
            int currentColorIndex = (int)node.color;
            currentColorIndex = (currentColorIndex + 1) % currentConfig.colorCount;
            node.color = (NodeColor)currentColorIndex;
        }

        private bool CheckPuzzleSolved()
        {
            foreach (var node in nodes)
            {
                if (node.color != targetColor)
                    return false;
            }
            return true;
        }

        private void OnPuzzleSolved()
        {
            currentComboStreak++;
            puzzlesSolved++;

            int scoreEarned = currentConfig.solveScore;

            if (currentComboStreak >= currentConfig.comboThreshold)
            {
                scoreEarned = Mathf.RoundToInt(scoreEarned * currentConfig.comboMultiplier);
            }

            sessionScore += scoreEarned;

            if (view != null)
            {
                view.AddScore(scoreEarned);
                view.ShowFeedback(true);
            }

            KiqqiAppManager.Instance.Audio.PlaySfx("answercorrect");

            app.Levels.NextLevel();

            Debug.Log($"[NeuronGraph] Puzzle solved! Score: +{scoreEarned}, Combo: {currentComboStreak}, Total: {sessionScore}, Next Level: {app.Levels.currentLevel}");

            StartCoroutine(GenerateNextPuzzleAfterDelay());
        }

        private IEnumerator GenerateNextPuzzleAfterDelay()
        {
            yield return new WaitForSeconds(0.5f);

            if (sessionRunning && view != null && view.RemainingTime > 2f)
            {
                GenerateNewPuzzle();
            }
        }

        #endregion

        #region VISUALS

        private void UpdateVisuals()
        {
            foreach (var node in nodes)
            {
                if (node.nodeImage != null)
                {
                    node.nodeImage.color = GetColorForNode(node.color);
                }
            }
        }

        private Color GetColorForNode(NodeColor nodeColor)
        {
            return nodeColor switch
            {
                NodeColor.Blue => blueColor,
                NodeColor.Red => redColor,
                NodeColor.Green => greenColor,
                NodeColor.Yellow => yellowColor,
                _ => blueColor
            };
        }

        private void ClearPuzzle()
        {
            foreach (var line in connectionLines)
            {
                if (line != null)
                    Destroy(line.gameObject);
            }
            connectionLines.Clear();
            connectionMap.Clear();

            ClearCurrentLayout();
        }

        #endregion
    }
}
