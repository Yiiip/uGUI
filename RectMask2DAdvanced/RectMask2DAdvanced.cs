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
    /// - Automatically manages AdvancedSoftnessRenderer components on children (hidden from Inspector)
    ///
    /// Usage: Add this component to a GameObject and set edgeSoftness.
    /// All child UI elements will automatically receive per-edge softness.
    /// No need to manually add AdvancedSoftnessRenderer to children.
    /// </remarks>
    public class RectMask2DAdvanced : RectMask2D
    {
        [SerializeField]
        private Vector4 m_EdgeSoftness = new Vector4();

        [SerializeField]
        [Tooltip("Automatically manage AdvancedSoftnessRenderer components on child UI elements")]
        private bool m_AutoManageRenderers = true;

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

        /// <summary>
        /// Whether to automatically manage AdvancedSoftnessRenderer components on child UI elements.
        /// When enabled, AdvancedSoftnessRenderer components are automatically added and hidden.
        /// </summary>
        public bool autoManageRenderers
        {
            get { return m_AutoManageRenderers; }
            set
            {
                if (m_AutoManageRenderers != value)
                {
                    m_AutoManageRenderers = value;
                    if (value)
                    {
                        UpdateChildSoftness();
                    }
                    else
                    {
                        CleanupManagedRenderers();
                    }
                }
            }
        }

        private Dictionary<GameObject, AdvancedSoftnessRenderer> m_ChildRenderers =
            new Dictionary<GameObject, AdvancedSoftnessRenderer>();

        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateChildSoftness();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (!m_AutoManageRenderers)
            {
                CleanupManagedRenderers();
            }
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
            // Skip if auto management is disabled
            if (!m_AutoManageRenderers)
            {
                return;
            }

            // Only process if we're active
            if (!isActiveAndEnabled)
            {
                return;
            }

            // If softness is zero, clean up all managed renderers
            if (m_EdgeSoftness == Vector4.zero)
            {
                CleanupManagedRenderers();
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
                        // Hide the component from Inspector (only auto-managed components are hidden)
#if UNITY_EDITOR
                        renderer.hideFlags = HideFlags.HideInInspector;
#endif
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
                    // Remove the component we created
                    if (kvp.Value != null && kvp.Key != null)
                    {
                        UnityEngine.Object.DestroyImmediate(kvp.Value);
                    }
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var key in toRemove)
            {
                m_ChildRenderers.Remove(key);
            }

            m_ChildRenderers = newRenderers;
        }

        private void CleanupManagedRenderers()
        {
            // Remove all AdvancedSoftnessRenderer components that were auto-managed
            var toDestroy = new List<AdvancedSoftnessRenderer>();
            foreach (var kvp in m_ChildRenderers)
            {
                if (kvp.Value != null)
                {
                    toDestroy.Add(kvp.Value);
                }
            }

            // Destroy components outside the loop
            foreach (var renderer in toDestroy)
            {
                if (renderer != null)
                {
                    if (Application.isPlaying)
                        UnityEngine.Object.Destroy(renderer);
                    else
                        UnityEngine.Object.DestroyImmediate(renderer);
                }
            }

            m_ChildRenderers.Clear();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            CleanupManagedRenderers();
        }

        protected void OnTransformChildrenChanged()
        {
            UpdateChildSoftness();
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
