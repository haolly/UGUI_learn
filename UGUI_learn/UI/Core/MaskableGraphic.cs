using System;
using UnityEngine.Events;
using UnityEngine.Rendering;

namespace UnityEngine.UI
{
    public class MaskableGraphic : Graphic, IClippable, IMaskable, IMaterialModifier
    {
        [NonSerialized] protected bool m_ShouldRecalculateStencil = true;

        [NonSerialized] protected Material m_MaskMaterial;
        
        [NonSerialized] protected int m_StencilValue;

        [NonSerialized] protected internal Mask m_Mask;

        [NonSerialized] private RectMask2D m_ParentMask;

        [NonSerialized] private bool m_Maskable = true;

        [Serializable]
        public class CullStateChangedEvent : UnityEvent<bool>
        {
        }
        
        [SerializeField]
        private CullStateChangedEvent m_OnCullStateChanged = new CullStateChangedEvent();

        public bool maskable
        {
            get => m_Maskable;
            set
            {
                if (value == m_Maskable)
                    return;
                m_Maskable = value;
                m_ShouldRecalculateStencil = true;
                SetMaterialDirty();
            }
        }


        protected override void Awake()
        {
            base.Awake();
            Mask mask = GetComponent<Mask>();
            if (mask != null)
                m_Mask = mask;
        }
        
        readonly Vector3[] m_Corners = new Vector3[4];

        private Rect rootCanvasRect
        {
            get
            {
                rectTransform.GetWorldCorners(m_Corners);
                if (canvas)
                {
                    Canvas rootCanvas = canvas.rootCanvas;
                    for (int i = 0; i < 4; i++)
                    {
                        m_Corners[i] = rootCanvas.transform.InverseTransformPoint(m_Corners[i]);
                    }
                }
                return new Rect(m_Corners[0].x, m_Corners[0].y, m_Corners[2].x - m_Corners[0].x, 
                    m_Corners[2].y - m_Corners[0].y);
            }
        }

        private void UpdateClipParent()
        {
            var newParent = (maskable && IsActive()) ? MaskUtilities.GetRectMaskForClippable(this) : null;
            if (m_ParentMask != null && (newParent != m_ParentMask || !newParent.IsActive()))
            {
                m_ParentMask.RemoveClippable(this);
                UpdateCull(false);
            }
            if(newParent != null && newParent.IsActive())
                newParent.AddClippable(this);

            m_ParentMask = newParent;
        }

        #region IClippable 

        public virtual void RecalculateClipping()
        {
            UpdateClipParent();
        }

        public virtual void Cull(Rect clipRect, bool validRect)
        {
            var cull = !validRect || !clipRect.Overlaps(rootCanvasRect, true);
            // if not overlap or is not valid rect, then this canvas is ignored by renderer
            UpdateCull(cull);
        }

        public virtual void SetClipRect(Rect value, bool validRect)
        {
            if(validRect)
                canvasRenderer.EnableRectClipping(value);
            else 
                canvasRenderer.DisableRectClipping();
        }

        #endregion
        
        private void UpdateCull(bool cull)
        {
            var cullingChanged = canvasRenderer.cull != cull;
            //Indicates whether geometry emitted by this renderer is ignored.
            canvasRenderer.cull = cull;
            if (cullingChanged)
            {
                SetVerticesDirty();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            m_ShouldRecalculateStencil = true;
            UpdateClipParent();
            SetMaterialDirty();

            if (m_Mask != null)
            {
                MaskUtilities.NotifyStencilStateChanged(this);
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            m_ShouldRecalculateStencil = true;
            SetMaterialDirty();
            UpdateClipParent();
            StencilMaterial.Remove(m_MaskMaterial);
            m_MaskMaterial = null;

            if (m_Mask != null)
            {
                MaskUtilities.NotifyStencilStateChanged(this);
            }
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            if (!isActiveAndEnabled)
                return;
            m_ShouldRecalculateStencil = true;
            UpdateClipParent();
            SetMaterialDirty();
        }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            if (!isActiveAndEnabled)
                return;
            m_ShouldRecalculateStencil = true;
            UpdateClipParent();
            SetMaterialDirty();
        }

        #region IMaskable

        public virtual void RecalculateMasking()
        {
            m_ShouldRecalculateStencil = true;
            SetMaterialDirty();
        }
        
        #endregion

        public virtual Material GetModifiedMaterial(Material baseMaterial)
        {
            var toUse = baseMaterial;

            if (m_ShouldRecalculateStencil)
            {
                var rootCanvas = MaskUtilities.FindRootSortOverrideCanvas(transform);
                m_StencilValue = maskable ? MaskUtilities.GetStencilDepth(transform, rootCanvas) : 0;
                m_ShouldRecalculateStencil = false;
            }

            Mask maskComponent = m_Mask;
            if (m_StencilValue > 0 && (maskComponent == null || !maskComponent.IsActive()))
            {
                var maskMat = StencilMaterial.Add(toUse, (1 << m_StencilValue) - 1, StencilOp.Keep,
                    CompareFunction.Equal,
                    ColorWriteMask.All, (1 << m_StencilValue) - 1, 0);
                StencilMaterial.Remove(m_MaskMaterial);
                m_MaskMaterial = maskMat;
                toUse = m_MaskMaterial;
            }

            return toUse;
        }
    }
}