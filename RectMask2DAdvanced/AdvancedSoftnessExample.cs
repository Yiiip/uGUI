using UnityEngine;

namespace UnityEngine.UI
{
    /// <summary>
    /// Example component demonstrating how to use advanced softness with four-edge control.
    /// Add this to any UI element (Image, Text, etc.) to enable per-edge softness.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Graphic))]
    public class AdvancedSoftnessExample : MonoBehaviour
    {
        [Header("Edge Softness Settings")]
        [Tooltip("Softness for left edge in pixels")]
        [Range(0, 100)]
        public float leftSoftness = 0f;

        [Tooltip("Softness for bottom edge in pixels")]
        [Range(0, 100)]
        public float bottomSoftness = 0f;

        [Tooltip("Softness for right edge in pixels")]
        [Range(0, 100)]
        public float rightSoftness = 0f;

        [Tooltip("Softness for top edge in pixels")]
        [Range(0, 100)]
        public float topSoftness = 0f;

        [Space]

        [Header("Advanced Options")]
        [Tooltip("Enable/disable advanced softness")]
        public bool enableAdvancedSoftness = true;

        private AdvancedSoftnessRenderer m_SoftnessRenderer;
        private Vector4 m_LastEdgeSoftness;

        private void OnEnable()
        {
            EnsureSoftnessRenderer();
            UpdateSoftness();
        }

        private void OnDisable()
        {
            UpdateSoftness();
        }

        private void Update()
        {
            // Check if softness values changed
            Vector4 currentSoftness = new Vector4(leftSoftness, bottomSoftness, rightSoftness, topSoftness);
            if (currentSoftness != m_LastEdgeSoftness)
            {
                UpdateSoftness();
                m_LastEdgeSoftness = currentSoftness;
            }
        }

        private void EnsureSoftnessRenderer()
        {
            if (m_SoftnessRenderer == null)
            {
                m_SoftnessRenderer = GetComponent<AdvancedSoftnessRenderer>();
                if (m_SoftnessRenderer == null)
                {
                    m_SoftnessRenderer = gameObject.AddComponent<AdvancedSoftnessRenderer>();
                }
            }
        }

        private void UpdateSoftness()
        {
            EnsureSoftnessRenderer();

            if (m_SoftnessRenderer != null)
            {
                m_SoftnessRenderer.edgeSoftness = new Vector4(
                    leftSoftness,
                    bottomSoftness,
                    rightSoftness,
                    topSoftness
                );
                m_SoftnessRenderer.useAdvancedSoftness = enableAdvancedSoftness;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            UpdateSoftness();
            m_LastEdgeSoftness = new Vector4(leftSoftness, bottomSoftness, rightSoftness, topSoftness);
        }
#endif

        /// <summary>
        /// Helper method to set all edge softness at once
        /// </summary>
        public void SetAllSoftness(float value)
        {
            leftSoftness = value;
            bottomSoftness = value;
            rightSoftness = value;
            topSoftness = value;
            UpdateSoftness();
        }

        /// <summary>
        /// Helper method to set horizontal edge softness (left and right)
        /// </summary>
        public void SetHorizontalSoftness(float value)
        {
            leftSoftness = value;
            rightSoftness = value;
            UpdateSoftness();
        }

        /// <summary>
        /// Helper method to set vertical edge softness (top and bottom)
        /// </summary>
        public void SetVerticalSoftness(float value)
        {
            topSoftness = value;
            bottomSoftness = value;
            UpdateSoftness();
        }
    }
}
