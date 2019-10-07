using System;
using System.Collections.Generic;
using UnityEngine.EventSystem;

namespace UnityEngine.UI
{
    public class GraphicRaycaster : BaseRaycaster
    {
        protected const int kNoEventMaskSet = -1;

        public enum BlockingObjects
        {
            None = 0,
            TwoD = 1,
            TreeD = 2,
            ALL = 3,
        }

        public override int sortOrderPriority
        {
            get
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    return canvas.rootCanvas.renderOrder;

                return base.renderOrderPriority;
            }
        }

        [SerializeField] private bool m_IgnoreReversedGraphics = true;
        private BlockingObjects m_BlockingObjects = BlockingObjects.None;

        public bool ignoreReversedGraphics
        {
            get => m_IgnoreReversedGraphics;
            set => m_IgnoreReversedGraphics = value;
        }

        public BlockingObjects blockingObjects
        {
            get => m_BlockingObjects;
            set => m_BlockingObjects = value;
        }

        protected LayerMask m_BlockingMask = kNoEventMaskSet;

        private Canvas m_Canvas;

        private Canvas canvas
        {
            get
            {
                if (m_Canvas != null)
                    return m_Canvas;

                m_Canvas = GetComponent<Canvas>();
                return m_Canvas;
            }
        }

        protected GraphicRaycaster()
        {
        }
        
        [NonSerialized]
        private List<Graphic> m_RaycastResults = new List<Graphic>();

        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            if (canvas == null)
                return;

            int displayIndex;
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay || !eventCamera)
                displayIndex = canvas.targetDisplay;
            else
                displayIndex = eventCamera.targetDisplay;

            var eventPosition = Display.RelativeMouseAt(eventData.position);
            if (eventPosition != Vector3.zero)
            {
                int eventDisplayIndex = (int) eventPosition.z;
                if (eventDisplayIndex != displayIndex)
                    return;
            }
            else
            {
                // when multiple display is not supported
                eventPosition = eventData.position;
            }
            
            // Convert to view space
            Vector2 pos;
            if (eventCamera == null)
            {
                float w = Screen.width;
                float h = Screen.height;
                if (displayIndex > 0 && displayIndex < Display.displays.Length)
                {
                    w = Display.displays[displayIndex].systemWidth;
                    h = Display.displays[displayIndex].systemHeight;
                }

                pos = new Vector2(eventPosition.x / w, eventPosition.y / h);
            }
            else
                pos = eventCamera.ScreenToViewportPoint(eventPosition);

            // outside the camera's viewport ?
            if (pos.x < 0f || pos.x > 1f || pos.y < 0f || pos.y > 1f)
                return;

            float hitDistance = float.MaxValue;
            Ray ray = new Ray();
            if (eventCamera != null)
                ray = eventCamera.ScreenPointToRay(eventPosition);
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && blockingObjects != BlockingObjects.None)
            {
                float dist = 100.0f;
                if (eventCamera != null)
                    dist = eventCamera.farClipPlane - eventCamera.nearClipPlane;
                if (blockingObjects == BlockingObjects.TreeD || blockingObjects == BlockingObjects.ALL)
                {
                    if(ReflectionMethodsCache.Singleton.raycast3D != null)
                    {
                        RaycastHit hit;
                        if (ReflectionMethodsCache.Singleton.raycast3D(ray, out hit, dist, m_BlockingMask))
                        {
                            hitDistance = hit.distance;
                        }
                    }
                }

                if (blockingObjects == BlockingObjects.TwoD || blockingObjects == BlockingObjects.ALL)
                {
                    if (ReflectionMethodsCache.Singleton.raycast2D != null)
                    {
                        var hit = ReflectionMethodsCache.Singleton.raycast2D(ray.origin, ray.direction, dist,
                            m_BlockingMask);
                        if (hit.collider)
                            hitDistance = hit.fraction * dist;
                    }
                }
            }
            m_RaycastResults.Clear();
            Raycast(canvas, eventCamera, eventPosition, m_RaycastResults);
            for (int i = 0; i < m_RaycastResults.Count; i++)
            {
                var go = m_RaycastResults[i].gameObject;
                bool appendGraphic = true;
                if (ignoreReversedGraphics)
                {
                    if (eventCamera == null)
                    {
                        // if we dont have a camera, we know that we should always be facing forward
                        var dir = go.transform.rotation * Vector3.forward;
                        appendGraphic = Vector3.Dot(Vector3.forward, dir) > 0;
                    }
                    else
                    {
                        var cameraForward = eventCamera.transform.rotation * Vector3.forward;
                        var dir = go.transform.rotation * Vector3.forward;
                        appendGraphic = Vector3.Dot(cameraForward, dir) > 0;
                    }
                }

                if (appendGraphic)
                {
                    float distance = 0;
                    if (eventCamera == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                        distance = 0;
                    else
                    {
                        Transform trans = go.transform;
                        Vector3 transForward = trans.forward;
                        //http://geomalgorithms.com/a06-_intersect-2.html 
                        distance = (Vector3.Dot(transForward, trans.position - ray.origin) /
                                    Vector3.Dot(transForward, ray.direction));
                        
                        // Check to see if the go is behind the camera.
                        if(distance < 0)
                            continue;
                    }
                    
                    //TODO 这里是干啥？
                    if(distance >= hitDistance)
                        continue;

                    var castResult = new RaycastResult
                    {
                        gameObject = go,
                        module = this,
                        distance = distance,
                        screenPosition = eventPosition,
                        index = resultAppendList.Count,
                        depth = m_RaycastResults[i].depth,
                        sortingLayer = canvas.sortingLayerID,
                        sortingOrder = canvas.sortingOrder
                    };
                    resultAppendList.Add(castResult);
                }
            }
        }

        public override Camera eventCamera
        {
            get
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay ||
                    (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null))
                    return null;

                return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
            }
        }
        
        static readonly List<Graphic> s_SortedGraphics = new List<Graphic>();

        private static void Raycast(Canvas canvas, Camera eventCamera, Vector2 pointerPosition, List<Graphic> results)
        {
            var foundGraphics = GraphicRegistry.GetGraphicsForCanvas(canvas);
            for (int i = 0; i < foundGraphics.Count; i++)
            {
                Graphic graphic = foundGraphics[i];
                if(graphic.canvasRenderer.cull)
                    continue;
                
                // -1 means it hasn't been processed by the canvas
                if(graphic.depth == -1 || !graphic.raycastTarget)
                    continue;
                
                if(!RectTransformUtility.RectangleContainsScreenPoint(graphic.rectTransform, pointerPosition, eventCamera))
                    continue;
                if (graphic.Raycast(pointerPosition, eventCamera))
                {
                    s_SortedGraphics.Add(graphic);
                }
            }
            
            // todo, ascent?
            s_SortedGraphics.Sort((g1, g2) => g2.depth.CompareTo(g1.depth));
            for (int i = 0; i < s_SortedGraphics.Count; i++)
            {
                results.Add(s_SortedGraphics[i]);
            }
            s_SortedGraphics.Clear();
        }
    }
}