using UnityEngine;
using System.Collections.Generic;

namespace Kiqqi.Framework
{
    public class KiqqiNeuronNodeData : MonoBehaviour
    {
        [Header("Node Connections")]
        [Tooltip("Drag neighbor nodes here to define connections")]
        public List<KiqqiNeuronNodeData> neighbors = new List<KiqqiNeuronNodeData>();

        [Header("Runtime Data (Auto-Set)")]
        [Tooltip("Node ID assigned at runtime")]
        [SerializeField] private int runtimeNodeId = -1;

        public int NodeId 
        { 
            get => runtimeNodeId; 
            set => runtimeNodeId = value; 
        }

        public int GetNeighborCount()
        {
            return neighbors.Count;
        }

        public bool HasNeighbor(KiqqiNeuronNodeData other)
        {
            return neighbors.Contains(other);
        }

        private void OnDrawGizmos()
        {
            if (neighbors == null || neighbors.Count == 0) return;

            Gizmos.color = new Color(0.3f, 0.6f, 0.8f, 0.5f);

            foreach (var neighbor in neighbors)
            {
                if (neighbor != null)
                {
                    Vector3 start = transform.position;
                    Vector3 end = neighbor.transform.position;
                    Gizmos.DrawLine(start, end);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (neighbors == null || neighbors.Count == 0) return;

            Gizmos.color = new Color(1f, 1f, 1f, 0.8f);

            foreach (var neighbor in neighbors)
            {
                if (neighbor != null)
                {
                    Vector3 start = transform.position;
                    Vector3 end = neighbor.transform.position;
                    Gizmos.DrawLine(start, end);
                    
                    Vector3 midPoint = (start + end) * 0.5f;
                    Gizmos.DrawSphere(midPoint, 5f);
                }
            }
        }
    }
}
