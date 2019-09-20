using System;
using UnityEngine.Events;
using UnityEngine.EventSystem;

namespace UnityEngine.UI
{
    public abstract class Graphic : UIBehaviour
    {
        static protected Material s_DefaultUI = null;
        static protected Texture2D s_WhiteTexture = null;

        /// <summary>
        /// Default material used to draw everything if no explicit material was specified
        /// </summary>
        static public Material defaultGraphicMaterial
        {
            get
            {
                if (s_DefaultUI == null)
                    s_DefaultUI = Canvas.GetDefaultCanvasMaterial();
                return s_DefaultUI;
            }
        }

        [SerializeField] protected Material m_Material;
        [SerializeField] private Color m_Color = Color.white;

        public virtual Color color
        {
            get { return m_Color; }
            set
            {
                if (SetPropertyUtility.SetColor(ref m_Color, value))
                    SetVerticesDirty();
            }
        }

        [SerializeField]private bool m_RaycastTarget = true;

        public virtual bool raycastTarget
        {
            get { return m_RaycastTarget; }
            set { m_RaycastTarget = value;  }
        }

        [NonSerialized] private RectTransform m_RectTransform;
        [NonSerialized] private CanvasRenderer m_CanvasRender;
        [NonSerialized] private Canvas m_Canvas;
        [NonSerialized] private bool m_VertsDirty;
        [NonSerialized] private bool m_MaterialDirty;

        [NonSerialized] protected UnityAction m_OnDirtyLayoutCallback;
        [NonSerialized] protected UnityAction m_OnDirtyVertsCallback;
        [NonSerialized] protected UnityAction m_OnDirtyMaterialCallback;

        [NonSerialized] protected static Mesh s_Mesh;
        [NonSerialized] private static readonly VertexHelper s_VertexHelper = new VertexHelper();

        [NonSerialized] private readonly TweenRunner<ColorTween> m_ColorTweenRunner;
        [SerializeField] protected bool m_Gray = false;

        public bool gray
        {
            get { return m_Gray; }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_Gray, value))
                {
                    SetVerticesDirty();
                }
            }
        }
        protected bool useLegacyMeshGeneration { get; set; }

        protected Graphic()
        {
            if(m_ColorTweenRunner == null)
                m_ColorTweenRunner = new TweenRunner<ColorTween>();
            //todo
            useLegacyMeshGeneration = true;
        }

        public float alpha
        {
            get { return color.a; }
            set
            {
                if (value != color.a)
                {
                    value = Mathf.Clamp01(value);
                    color = new Color(color.r, color.g, color.b, value);
                }
            }
        }

        public virtual void SetAllDirty()
        {
            SetLayoutDirty();
            //
        }
        
        //todo
        

    }
}