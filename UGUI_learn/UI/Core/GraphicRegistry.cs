using System.Collections.Generic;
using UnityEngine.UI.Collections;

namespace UnityEngine.UI
{
    public class GraphicRegistry
    {
        private static GraphicRegistry s_Instance;
        private readonly Dictionary<Canvas, IndexedSet<Graphic>> m_Graphics = new Dictionary<Canvas, IndexedSet<Graphic>>();

        protected GraphicRegistry()
        {
            Dictionary<Graphic, int> emptyGraphicDic;
            Dictionary<ICanvasElement, int> emptyElementDic;
        }

        public static GraphicRegistry instance
        {
            get
            {
                if(s_Instance == null)
                    s_Instance = new GraphicRegistry();
                return s_Instance;
            }
        }

        public static void RegisterGraphicForCanvas(Canvas c, Graphic graphic)
        {
            if (c == null)
                return;
            IndexedSet<Graphic> graphics;
            instance.m_Graphics.TryGetValue(c, out graphics);
            if (graphics != null)
            {
                graphics.AddUnique(graphic);
                return;
            }
            graphics = new IndexedSet<Graphic>();
            graphics.AddUnique(graphic);
            instance.m_Graphics.Add(c, graphics);
        }

        public static void UnregisterGraphicForCanvas(Canvas c, Graphic graphic)
        {
            if (c == null)
                return;
            IndexedSet<Graphic> graphics;
            instance.m_Graphics.TryGetValue(c, out graphics);
            if (graphics != null)
            {
                graphics.Remove(graphic);
                if (graphics.Count == 0)
                    instance.m_Graphics.Remove(c);
            }
        }
    }
}