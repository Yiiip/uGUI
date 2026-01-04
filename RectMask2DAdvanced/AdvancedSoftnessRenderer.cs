using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Pool;

namespace UnityEngine.UI
{
    /// <summary>
    /// Advanced softness renderer that applies per-edge softness through custom material manipulation.
    /// This works by injecting softness parameters into the material's shader.
    /// Implements IClippable to receive clip rect updates from parent RectMask2D.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class AdvancedSoftnessRenderer : MonoBehaviour, IMaterialModifier, IClippable
    {
        private static readonly int EdgeSoftnessID = Shader.PropertyToID("_EdgeSoftness");
        private static readonly int ClipRectID = Shader.PropertyToID("_ClipRect");
        private static readonly int UseAdvancedSoftnessID = Shader.PropertyToID("_UseAdvancedSoftness");

        [SerializeField]
        private Vector4 m_EdgeSoftness = new Vector4();

        /// <summary>
        /// Per-edge softness: X=Left, Y=Bottom, Z=Right, W=Top (in pixels)
        /// </summary>
        public Vector4 edgeSoftness
        {
            get { return m_EdgeSoftness; }
            set
            {
                if (m_EdgeSoftness != value)
                {
                    m_EdgeSoftness.x = Mathf.Max(0, value.x);
                    m_EdgeSoftness.y = Mathf.Max(0, value.y);
                    m_EdgeSoftness.z = Mathf.Max(0, value.z);
                    m_EdgeSoftness.w = Mathf.Max(0, value.w);
                    SetMaterialDirty();
                }
            }
        }

        [SerializeField]
        private bool m_UseAdvancedSoftness = true;

        public bool useAdvancedSoftness
        {
            get { return m_UseAdvancedSoftness; }
            set
            {
                if (m_UseAdvancedSoftness != value)
                {
                    m_UseAdvancedSoftness = value;
                    SetMaterialDirty();
                }
            }
        }

        private Graphic m_Graphic;
        private Material m_ModifiedMaterial;
        private Material m_AdvancedSoftnessShader;
        private bool m_ShouldRecalculate = true;

        // IClippable implementation - store current clip rect
        private Rect m_ClipRect = new Rect();

        // Parent mask tracking
        [NonSerialized]
        private RectMask2D m_ParentMask;



        private Graphic graphic
        {
            get
            {
                if (m_Graphic == null)
                    m_Graphic = GetComponent<Graphic>();
                return m_Graphic;
            }
        }

        // IClippable properties
        new public GameObject gameObject { get { return base.gameObject; } }
        public RectTransform rectTransform
        {
            get
            {
                if (m_Graphic != null)
                    return m_Graphic.rectTransform;
                return GetComponent<RectTransform>();
            }
        }

        protected void Awake()
        {
            // Find or create's advanced softness shader material
            FindAdvancedSoftnessShader();

            if (graphic != null)
            {
                graphic.SetMaterialDirty();
            }

            // Register with parent mask if exists
            RecalculateClipping();
        }

        private void FindAdvancedSoftnessShader()
        {
            // Try to find's UI-AdvancedSoftness shader
            Shader shader = Shader.Find("UI/AdvancedSoftness");
            if (shader != null)
            {
                m_AdvancedSoftnessShader = new Material(shader);
                m_AdvancedSoftnessShader.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        protected void OnEnable()
        {
            SetMaterialDirty();
            RecalculateClipping();

            // In editor, also try to update clipping for Scene view
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null && m_ParentMask != null && m_ParentMask.isActiveAndEnabled)
                    {
                        m_ClipRect = m_ParentMask.canvasRect;
                        if (m_ModifiedMaterial != null)
                        {
                            UpdateMaterialClipRect();
                        }
                    }
                };
            }
#endif
        }

        protected void OnDisable()
        {
            SetMaterialDirty();
            RecalculateClipping();
        }

#if UNITY_EDITOR
        private void Update()
        {
            // In editor Scene view, ensure clip rect is properly set
            if (!Application.isPlaying && isActiveAndEnabled && m_ParentMask != null && m_ParentMask.isActiveAndEnabled)
            {
                // Check if we need to update the clip rect
                if (m_ModifiedMaterial != null)
                {
                    // Get current canvas rect from parent mask
                    Rect parentCanvasRect = m_ParentMask.canvasRect;

                    // Only update if it's different (to avoid unnecessary updates)
                    if (parentCanvasRect != m_ClipRect && parentCanvasRect.width > 0 && parentCanvasRect.height > 0)
                    {
                        m_ClipRect = parentCanvasRect;
                        UpdateMaterialClipRect();
                    }
                }
            }
        }
#endif

        protected void OnDestroy()
        {
            if (m_ModifiedMaterial != null)
            {
                MaterialManager.Destroy(m_ModifiedMaterial);
                m_ModifiedMaterial = null;
            }

            if (m_AdvancedSoftnessShader != null)
            {
                MaterialManager.Destroy(m_AdvancedSoftnessShader);
                m_AdvancedSoftnessShader = null;
            }
        }

        protected void OnTransformParentChanged()
        {
            RecalculateClipping();
        }

        protected void OnCanvasHierarchyChanged()
        {
            RecalculateClipping();
        }

        // IClippable implementation
        public void SetClipRect(Rect clipRect, bool validRect)
        {
            if (validRect)
            {
                m_ClipRect = clipRect;
            }
            else
            {
                m_ClipRect = new Rect(0, 0, 0, 0);
            }

            // Update's material immediately if it exists
            if (m_ModifiedMaterial != null)
            {
                UpdateMaterialClipRect();
            }
        }

        private void DisableHardwareClipping()
        {
            // Disable the Graphic's hardware clipping to avoid conflicts with our shader clipping
            if (graphic != null && graphic.canvasRenderer != null)
            {
                // Pass an invalid rect to disable hardware clipping
                graphic.canvasRenderer.DisableRectClipping();
            }
        }

        private void UpdateMaterialClipRect()
        {
            if (m_ModifiedMaterial == null)
                return;

            // If clipRect is invalid, try to get from parent mask
            if (!validRect && m_ParentMask != null && m_ParentMask.isActiveAndEnabled)
            {
                // Try to get canvas rect from parent mask
                m_ClipRect = m_ParentMask.canvasRect;
            }

            // If still invalid, don't apply clipping
            if (!validRect)
            {
                return;
            }

            // Convert canvas space clipRect to object space
            Rect objectSpaceClipRect = CanvasSpaceToObjectSpace(m_ClipRect);

            // Convert Rect to Vector4 for shader
            // Shader expects: x=xmin, y=ymin, z=xmax, w=ymax
            Vector4 clipRectVector = new Vector4(
                objectSpaceClipRect.x,
                objectSpaceClipRect.y,
                objectSpaceClipRect.x + objectSpaceClipRect.width,
                objectSpaceClipRect.y + objectSpaceClipRect.height
            );
            m_ModifiedMaterial.SetVector(ClipRectID, clipRectVector);
            m_ModifiedMaterial.SetFloat(UseAdvancedSoftnessID, 1.0f);
        }

        private void UpdateClipParent()
        {
            var newParent = MaskUtilities.GetRectMaskForClippable(this);

            // if the new parent is different OR is now inactive
            if (m_ParentMask != null && (newParent != m_ParentMask || !newParent.isActiveAndEnabled))
            {
                m_ParentMask.RemoveClippable(this);
                UpdateCull(false);
            }

            // don't re-add it if the newparent is inactive
            if (newParent != null && newParent.isActiveAndEnabled)
                newParent.AddClippable(this);

            m_ParentMask = newParent;
        }

        private void UpdateCull(bool cull)
        {
            if (graphic != null && graphic.canvasRenderer != null)
            {
                graphic.canvasRenderer.cull = cull;
            }
        }

        private bool validRect
        {
            get { return m_ClipRect.width > 0 && m_ClipRect.height > 0; }
        }

        private Rect CanvasSpaceToObjectSpace(Rect canvasRect)
        {
            if (rectTransform == null || canvasRect.width == 0 || canvasRect.height == 0)
                return canvasRect;

            // Get the canvas
            Canvas canvas = graphic != null ? graphic.canvas : null;
            if (canvas == null)
                return canvasRect;

            // Transform canvas rect corners to object space
            Vector3[] canvasCorners = new Vector3[4];
            canvasCorners[0] = new Vector3(canvasRect.x, canvasRect.y, 0);
            canvasCorners[1] = new Vector3(canvasRect.x, canvasRect.y + canvasRect.height, 0);
            canvasCorners[2] = new Vector3(canvasRect.x + canvasRect.width, canvasRect.y + canvasRect.height, 0);
            canvasCorners[3] = new Vector3(canvasRect.x + canvasRect.width, canvasRect.y, 0);

            // Transform to world space first
            for (int i = 0; i < 4; i++)
            {
                canvasCorners[i] = canvas.transform.TransformPoint(canvasCorners[i]);
            }

            // Transform to object space
            for (int i = 0; i < 4; i++)
            {
                canvasCorners[i] = rectTransform.InverseTransformPoint(canvasCorners[i]);
            }

            // Calculate bounds in object space
            float xMin = float.MaxValue;
            float yMin = float.MaxValue;
            float xMax = float.MinValue;
            float yMax = float.MinValue;

            for (int i = 0; i < 4; i++)
            {
                xMin = Mathf.Min(xMin, canvasCorners[i].x);
                yMin = Mathf.Min(yMin, canvasCorners[i].y);
                xMax = Mathf.Max(xMax, canvasCorners[i].x);
                yMax = Mathf.Max(yMax, canvasCorners[i].y);
            }

            // Validate the result
            if (xMin == float.MaxValue || xMax == float.MinValue)
                return canvasRect;

            return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        public void Cull(Rect clipRect, bool validRect)
        {
            // Cull's graphic based on clip rect
            if (graphic != null && graphic.canvasRenderer != null)
            {
                bool shouldCull = !validRect;

                if (!shouldCull)
                {
                    // Convert clipRect to object space for overlap check
                    Rect objectSpaceClipRect = CanvasSpaceToObjectSpace(clipRect);
                    shouldCull = !objectSpaceClipRect.Overlaps(rectTransform.rect, true);
                }

                graphic.canvasRenderer.cull = shouldCull;
            }
        }

        public void SetClipSoftness(Vector2 clipSoftness)
        {
            // Softness is handled by this component, ignore parent mask's softness
        }

        public void RecalculateClipping()
        {
            UpdateClipParent();
            // Force material update after clipping changes
            SetMaterialDirty();
        }

        public Material GetModifiedMaterial(Material baseMaterial)
        {
            if (!enabled || !useAdvancedSoftness || baseMaterial == null)
                return baseMaterial;

            // Only apply if there's actual softness
            if (m_EdgeSoftness.x == 0 && m_EdgeSoftness.y == 0 && m_EdgeSoftness.z == 0 && m_EdgeSoftness.w == 0)
                return baseMaterial;

            // Check if we have's advanced softness shader
            if (m_AdvancedSoftnessShader == null)
                FindAdvancedSoftnessShader();

            if (m_AdvancedSoftnessShader == null)
                return baseMaterial; // Fallback if shader not found

            if (m_ShouldRecalculate)
            {
                if (m_ModifiedMaterial != null)
                    MaterialManager.Destroy(m_ModifiedMaterial);

                // Clone's base material properties but use advanced softness shader
                m_ModifiedMaterial = new Material(m_AdvancedSoftnessShader);
                m_ModifiedMaterial.name = baseMaterial.name + " (Advanced Softness)";
                m_ModifiedMaterial.hideFlags = HideFlags.HideAndDontSave;

                // Copy properties from base material
                m_ModifiedMaterial.CopyPropertiesFromMaterial(baseMaterial);

                // Set softness parameters
                m_ModifiedMaterial.SetVector(EdgeSoftnessID, m_EdgeSoftness);
                m_ModifiedMaterial.SetFloat(UseAdvancedSoftnessID, 1.0f);

                // Set's clip rect we received (with coordinate space conversion)
                UpdateMaterialClipRect();

                m_ShouldRecalculate = false;
            }
            else if (m_ModifiedMaterial != null)
            {
                // Update parameters without recreating material
                m_ModifiedMaterial.SetVector(EdgeSoftnessID, m_EdgeSoftness);
                m_ModifiedMaterial.SetFloat(UseAdvancedSoftnessID, 1.0f);

                // Update clip rect (with coordinate space conversion)
                UpdateMaterialClipRect();
            }

            return m_ModifiedMaterial;
        }

        private void SetMaterialDirty()
        {
            m_ShouldRecalculate = true;
            if (graphic != null)
                graphic.SetMaterialDirty();
        }

#if UNITY_EDITOR
        protected void OnValidate()
        {
            m_EdgeSoftness.x = Mathf.Max(0, m_EdgeSoftness.x);
            m_EdgeSoftness.y = Mathf.Max(0, m_EdgeSoftness.y);
            m_EdgeSoftness.z = Mathf.Max(0, m_EdgeSoftness.z);
            m_EdgeSoftness.w = Mathf.Max(0, m_EdgeSoftness.w);
            SetMaterialDirty();
        }
#endif
    }

    /// <summary>
    /// Helper class for managing modified materials
    /// </summary>
    internal static class MaterialManager
    {
        public static void Destroy(Material material)
        {
            if (material != null)
            {
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(material);
                else
                    UnityEngine.Object.DestroyImmediate(material);
            }
        }
    }
}
