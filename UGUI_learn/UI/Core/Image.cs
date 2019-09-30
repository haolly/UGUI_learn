using System;
using System.Collections.Generic;
using System.Configuration;

namespace UnityEngine.UI
{
    public class Image : MaskableGraphic, ISerializationCallbackReceiver, ILayoutElement, ICanvasRaycastFilter
    {
        protected static Color DEFAULT_OVERLAY_COLOR = new Color(0, 0, 0, 0);

        public enum Type
        {
            Simple,
            Sliced,
            Tiled,
            Filled
        }
        
        public enum FillMethod
        {
            Horizontal,
            Vertical,
            Radial90,
            Radial180,
            Radial360
        }
        
        public enum OriginHorizontal
        {
            Left, 
            Right,
        }
        
        public enum OriginVertical
        {
            Bottom,
            Top,
        }
        //todo

        static protected Material s_ETC1DefaultUI = null;
        public enum Rotation
        {
            None,
            Rotation90,
            Rotation180,
            Rotation270,
            FlipHorizontal,
            FlipVertical,
        }

        [SerializeField] 
        private Sprite m_Sprite;

        public Sprite sprite
        {
            get { return m_Sprite; }
            set
            {
                if(SetPropertyUtility.SetClass(ref m_Sprite, value))
                    SetAllDirty();
            }
        }

        [NonSerialized]
        private Sprite m_OverrideSprite;

        public Sprite overrideSprite
        {
            get { return m_OverrideSprite; }
            set
            {
                if(SetPropertyUtility.SetClass(ref m_OverrideSprite, value))
                    SetAllDirty();
            }
        }

        private Sprite activeSprite
        {
            get
            {
                return m_OverrideSprite != null ? m_OverrideSprite : sprite;
            }
        }

        [SerializeField] private Type m_Type = Type.Simple;

        public Type type
        {
            get { return m_Type;  }
            set
            {
                if(SetPropertyUtility.SetStruct(ref m_Type, value))
                    SetVerticesDirty();
            }
        }

        [SerializeField] private bool m_PreserveAspect = false;

        public bool preserveAspect
        {
            get { return m_PreserveAspect; }
            set
            {
                if(SetPropertyUtility.SetStruct(ref m_PreserveAspect, value))
                    SetVerticesDirty();
            }
        }
        
        //todo

        [Range(0, 1)] [SerializeField] private float m_FillAmount = 1.0f;
        public float fillAmount
        {
            get { return m_FillAmount; }
            set
            {
                if(SetPropertyUtility.SetStruct(ref m_FillAmount, value))
                    SetVerticesDirty();
            }
        }

        [SerializeField] private Rotation m_Rotation = Rotation.None;

        public Rotation rotation
        {
            get { return m_Rotation; }
            set
            {
                if(SetPropertyUtility.SetStruct(ref m_Rotation, value))
                    SetVerticesDirty();
            }
        }
        
        [SerializeField] private Vector4 m_SpritePadding = Vector4.zero;

        public Vector4 spritePadding
        {
            get { return m_SpritePadding; }
            set { m_SpritePadding = value; }
        }

        private static bool m_EnablePolygon = true;
        public bool enablePolygon
        {
            get { return m_EnablePolygon;  }
            set
            {
                if(SetPropertyUtility.SetStruct(ref m_EnablePolygon, value))
                    SetVerticesDirty();
            }
        }

        
        public void OnBeforeSerialize()
        {
            throw new System.NotImplementedException();
        }

        public void OnAfterDeserialize()
        {
            throw new System.NotImplementedException();
        }

        public void CalculateLayoutInputHorizontal()
        {
            throw new System.NotImplementedException();
        }

        public void CalculateLayoutInputVertical()
        {
            throw new System.NotImplementedException();
        }

        public float mainWidth { get; }
        public float preferredWidth { get; }
        public float flexibleWidth { get; }
        public float mainHeight { get; }
        public float preferredHeight { get; }
        public float flexibleHeight { get; }
        public int layoutPriority { get; }
        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            throw new System.NotImplementedException();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            if (activeSprite == null)
            {
                base.OnPopulateMesh(vh);
                return;
            }

            switch (type)
            {
                case Type.Simple:
                {
                    if (!m_EnablePolygon || this.overrideSprite == null || this.overrideSprite.triangles.Length == 6)
                    {
                        // quad
                        GenerateSimpleSprite(vh, m_PreserveAspect);
                    }
                    else
                    {
                        GeneratePolygonSprite(vh, m_PreserveAspect);
                    }
                    break;
                }
                case Type.Filled:
                    GenerateFilledSprite(vh, m_PreserveAspect);
                    break;
                //todo
            }

            ApplyOverlayColorAndGrey(vh);
        }
        
        static readonly Vector3[] s_Xy = new Vector3[4];
        static readonly Vector3[] s_Uv = new Vector3[4];

        void GenerateFilledSprite(VertexHelper toFill, bool preserveAspect)
        {
            toFill.Clear();
            if (m_FillAmount < 0.001f)
                return;
            Vector4 v = GetDrawingDimensions(preserveAspect);
            Vector4 outer = activeSprite != null ? Sprites.DataUtility.GetOuterUV(activeSprite) : Vector4.zero;
            UIVertex uiv = UIVertex.simpleVert;
            uiv.color = color;

            float tx0 = outer.x;
            float ty0 = outer.y;
            float tx1 = outer.z;
            float ty1 = outer.w;
            
            if(m_FillAmount)
                //todo
        }

        private Vector4 GetDrawingDimensions(bool shoudlPreserveAspect)
        {
            var padding = activeSprite == null ? Vector4.zero : Sprites.DataUtility.GetPadding(activeSprite);
            var size = activeSprite == null
                ? Vector2.zero
                : new Vector2(activeSprite.rect.width, activeSprite.rect.height);

            Rect r = GetPixelAdjustedRect();

            int spriteW = Mathf.RoundToInt(size.x);
            int spriteH = Mathf.RoundToInt(size.y);
            
            // x,y,width,height
            var v = new Vector4(
                    (padding.x + m_SpritePadding.x) / spriteW,
                    (padding.y + m_SpritePadding.y) / spriteH,
                    (spriteW - padding.z - m_SpritePadding.z) / spriteW,
                    (spriteH - padding.w - m_SpritePadding.w) / spriteH
                );

            if (shoudlPreserveAspect && size.sqrMagnitude > 0.0f)
            {
                var spriteRatio = size.x / size.y;
                var rectRatio = r.width / r.height;
                // 以 spriteRatio 为准
                if (spriteRatio > rectRatio)
                {
                    var oldHeight = r.height;
                    var newHeight = r.width / spriteRatio;
                    r.height = newHeight;
                    r.y += (oldHeight - r.height) * rectTransform.pivot.y;
                }
                else
                {
                    var oldWidth = r.width;
                    var newWidth = r.height * spriteRatio;
                    r.width = newWidth;
                    r.x += (oldWidth - r.width) * rectTransform.pivot.x;
                }
            }
            v = new Vector4(
                r.x + r.width * v.x,
                r.y + r.height * v.y,
                r.x + r.width * v.z,
                r.y + r.height * v.w
                );
            return v;
        }

        void GeneratePolygonSprite(VertexHelper vh, bool preserveAspect)
        {
            Vector4 v = GetDrawingDimensions(preserveAspect);
            var color32 = color;

            int len = sprite.vertices.Length;
            var vertices = new List<UIVertex>(len);
            Vector2 center = sprite.bounds.center;
            Vector2 invExtend = new Vector2(1 / sprite.bounds.size.x, 1 / sprite.bounds.size.y);
            for (int i = 0; i < len; i++)
            {
                // range [-0.5, 0.5] + 0.5 = [0, 1]
                float x = (sprite.vertices[i].x - center.x) * invExtend.x + 0.5f;
                float y = (sprite.vertices[i].y - center.y) * invExtend.y + 0.5f;
                
                UIVertex vertice = new UIVertex();
                vertice.color = color32;
                vertice.uv0 = sprite.uv[i];
                vertice.position = CalPosition(v, x, y);
                
                vertices.Add(vertice);
            }

            len = sprite.triangles.Length;
            
            var triangles = new List<int>(len);
            for (int i = 0; i < len; i++)
            {
                triangles.Add(sprite.triangles[i]);
            }
            vh.Clear();
            vh.AddUIVertexStream(vertices, triangles);
        }

        private Vector3 CalPosition(Vector4 bound, float x, float y)
        {
            Vector3 pos = Vector3.zero;
            switch (m_Rotation)
            {
                case Rotation.FlipHorizontal:
                {
                    pos.x = Mathf.Lerp(bound.z, bound.x, x);
                    pos.y = Mathf.Lerp(bound.y, bound.w, y);
                    break;
                }
                case Rotation.FlipVertical:
                {
                    pos.x = Mathf.Lerp(bound.x, bound.z, x);
                    pos.y = Mathf.Lerp(bound.w, bound.y, y);
                    break;
                }
                case Rotation.Rotation90:
                {
                    //Note, rotate (x,y) to get new (x,y), and then use new x,y to lerp
                    // rotate 90 clockwise get (y, -x)
                    pos.x = Mathf.Lerp(bound.x, bound.z, y);
                    pos.y = Mathf.Lerp(bound.y, bound.w, -x);
                    break;
                }
                default:
                    pos.x = Mathf.Lerp(bound.x, bound.z, x);
                    pos.y = Mathf.Lerp(bound.y, bound.w, y);
                    break;
            }

            return pos;
        }

        /// <summary>
        /// Quad , two triangle
        /// </summary>
        /// <param name="vh"></param>
        /// <param name="preserveAspect"></param>
        void GenerateSimpleSprite(VertexHelper vh, bool preserveAspect)
        {
            Vector4 v = GetDrawingDimensions(preserveAspect);
            var uv = (activeSprite != null) ? Sprites.DataUtility.GetOuterUV(activeSprite) : Vector4.zero;
            var color32 = color;
            vh.Clear();
            switch (m_Rotation)
            {
                case Rotation.None:
                    vh.AddVert(new Vector3(v.x, v.y), color32, new Vector2(uv.x, uv.y) );
                    vh.AddVert(new Vector3(v.x, v.w), color32, new Vector2(uv.x, uv.w));
                    vh.AddVert(new Vector3(v.z, v.w), color32, new Vector2(uv.z, uv.w));
                    vh.AddVert(new Vector3(v.z, v.y), color32, new Vector2(uv.z, uv.y));
                    break;
                
                case Rotation.Rotation90:
                    //TODO, 怎么算的?
                    vh.AddVert(new Vector3(v.x, v.y), color32, new Vector2(uv.z, uv.y));
                    break;
                case Rotation.FlipHorizontal:
                    vh.AddVert(new Vector3(v.x, v.y), color32, new Vector2(uv.z, uv.y));
                    vh.AddVert(new Vector3(v.x, v.w), color32, new Vector2(uv.z, uv.w));
                    vh.AddVert(new Vector3(v.z, v.w), color32, new Vector2(uv.x, uv.w));
                    vh.AddVert(new Vector3(v.z, v.y), color32, new Vector2(uv.x, uv.y));
                    break;
            }
            // TODO, clockwise ?
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }
    }
}