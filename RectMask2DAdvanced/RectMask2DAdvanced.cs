using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEngine.UI
{
    [AddComponentMenu("UI (Canvas)/Rect Mask 2D Advanced", 15)]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    /// <summary>
    /// An advanced 2D rectangular mask that supports independent softness control for each edge.
    /// </summary>
    /// <remarks>
    /// Extends RectMask2D functionality with per-edge softness:
    /// - Supports Vector4 edgeSoftness parameter (Left, Bottom, Right, Top)
    /// - Automatically applies per-edge softness to all child UI elements
    /// - Uses AdvancedSoftnessRenderer internally for true per-edge control
    /// - Maintains all RectMask2D benefits (no stencil buffer, fewer draw calls)
    ///
    /// Usage: Add this component to a GameObject and set edgeSoftness.
    /// All child UI elements will automatically receive per-edge softness.
    /// </remarks>
    public class RectMask2DAdvanced : RectMask2D
    {
        [SerializeField]
        private Vector4 m_EdgeSoftness = new Vector4();

        /// <summary>
        /// The softness to apply to each edge independently.
        /// X = Left, Y = Bottom, Z = Right, W = Top
        /// Each value represents the number of pixels for the softness falloff.
        /// </summary>
        public Vector4 edgeSoftness
        {
            get { return m_EdgeSoftness; }
            set
            {
                m_EdgeSoftness.x = Mathf.Max(0, value.x);
                m_EdgeSoftness.y = Mathf.Max(0, value.y);
                m_EdgeSoftness.z = Mathf.Max(0, value.z);
                m_EdgeSoftness.w = Mathf.Max(0, value.w);
                UpdateChildSoftness();
                MaskUtilities.Notify2DMaskStateChanged(this);
            }
        }

        private Dictionary<GameObject, AdvancedSoftnessRenderer> m_ChildRenderers =
            new Dictionary<GameObject, AdvancedSoftnessRenderer>();

        protected override void OnEnable()
        {
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            // Don't allow negative softness.
            m_EdgeSoftness.x = Mathf.Max(0, m_EdgeSoftness.x);
            m_EdgeSoftness.y = Mathf.Max(0, m_EdgeSoftness.y);
            m_EdgeSoftness.z = Mathf.Max(0, m_EdgeSoftness.z);
            m_EdgeSoftness.w = Mathf.Max(0, m_EdgeSoftness.w);

            if (!IsActive())
                return;

            UpdateChildSoftness();
            MaskUtilities.Notify2DMaskStateChanged(this);
        }
#endif

        /// <summary>
        /// Override UpdateClipSoftness to apply per-edge softness to all child elements.
        /// This method is called automatically by the clipping system.
        /// </summary>
        public override void UpdateClipSoftness()
        {
            base.UpdateClipSoftness();
            UpdateChildSoftness();
        }

        private void UpdateChildSoftness()
        {
            // Skip if there's no softness to apply
            if (m_EdgeSoftness == Vector4.zero)
            {
                return;
            }

            // Only process if we're active
            if (!isActiveAndEnabled)
            {
                return;
            }

            // Get all Graphic children (UI elements that can be masked)
            var graphics = new List<Graphic>();
            GetComponentsInChildren(false, graphics);

            // Update existing renderers and add new ones
            var newRenderers = new Dictionary<GameObject, AdvancedSoftnessRenderer>();

            foreach (var graphic in graphics)
            {
                GameObject childObj = graphic.gameObject;

                // Skip if this is the mask itself
                if (childObj == gameObject)
                    continue;

                // Skip if it already has an AdvancedSoftnessRenderer that wasn't created by us
                AdvancedSoftnessRenderer existingRenderer = childObj.GetComponent<AdvancedSoftnessRenderer>();
                if (existingRenderer != null && !m_ChildRenderers.ContainsKey(childObj))
                {
                    continue; // Don't manage renderers created by user
                }

                // Get or create renderer
                AdvancedSoftnessRenderer renderer;
                if (m_ChildRenderers.TryGetValue(childObj, out renderer))
                {
                    // Update existing renderer
                    if (renderer != null)
                    {
                        renderer.edgeSoftness = m_EdgeSoftness;
                        newRenderers[childObj] = renderer;
                    }
                }
                else
                {
                    // Create new renderer
                    renderer = childObj.GetComponent<AdvancedSoftnessRenderer>();
                    if (renderer == null)
                    {
                        renderer = childObj.AddComponent<AdvancedSoftnessRenderer>();
                    }
                    renderer.edgeSoftness = m_EdgeSoftness;
                    newRenderers[childObj] = renderer;
                }
            }

            // Clean up renderers for destroyed children
            var toRemove = new List<GameObject>();
            foreach (var kvp in m_ChildRenderers)
            {
                if (!newRenderers.ContainsKey(kvp.Key) || kvp.Value == null)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var key in toRemove)
            {
                m_ChildRenderers.Remove(key);
            }

            m_ChildRenderers = newRenderers;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            m_ChildRenderers.Clear();
        }

        /// <summary>
        /// Helper method to set all edge softness at once
        /// </summary>
        public void SetAllSoftness(float value)
        {
            edgeSoftness = new Vector4(value, value, value, value);
        }

        /// <summary>
        /// Helper method to set horizontal edge softness (left and right)
        /// </summary>
        public void SetHorizontalSoftness(float value)
        {
            edgeSoftness = new Vector4(value, edgeSoftness.y, value, edgeSoftness.w);
        }

        /// <summary>
        /// Helper method to set vertical edge softness (top and bottom)
        /// </summary>
        public void SetVerticalSoftness(float value)
        {
            edgeSoftness = new Vector4(edgeSoftness.x, value, edgeSoftness.z, value);
        }
    }
}
