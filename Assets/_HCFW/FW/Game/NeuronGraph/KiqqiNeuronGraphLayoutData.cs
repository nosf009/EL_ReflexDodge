using UnityEngine;
using System.Collections.Generic;

namespace Kiqqi.Framework
{
    public class KiqqiNeuronGraphLayoutData : MonoBehaviour
    {
        [Header("Layout Configuration")]
        [Tooltip("Number of nodes in this layout (for validation)")]
        public int nodeCount = 4;
        
        [Tooltip("Variant ID for this layout (e.g., 1, 2, 3)")]
        public int variantId = 1;

        [Header("Container References")]
        [Tooltip("Parent transform containing all node GameObjects")]
        public Transform nodesContainer;
        
        [Tooltip("Parent transform where connection lines will be spawned")]
        public Transform connectionsContainer;

        [Header("Validation")]
        [Tooltip("Enable to show debug info in console")]
        public bool debugMode = false;

        public List<KiqqiNeuronNodeData> GetAllNodes()
        {
            if (nodesContainer == null)
            {
                Debug.LogError($"[LayoutData] nodesContainer is null on {gameObject.name}");
                return new List<KiqqiNeuronNodeData>();
            }

            List<KiqqiNeuronNodeData> nodes = new List<KiqqiNeuronNodeData>();
            
            foreach (Transform child in nodesContainer)
            {
                KiqqiNeuronNodeData nodeData = child.GetComponent<KiqqiNeuronNodeData>();
                if (nodeData != null)
                {
                    nodes.Add(nodeData);
                }
            }

            if (debugMode)
            {
                Debug.Log($"[LayoutData] Found {nodes.Count} nodes in {gameObject.name}");
            }

            if (nodes.Count != nodeCount)
            {
                Debug.LogWarning($"[LayoutData] Expected {nodeCount} nodes but found {nodes.Count} in {gameObject.name}");
            }

            return nodes;
        }

        public bool ValidateLayout()
        {
            List<KiqqiNeuronNodeData> nodes = GetAllNodes();
            
            if (nodes.Count == 0)
            {
                Debug.LogError($"[LayoutData] No nodes found in {gameObject.name}");
                return false;
            }

            bool isValid = true;

            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].neighbors.Count == 0)
                {
                    Debug.LogWarning($"[LayoutData] Node {i} ({nodes[i].gameObject.name}) has no neighbors!");
                    isValid = false;
                }

                foreach (var neighbor in nodes[i].neighbors)
                {
                    if (neighbor == null)
                    {
                        Debug.LogWarning($"[LayoutData] Node {i} has null neighbor reference!");
                        isValid = false;
                    }
                }
            }

            if (connectionsContainer == null)
            {
                Debug.LogError($"[LayoutData] connectionsContainer is null on {gameObject.name}");
                isValid = false;
            }

            if (isValid && debugMode)
            {
                Debug.Log($"[LayoutData] Layout {gameObject.name} is valid!");
            }

            return isValid;
        }

        private void OnValidate()
        {
            if (nodesContainer == null)
            {
                nodesContainer = transform.Find("ngNodesContainer");
            }

            if (connectionsContainer == null)
            {
                connectionsContainer = transform.Find("ngConnectionsContainer");
            }
        }
    }
}
