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

                // Request update to apply changes
                RequestUpdate();
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
                        RequestUpdate();
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

        private bool m_Dirty = false;

        protected override void OnEnable()
        {
            base.OnEnable();
            RequestUpdate();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (!m_AutoManageRenderers)
            {
                CleanupManagedRenderers();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            CleanupManagedRenderers();
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

            // Mark as dirty and schedule update
            m_Dirty = true;

            // Schedule delayed update in editor to avoid serialization issues
            if (!Application.isPlaying)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null)
                        PerformUpdate();
                };
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif

        /// <summary>
        /// Override UpdateClipSoftness to apply per-edge softness to all child elements.
        /// This method is called automatically by the clipping system.
        /// </summary>
        public override void UpdateClipSoftness()
        {
            base.UpdateClipSoftness();
            PerformUpdate();
        }

        private void RequestUpdate()
        {
            // In runtime mode, perform update immediately
            if (Application.isPlaying && isActiveAndEnabled)
            {
                PerformUpdate();
                return;
            }

            m_Dirty = true;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // In editor mode, schedule delayed update to avoid serialization issues
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null && m_Dirty)
                        PerformUpdate();
                };
            }
#endif
        }

#if UNITY_EDITOR
        private void Update()
        {
            // Backup: process any remaining dirty flags in editor mode
            if (m_Dirty && isActiveAndEnabled && !Application.isPlaying)
            {
                PerformUpdate();
            }
        }
#endif

        private void PerformUpdate()
        {
            m_Dirty = false;

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

            UpdateChildSoftness();
        }

        private void UpdateChildSoftness()
        {
            // Get all Graphic children (UI elements that can be masked)
            var graphics = new List<Graphic>();
            GetComponentsInChildren(false, graphics);

            // Update existing renderers and add new ones
            var newRenderers = new Dictionary<GameObject, AdvancedSoftnessRenderer>();
            var renderersToDestroy = new List<AdvancedSoftnessRenderer>();

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
                if (m_ChildRenderers.TryGetValue(childObj, out renderer) && renderer != null)
                {
                    // Update existing renderer
                    renderer.edgeSoftness = m_EdgeSoftness;
                    newRenderers[childObj] = renderer;
                }
                else
                {
                    // Create new renderer or use existing one
                    if (existingRenderer != null)
                    {
                        // Use the existing renderer (user created it but it wasn't tracked)
                        renderer = existingRenderer;
                    }
                    else
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

            // Find renderers to destroy
            foreach (var kvp in m_ChildRenderers)
            {
                if (!newRenderers.ContainsKey(kvp.Key) && kvp.Value != null)
                {
                    renderersToDestroy.Add(kvp.Value);
                }
            }

            // Update the dictionary first
            m_ChildRenderers = newRenderers;

            // Destroy components after updating dictionary to avoid serialization issues
            foreach (var renderer in renderersToDestroy)
            {
                if (renderer != null)
                {
                    if (Application.isPlaying)
                        UnityEngine.Object.Destroy(renderer);
                    else
                        UnityEngine.Object.DestroyImmediate(renderer);
                }
            }
        }

        private void CleanupManagedRenderers()
        {
            // Remove all AdvancedSoftnessRenderer components that were auto-managed
            foreach (var kvp in m_ChildRenderers)
            {
                if (kvp.Value != null)
                {
                    if (Application.isPlaying)
                        UnityEngine.Object.Destroy(kvp.Value);
                    else
                        UnityEngine.Object.DestroyImmediate(kvp.Value);
                }
            }

            m_ChildRenderers.Clear();
        }

        protected void OnTransformChildrenChanged()
        {
            RequestUpdate();
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
