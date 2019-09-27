using System;
using UnityEngine.Events;
using UnityEngine.EventSystem;

namespace UnityEngine.UI
{
    public abstract class Graphic : UIBehaviour, ICanvasElement
    {
        static protected Material s_DefaultUI = null;
        static protected Texture2D s_WhiteTexture = null;

        [SerializeField] protected Material m_Material;
        [SerializeField] private Color m_Color = Color.white;
        
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

        public virtual Material defaultMaterial
        {
            get { return defaultGraphicMaterial;  }
        }

        public virtual Material material
        {
            get { return (m_Material != null) ? m_Material : defaultMaterial; }
            set
            {
                if (m_Material == null)
                    return;
                m_Material = value;
                SetMaterialDirty();
            }
        }


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

        // todo, 所有可显示对象使用同一个mesh? 例如一个canvas 下面有一个image + text，这两个是按照先后顺序来分别重建mesh的？而不是一个整体?
        [NonSerialized] protected static Mesh s_Mesh;
        [NonSerialized] private static readonly VertexHelper s_VertexHelper = new VertexHelper();

//        [NonSerialized] private readonly TweenRunner<ColorTween> m_ColorTweenRunner;
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
//            if(m_ColorTweenRunner == null)
//                m_ColorTweenRunner = new TweenRunner<ColorTween>();
            useLegacyMeshGeneration = true;
        }

        public RectTransform rectTransform
        {
            get { return m_RectTransform ?? (m_RectTransform = GetComponent<RectTransform>()); }
        }

        public Canvas canvas
        {
            get
            {
                if (m_Canvas == null)
                    CacheCanvas();
                return m_Canvas;
            }
        }

        private void CacheCanvas()
        {
            var list = ListPool<Canvas>.Get();
            gameObject.GetComponentsInParent(false, list);
            if (list.Count > 0)
            {
                // Find the nearest parent canvas?
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].isActiveAndEnabled)
                    {
                        m_Canvas = list[i];
                        break;
                    }
                }
            }
            else
                m_Canvas = null;
            
            ListPool<Canvas>.Release(list);
        }
        
        public CanvasRenderer canvasRenderer
        {
            get
            {
                if (m_CanvasRender == null)
                    m_CanvasRender = GetComponent<CanvasRenderer>();
                return m_CanvasRender;
            }
        }

        public int depth
        {
            get { return canvasRenderer.absoluteDepth; }
        }

        public virtual Material materialForRendering
        {
            get
            {
                var components = ListPool<Component>.Get();
                GetComponents(typeof(IMaterialModifier), components);
                var currentMat = material;
                for (int i = 0; i < components.Count; i++)
                {
                    currentMat = (components[i] as IMaterialModifier).GetModifiedMaterial(currentMat);
                }
                ListPool<Component>.Release(components);
                return currentMat;
            }
        }

        public virtual Texture mainTexture
        {
            get { return s_WhiteTexture; }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            CacheCanvas();
            GraphicRegistry.RegisterGraphicForCanvas(canvas, this);
            
            if(s_WhiteTexture == null)
                s_WhiteTexture = Texture2D.whiteTexture;
            
            SetAllDirty();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            GraphicRegistry.UnregisterGraphicForCanvas(canvas, this);
            CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(this);
            //todo
        }


        public virtual void SetAllDirty()
        {
            SetLayoutDirty();
            SetVerticesDirty();
            SetMaterialDirty();
        }

        public virtual void SetLayoutDirty()
        {
            if (!IsActive())
                return;
            
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
            if (m_OnDirtyLayoutCallback != null)
                m_OnDirtyLayoutCallback();
        }

        public virtual void SetVerticesDirty()
        {
            if (!IsActive())
                return;
            m_VertsDirty = true;
            CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);
            if (m_OnDirtyVertsCallback != null)
                m_OnDirtyVertsCallback();
        }

        public virtual void SetMaterialDirty()
        {
            if (!IsActive())
                return;
            m_MaterialDirty = true;
            CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);
            if (m_OnDirtyMaterialCallback != null)
                m_OnDirtyMaterialCallback();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            if (gameObject.activeInHierarchy)
            {
                if (CanvasUpdateRegistry.IsRebuildingLayout())
                {
                    SetVerticesDirty();
                }
                else
                {
                    SetVerticesDirty();
                    SetLayoutDirty();
                }
            }
        }

        protected override void OnBeforeTransformParentChanged()
        {
            GraphicRegistry.UnregisterGraphicForCanvas(canvas, this);
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            m_Canvas = null;
            if (!IsActive())
                return;
            
            CacheCanvas();
            GraphicRegistry.RegisterGraphicForCanvas(canvas, this);
            SetAllDirty();
        }

        public void Rebuild(CanvasUpdate executing)
        {
            if (canvasRenderer.cull)
                return;
            switch (executing)
            {
                case CanvasUpdate.PreRender:
                    if (m_VertsDirty)
                    {
                        UpdateGeometry();
                        m_VertsDirty = false;
                    }

                    if (m_MaterialDirty)
                    {
                        UpdateMaterial();
                        m_MaterialDirty = false;
                    }

                    break;
            }
        }

        protected virtual void UpdateGeometry()
        {
            if(useLegacyMeshGeneration)
                DoLegacyMeshGeneration();
            else
            {
                DoMeshGeneration();
            }
        }

        public void LayoutComplete()
        {
        }

        public void GraphicUpdateComplete()
        {
        }

        protected virtual void UpdateMaterial()
        {
            if (!IsActive())
                return;
            canvasRenderer.materialCount = 1;
            canvasRenderer.SetMaterial(materialForRendering, 0);
            canvasRenderer.SetTexture(mainTexture);
        }

        protected static Mesh workerMesh
        {
            get
            {
                if (s_Mesh == null)
                {
                    s_Mesh = new Mesh();
                    s_Mesh.name = "Shared UI Mesh";
                    s_Mesh.hideFlags = HideFlags.HideAndDontSave;
                }

                return s_Mesh;
            }
        }

        private void DoMeshGeneration()
        {
            if (rectTransform != null && rectTransform.rect.width >= 0 && rectTransform.rect.height >= 0)
            {
                OnPopulateMesh(s_VertexHelper);
            }
            else
            {
                s_VertexHelper.Clear();
            }

            var components = ListPool<Component>.Get();
            GetComponents(typeof(IMeshModifier), components);
            for (int i = 0; i < components.Count; i++)
            {
                ((IMeshModifier)components[i]).ModifyMesh(s_VertexHelper);
            }
            ListPool<Component>.Release(components);
            s_VertexHelper.FillMesh(workerMesh);
            canvasRenderer.SetMesh(workerMesh);
        }

        private void DoLegacyMeshGeneration()
        {
            if (rectTransform != null && rectTransform.rect.width >= 0 && rectTransform.rect.height >= 0)
            {
                OnPopulateMesh(workerMesh);
            }
            else
            {
                workerMesh.Clear();
            }

            var components = ListPool<Component>.Get();
            GetComponents(typeof(IMeshModifier), components);
            for (int i = 0; i < components.Count; i++)
            {
                ((IMeshModifier) components[i]).ModifyMesh(workerMesh);
            }
            ListPool<Component>.Release(components);
            canvasRenderer.SetMesh(workerMesh);
        }

        protected virtual void OnPopulateMesh(Mesh m)
        {
            OnPopulateMesh(s_VertexHelper);
            s_VertexHelper.FillMesh(m);
        }

        protected virtual void OnPopulateMesh(VertexHelper vh)
        {
            var r = GetPixelAdjustedRect();
            var v = new Vector4(r.x, r.y, r.x + r.width, r.y + r.height);

            Color32 color32 = color;
            vh.Clear();
            vh.AddVert(new Vector3(v.x, v.y), color32, new Vector2(0f, 0f));
            vh.AddVert(new Vector3(v.x,v.w), color32, new Vector2(0f, 1f));
            vh.AddVert(new Vector3(v.z, v.w), color32, new Vector2(1f, 1f));
            vh.AddVert(new Vector3(v.z, v.y), color32, new Vector2(1f, 0f));
            
            vh.AddTriangle(0, 1, 2);
            // todo, the last idx must be the same ? why not 0,2,3
            vh.AddTriangle(2, 3, 0);
        }

        public Rect GetPixelAdjustedRect()
        {
            if (!canvas || canvas.renderMode == RenderMode.WorldSpace || canvas.scaleFactor == 0.0f ||
                !canvas.pixelPerfect)
                return rectTransform.rect;
            else
            {
                return RectTransformUtility.PixelAdjustRect(rectTransform, canvas);
            }
        }
    }
}