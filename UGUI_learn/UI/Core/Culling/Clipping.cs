using System.Collections.Generic;

namespace UnityEngine.UI
{
    public static class Clipping
    {
        public static Rect FindCullAndClipWorldRect(List<RectMask2D> rectMaskParents, out bool validRect)
        {
            if (rectMaskParents.Count == 0)
            {
                validRect = false;
                return new Rect();
            }

            var componentRect = rectMaskParents[0].canvasRect;
            for (int i = 0; i < rectMaskParents.Count; i++)
            {
                componentRect = RectIntersect(componentRect, rectMaskParents[i].canvasRect);
            }

            var cull = componentRect.width <= 0 || componentRect.height <= 0;
            if (cull)
            {
                validRect = false;
                return new Rect();
            }

            Vector3 point1 = new Vector3(componentRect.x, componentRect.y, 0.0f);
            Vector3 point2 = new Vector3(componentRect.x + componentRect.width, componentRect.y + componentRect.height, 0.0f);
            validRect = true;
            return new Rect(point1.x, point1.y, point2.x - point1.x, point2.y - point1.y);
        }

        private static Rect RectIntersect(Rect a, Rect b)
        {
            float xMin = Mathf.Max(a.x, b.x);
            float xMax = Mathf.Min(a.x + a.width, b.x + b.width);
            float yMin = Mathf.Max(a.y, b.y);
            float yMax = Mathf.Max(a.y + a.height, b.y + b.height);
            if(xMin <= xMax && yMin <= yMax)
                return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
            
            return Rect.zero;
        }
    }
}