using System.Collections.Generic;
using UnityEngine.VR.WSA.Persistence;

namespace UnityEngine.UI
{
    public class MaskUtilities
    {
        public static void Notify2DMaskStageChanged(Component mask)
        {
            var components = ListPool<Component>.Get();
            mask.GetComponentsInChildren(components);
            for (int i = 0; i < components.Count; i++)
            {
                if(components[i] == null || components[i].gameObject == mask.gameObject)
                    continue;

                var toNotify = components[i] as IClippable;
                if(toNotify != null)
                    toNotify.RecalculateClipping();
            }
            ListPool<Component>.Release(components);
        }
        
        public static void NotifyStencilStateChanged(Component mask)
        {
            var components = ListPool<Component>.Get();
            mask.GetComponentsInChildren(components);
            for (int i = 0; i < components.Count; i++)
            {
                if(components[i] == null || components[i].gameObject == mask.gameObject)
                    continue;
                var toNotify = components[i] as IMaskable;
                if(toNotify != null)
                    toNotify.RecalculateMasking();
            }
            ListPool<Component>.Release(components);
        }

        public static Transform FindRootSortOverrideCanvas(Transform start)
        {
            var canvasList = ListPool<Canvas>.Get();
            start.GetComponentsInParent(false, canvasList);
            Canvas canvas = null;

            for (int i = 0; i < canvasList.Count; i++)
            {
                canvas = canvasList[i];
                if (canvas.overrideSorting)
                    break;
            }
            ListPool<Canvas>.Release(canvasList);
            return canvas != null ? canvas.transform : null;
        }

        public static int GetStencilDepth(Transform transform, Transform stopAfter)
        {
            var depth = 0;
            if (transform == stopAfter)
                return depth;

            var t = transform.parent;
            var components = ListPool<Mask>.Get();
            while (t != null)
            {
                t.GetComponents<Mask>(components);
                for (int i = 0; i < components.Count; i++)
                {
                    if (components[i] != null && components[i].MaskEnabled() && components[i].graphic.IsActive())
                    {
                        // todo, 每个父亲节点增加一个depth ？
                        ++depth;
                        break;
                    }
                }
                if(t == stopAfter)
                    break;

                t = t.parent;
            }
            ListPool<Mask>.Release(components);
            return depth;
        }

        public static bool IsDescendantOfSelf(Transform father, Transform child)
        {
            if (father == null || child == null)
                return false;
            if (father == child)
                return true;
            while (child.parent != null)
            {
                if (child.parent == father)
                    return true;
                child = child.parent;
            }

            return false;
        }

        public static RectMask2D GetRectMaskForClippable(IClippable clippable)
        {
           //todo 
        }

        public static void GetRectMaskForClip(RectMask2D clipper, List<RectMask2D> masks)
        {
            masks.Clear();
            List<Canvas> canvasComponents = ListPool<Canvas>.Get();
            List<RectMask2D> rectMaskComponents = ListPool<RectMask2D>.Get();
            clipper.transform.GetComponentsInParent(false, rectMaskComponents);

            if (rectMaskComponents.Count > 0)
            {
                clipper.transform.GetComponentsInParent(false, canvasComponents);
                for (int i = rectMaskComponents.Count - 1; i >= 0; i--)
                {
                    if(!rectMaskComponents[i].IsActive())
                        continue;
                    bool shouldAdd = true;
                    // note, 如果能够找到一个 mask 的孩子节点，有canvas并且overrideSorting==true，那么这个mask就不起作用
                    for (int j = canvasComponents.Count - 1; j >= 0; j--)
                    {
                        if ((!IsDescendantOfSelf(canvasComponents[j].transform, rectMaskComponents[i].transform))&&
                            canvasComponents[j].overrideSorting)
                        {
                            shouldAdd = false;
                            break;
                        }
                    }
                    if(shouldAdd)
                        masks.Add(rectMaskComponents[i]);
                }
            }
            ListPool<Canvas>.Release(canvasComponents);
            ListPool<RectMask2D>.Release(rectMaskComponents);
        }
    }
}