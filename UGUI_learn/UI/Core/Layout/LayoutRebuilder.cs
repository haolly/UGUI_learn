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
            RectTransform.reapplyDrivenProperties += ReapplyDrivenProperties
        }

        static void ReapplyDrivenProperties(RectTransform driven)
        {
            MarkLayoutForRebuild(driven);
        }
        //todo

        static void StripDisabledBehavioursFromList(List<Component> components)
        {
            components.RemoveAll(e => e is Behaviour && !((Behaviour) e).isActiveAndEnabled);
        }

        public static void MarkLayoutForRebuilder(RectTransform rect)
        {
            if (rect == null)
                return;
            var comps = ListPool<Component>.Get();
            
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
        
        public void Rebuild(CanvasUpdate executing)
        {
            switch (executing)
            {
                case CanvasUpdate.Layout:
                    PerformLayoutCalculation(m_ToRebuild, e => (e as ILayoutElement).CalculateLayoutInputHorizontal());
                    //todo
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
                    PerformLayoutControl(rect.GetChild(i));
                }
            }
        }

        public Transform transform { get; }
        public void LayoutComplete()
        {
            throw new System.NotImplementedException();
        }

        public void GraphicUpdateComplete()
        {
            throw new System.NotImplementedException();
        }

        public bool IsDestroyed()
        {
            throw new System.NotImplementedException();
        }
    }
}