using UnityEngine;

namespace Kiqqi.Framework
{
    public class RivalOrbVisuals : MonoBehaviour
    {
        [Header("Rotation")]
        [SerializeField] private float rotationSpeed = 100f;
        [SerializeField] private bool randomizeInitialRotation = true;

        [Header("Optional Scale Pulse")]
        [SerializeField] private bool enablePulse = false;
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseAmount = 0.1f;

        private RectTransform rectTransform;
        private Vector3 baseScale;
        private float pulseTimer = 0f;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            baseScale = rectTransform.localScale;

            if (randomizeInitialRotation)
            {
                rectTransform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            }
        }

        private void Update()
        {
            if (rectTransform == null) return;

            rectTransform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);

            if (enablePulse)
            {
                pulseTimer += Time.deltaTime * pulseSpeed;
                float scale = 1f + Mathf.Sin(pulseTimer) * pulseAmount;
                rectTransform.localScale = baseScale * scale;
            }
        }
    }
}
