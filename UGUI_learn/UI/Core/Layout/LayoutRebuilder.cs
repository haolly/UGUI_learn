using System.Collections.Generic;
using UnityEngine.Events;

namespace UnityEngine.UI
{
    public class LayoutRebuilder : ICanvasElement
    {
        private RectTransform m_ToRebuild;
        //todo 
        private int m_CachedHashFromTransform;
        
        static ObjectPool<LayoutRebuilder> s_Rebuilders = new ObjectPool<LayoutRebuilder>(null, x => x.Clear());

        private void Initialize(RectTransform controller)
        {
            m_ToRebuild = controller;
            m_CachedHashFromTransform = controller.GetHashCode();
        }

        private void Clear()
        {
            m_ToRebuild = null;
            m_CachedHashFromTransform = 0;
        }

        static LayoutRebuilder()
        {
            RectTransform.reapplyDrivenProperties += ReapplyDrivenProperties;
        }

        static void ReapplyDrivenProperties(RectTransform driven)
        {
            MarkLayoutForRebuild(driven);
        }

        public Transform transform {  get { return m_ToRebuild; } }

        public bool IsDestroyed()
        {
            return m_ToRebuild == null;
        }

        static void StripDisabledBehavioursFromList(List<Component> components)
        {
            components.RemoveAll(e => e is Behaviour && !((Behaviour) e).isActiveAndEnabled);
        }

        //todo

        
        public void Rebuild(CanvasUpdate executing)
        {
            switch (executing)
            {
                case CanvasUpdate.Layout:
                    PerformLayoutCalculation(m_ToRebuild, e => (e as ILayoutElement).CalculateLayoutInputHorizontal());
                    PerformLayoutControl(m_ToRebuild, e => (e as ILayoutController).SetLayoutHorizontal());
                    //todo
                    break;
            }
        }

        private void PerformLayoutControl(RectTransform rect, UnityAction<Component> action)
        {
            if (rect == null)
                return;

            var components = ListPool<Component>.Get();
            rect.GetComponents(typeof(ILayoutController), components);
            StripDisabledBehavioursFromList(components);

            //todo what if components.Count == 0 and there has children ?
            if (components.Count > 0)
            {
                for (int i = 0; i < components.Count; i++)
                {
                    if (components[i] is ILayoutSelfController)
                        action(components[i]);
                }

                for (int i = 0; i < components.Count; i++)
                {
                    if (!(components[i] is ILayoutSelfController))
                        action(components[i]);
                }

                for (int i = 0; i < rect.childCount; i++)
                {
                    PerformLayoutControl(rect.GetChild(i) as RectTransform, action);
                }
            }

            ListPool<Component>.Release(components);
        }
        
        private void PerformLayoutCalculation(RectTransform rect, UnityAction<Component> action)
        {
            if (rect == null)
                return;
            var components = ListPool<Component>.Get();
            rect.GetComponents(typeof(ILayoutElement), components);
            StripDisabledBehavioursFromList(components);
            if (components.Count > 0 || rect.GetComponent(typeof(ILayoutGroup)))
            {
                for (int i = 0; i < rect.childCount; i++)
                {
                    PerformLayoutCalculation(rect.GetChild(i) as RectTransform, action);
                }

                for (int i = 0; i < components.Count; i++)
                {
                    action(components[i]);
                }
            }
            ListPool<Component>.Release(components);
        }

        public static void MarkLayoutForRebuild(RectTransform rect)
        {
            if (rect == null)
                return;

            var comps = ListPool<Component>.Get();
            RectTransform layoutRoot = rect;
            while(true)
            {
                var parent = layoutRoot.parent as RectTransform;
                if (!ValidLayoutGroup(parent, comps))
                    break;
                layoutRoot = parent;
            }

            if(layoutRoot == rect && (!ValidController(layoutRoot, comps)))
            {
                ListPool<Component>.Release(comps);
                return;
            }
            MarkLayoutRootForRebuild(layoutRoot);
            ListPool<Component>.Release(comps);
        }

        private static bool ValidLayoutGroup(RectTransform parent, List<Component> comps)
        {
            if (parent == null)
                return false;
            parent.GetComponents(typeof(ILayoutGroup), comps);
            StripDisabledBehavioursFromList(comps);
            return comps.Count > 0;
        }

        private static bool ValidController(RectTransform layoutRoot, List<Component> comps)
        {
            if (layoutRoot == null)
                return false;
            layoutRoot.GetComponents(typeof(ILayoutController), comps);
            StripDisabledBehavioursFromList(comps);
            return comps.Count > 0;
        }

        private static void MarkLayoutRootForRebuild(RectTransform controller)
        {
            if (controller == null)
                return;
            var rebuilder = s_Rebuilders.Get();
            rebuilder.Initialize(controller);
            if(!CanvasUpdateRegistry.TryRegisterCanvasElementForLayoutRebuild(rebuilder))
                s_Rebuilders.Release(rebuilder);
        }

        public void LayoutComplete()
        {
            s_Rebuilders.Release(this);
        }

        public void GraphicUpdateComplete()
        {
            // do nothing
        }

        public override int GetHashCode()
        {
            return m_CachedHashFromTransform;
        }

        public override string ToString()
        {
            return "(Layout Rebuilder for) " + m_ToRebuild;
        }
    }
}