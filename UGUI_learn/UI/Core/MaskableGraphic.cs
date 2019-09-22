using System;
using UnityEngine.Events;

namespace UnityEngine.UI
{
    public class MaskableGraphic : Graphic, IClippable, IMaskable, IMaterialModifier
    {
        [NonSerialized] protected bool m_ShouldRecalculateStencil = true;

        [NonSerialized] protected Material m_MaskMaterial;

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

        [NonSerialized] protected int m_StencilValue;

        public void RecalculateClipping()
        {
            throw new System.NotImplementedException();
        }

        public void Cull(Rect clipRect, bool validRect)
        {
            throw new System.NotImplementedException();
        }

        public void SetClipRect(Rect value, bool validRect)
        {
            throw new System.NotImplementedException();
        }

        public void RecalculateMasking()
        {
            throw new System.NotImplementedException();
        }

        public virtual Material GetModifiedMaterial(Material baseMaterial)
        {
            var toUse = baseMaterial;
            //todo
        }
    }
}