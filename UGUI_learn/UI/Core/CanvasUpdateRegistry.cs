using System;
using UnityEngine.UI.Collections;

namespace UnityEngine.UI
{
    public enum CanvasUpdate
    {
        PreLayout = 0,
        Layout = 1,
        PostLayout = 2,
        PreRender = 3,
        LateRender = 4,
        MaxUpdateValue = 5,
    }

    public interface ICanvasElement
    {
        void Rebuild(CanvasUpdate executing);
        Transform transform { get; }
        void LayoutComplete();
        void GraphicUpdateComplete();
        bool IsDestroyed();
    }

    public class CanvasUpdateRegistry
    {
        private static CanvasUpdateRegistry s_Instance;

        private bool m_PerformingLayoutUpdate;
        private bool m_PerformingGraphicUpdate;
        
        private readonly IndexedSet<ICanvasElement> m_LayoutRebuildQueue = new IndexedSet<ICanvasElement>();
        private readonly IndexedSet<ICanvasElement> m_GraphicRebuildQueue = new IndexedSet<ICanvasElement>();

        protected CanvasUpdateRegistry()
        {
            Canvas.willRenderCanvases += PerformUpdate;
        }

        public static CanvasUpdateRegistry instance
        {
            get
            {
                if(s_Instance == null)
                    s_Instance = new CanvasUpdateRegistry();
                return s_Instance;
            }
        }

        private bool ObjectValidForUpdate(ICanvasElement element)
        {
            var valid = element != null;
            var isUnityObject = element as Object;
            if (isUnityObject)
                valid = (element as Object) != null;

            return valid;
        }
        
        //todo

        private void CleanInvalidItems()
        {
            for (int i = m_LayoutRebuildQueue.Count - 1; i >= 0; i--)
            {
                var item = m_LayoutRebuildQueue[i];
                if (item == null)
                {
                    m_LayoutRebuildQueue.RemoveAt(i);
                    continue;
                }

                if (item.IsDestroyed())
                {
                    m_LayoutRebuildQueue.RemoveAt(i);
                    item.LayoutComplete();
                }
            }

            for (int i = m_GraphicRebuildQueue.Count - 1; i >= 0; i--)
            {
                var item = m_GraphicRebuildQueue[i];
                if (item == null)
                {
                    m_GraphicRebuildQueue.RemoveAt(i);
                    continue;
                }

                if (item.IsDestroyed())
                {
                    m_GraphicRebuildQueue.RemoveAt(i);
                    item.GraphicUpdateComplete();
                }
            }
        }

        private static readonly Comparison<ICanvasElement> s_SortLayoutElement = SortLayoutList;

        private void PerformUpdate()
        {
            CleanInvalidItems();
            m_PerformingLayoutUpdate = true;
            
            // sort by parentCount, increase
            m_LayoutRebuildQueue.Sort(s_SortLayoutElement);
            for (int i = 0; i <= (int) CanvasUpdate.MaxUpdateValue; i++)
            {
                for (int j = 0; j < m_LayoutRebuildQueue.Count; j++)
                {
                    var rebuild = m_LayoutRebuildQueue[j];
                    try
                    {
                        if(ObjectValidForUpdate(rebuild))
                            rebuild.Rebuild((CanvasUpdate)i);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e, rebuild.transform);
                    }
                }
            }

            for (int i = 0; i < m_LayoutRebuildQueue.Count; i++)
            {
                m_LayoutRebuildQueue[i].LayoutComplete();
            }
            
            m_LayoutRebuildQueue.Clear();
            m_PerformingLayoutUpdate = false;
            
            ClipperRegistry.instance.Cull();

            m_PerformingGraphicUpdate = true;
            for (int i = (int)CanvasUpdate.PreRender; i < (int)CanvasUpdate.MaxUpdateValue; i++)
            {
                for (int j = 0; j < instance.m_GraphicRebuildQueue.Count; j++)
                {
                    try
                    {
                        var element = instance.m_GraphicRebuildQueue[j];
                        if (ObjectValidForUpdate(element))
                            element.Rebuild((CanvasUpdate) i);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e, instance.m_GraphicRebuildQueue[j].transform);
                    }
                }
            }

            for (int i = 0; i < m_GraphicRebuildQueue.Count; i++)
            {
                m_GraphicRebuildQueue[i].GraphicUpdateComplete();
            }
            
            instance.m_GraphicRebuildQueue.Clear();
            m_PerformingGraphicUpdate = false;
        }

        private static int ParentCount(Transform child)
        {
            if (child == null)
                return 0;
            Transform t;
            int ret = 0;
            while (true)
            {
                if (child.parent)
                {
                    t = child.parent;
                    ret++;
                }
                else
                {
                    break;
                }
            }

            return ret;
        }
        

        // if return positive value, then the order is y x
        // so the result is, sorted by parentCount increase
        private static int SortLayoutList(ICanvasElement x, ICanvasElement y)
        {
            Transform t1 = x.transform;
            Transform t2 = y.transform;
            return ParentCount(t1) - ParentCount(t2);
        }
        
        public static bool TryRegisterCanvasElementForLayoutRebuild(ICanvasElement element)
        {
            return instance.InternalRegisterCanvasElementForLayoutRebuild(element);
        }

        private bool InternalRegisterCanvasElementForLayoutRebuild(ICanvasElement element)
        {
            if (m_LayoutRebuildQueue.Contains(element))
                return false;

            return m_LayoutRebuildQueue.AddUnique(element);
        }

        public static bool RegisterCanvasElementForGraphicRebuild(ICanvasElement element)
        {
            return instance.InternalRegisterCanvasElementForGraphicRebuild(element);
        }

        private bool InternalRegisterCanvasElementForGraphicRebuild(ICanvasElement element)
        {
            if (m_PerformingGraphicUpdate)
            {
                Debug.LogError(string.Format("Trying to add {0} for graphic rebuild while we are already inside a graphic rebuild loop. This is not supproted", element));
                return false;
            }

            return m_GraphicRebuildQueue.AddUnique(element);
        }

        public static bool IsRebuildingLayout()
        {
            return instance.m_PerformingLayoutUpdate;
        }

        public static bool IsRebuildingGraphics()
        {
            return instance.m_PerformingGraphicUpdate;
        }
    }
}