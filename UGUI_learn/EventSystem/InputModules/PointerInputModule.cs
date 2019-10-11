using System.Collections.Generic;

namespace UnityEngine.EventSystem
{
    public abstract class PointerInputModule : BaseInputModule
    {
        public const int kMouseLeftId = -1;
        public const int kMouseRightId = -2;
        public const int kMouseMiddleId = -3;

        public const int kFakeTouchesId = -4;
        
        protected Dictionary<int, PointerEventData> m_PointerData = new Dictionary<int, PointerEventData>();

        protected bool GetPointerData(int id, out PointerEventData data, bool create)
        {
            if (!m_PointerData.TryGetValue(id, out data) && create)
            {
                data = new PointerEventData(eventSystem)
                {
                    pointerId = id,
                };
                m_PointerData.Add(id, data);
                return true;
            }

            return false;
        }

        protected void RemovePointerData(PointerEventData data)
        {
            m_PointerData.Remove(data.pointerId);
        }

        protected PointerEventData GetTouchPointerEventData(Touch input, out bool pressed, out bool released)
        {
            PointerEventData pointerData;
            var created = GetPointerData(input.fingerId, out pointerData, true);
            pointerData.Reset();

            pressed = created || (input.phase == TouchPhase.Began);
            released = (input.phase == TouchPhase.Canceled) || (input.phase == TouchPhase.Ended);
            if (created)
            {
                pointerData.position = input.position;
            }

            if (pressed)
            {
                pointerData.delta = Vector2.zero;
            }
            else
            {
                pointerData.delta = input.position - pointerData.position;
            }

            pointerData.position = input.position;
            pointerData.button = PointerEventData.InputButton.Left;
            eventSystem.RaycastAll(pointerData, m_RaycastResultCache);

            // NOTE: find the most depth one 
            var raycast = FindFirstRaycast(m_RaycastResultCache);
            // TODO: 这里有可能是null ?
            pointerData.pointerCurrentRaycast = raycast;
            m_RaycastResultCache.Clear();
            return pointerData;
        }

        private static bool ShouldStartDrag(Vector2 pressPos, Vector2 currentPos, float threshold,
            bool useDragThreshold)
        {
            if (!useDragThreshold)
                return true;
            return (currentPos - pressPos).sqrMagnitude >= threshold * threshold;
        }

        protected virtual void ProcessMove(PointerEventData pointerEventData)
        {
            var targetGo = (Cursor.lockState == CursorLockMode.Locked
                ? null
                : pointerEventData.pointerCurrentRaycast.gameObject);
            HandlePointerExitAndEnter(pointerEventData, targetGo);
        }

        protected virtual void ProcessDrag(PointerEventData pointerEventData)
        {
            if (!pointerEventData.IsPointerMoving() || Cursor.lockState == CursorLockMode.Locked ||
                pointerEventData.pointerDrag == null)
                return;

            if (!pointerEventData.dragging && ShouldStartDrag(pointerEventData.pressPosition, pointerEventData.position,
                    eventSystem.pixelDragThresHold, pointerEventData.useDragThreshold))
            {
                ExecuteEvents.Execute(pointerEventData.pointerDrag, pointerEventData, ExecuteEvents.beginDragHandler);
                pointerEventData.dragging = true;
            }

            if (pointerEventData.dragging)
            {
                // Before dong drag we should cancel any pointer down state 
                // And clear selection
                if (pointerEventData.pointerPress != pointerEventData.pointerDrag)
                {
                    ExecuteEvents.Execute(pointerEventData.pointerPress, pointerEventData,
                        ExecuteEvents.pointerUpHandler);
                    pointerEventData.eligibleForClick = false;
                    pointerEventData.pointerPress = null;
                    pointerEventData.rawPointerPress = null;
                }

                ExecuteEvents.Execute(pointerEventData.pointerDrag, pointerEventData, ExecuteEvents.dragHandler);
            }
        }

        protected void DeselectIfSelectionChanged(GameObject currentOverGo, BaseEventData pointerEvent)
        {
            var selectHandlerGo = ExecuteEvents.GetEventHandler<ISelectHandler>(currentOverGo);
            if(selectHandlerGo != eventSystem.currentSelectedGameObject)
                eventSystem.SetSelectedGameObject(null, pointerEvent);
        }

        protected void ClearSelection()
        {
            var baseEventData = GetBaseEventData();
            foreach (var pointerDataValue in m_PointerData.Values)
            {
                HandlePointerExitAndEnter(pointerDataValue, null);
            }
            m_PointerData.Clear();
            eventSystem.SetSelectedGameObject(null, baseEventData);
        }
    }
}