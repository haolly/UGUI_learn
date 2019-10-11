namespace UnityEngine.EventSystem
{
    public class StandaloneInputModule : PointerInputModule
    {
        private float m_PrevActionTime;
        private Vector2 m_LastMoveVector;
        private int m_ConsecutiveMoveCount = 0;

        private Vector2 m_LastMousePosition;
        private Vector2 m_MousePosition;

        private GameObject m_CurrentFocusedGameObject;

        protected StandaloneInputModule()
        {
        }

        [SerializeField] private string m_HorizontalAxis = "Horizontal";
        private string m_VerticalAxis = "Vertical";
        private string m_SubmitButton = "Submit";
        private string m_CancelButton = "Cancel";
        private float m_InputActionsPerSecond = 10;
        private float m_RepeatDelay = 0.5f;
        private bool m_ForceModuleActive;

        public override void Process()
        {
            bool usedEvent = SendUpdateEventToSelectedObject();
            if (eventSystem.sendNavigationEvents)
            {
                if (!usedEvent)
                    usedEvent |= SendMoveEventToSelectedObject();
                if (!usedEvent)
                    SendSubmitEventToSelectedObject();
            }

            if (!ProcessTouchEvent() && input.mousePresent)
            {
                //ProcessMouseEvent();
            }
        }

        private bool ProcessTouchEvent()
        {
            for (int i = 0; i < input.touchCount; i++)
            {
                Touch touch = input.GetTouch(i);
                if(touch.type == TouchType.Indirect)
                    continue;

                bool released;
                bool pressed;
                var pointer = GetTouchPointerEventData(touch, out pressed, out released);
                ProcessTouchPress(pointer, pressed, released);

                if (!released)
                {
                    ProcessMove(pointer);
                    ProcessDrag(pointer);
                }
                else
                {
                    RemovePointerData(pointer);
                }
            }

            //Note, 当前有几个手指在屏幕上面 liuhao
            return input.touchCount > 0;
        }

        protected void ProcessTouchPress(PointerEventData pointerEventData, bool pressed, bool released)
        {
            // TODO pointerCurrentRaycast 为null 的时候怎么办？
            var currentOverGo = pointerEventData.pointerCurrentRaycast.gameObject;
            if (pressed)
            {
                pointerEventData.eligibleForClick = true;
                pointerEventData.delta = Vector2.zero;
                pointerEventData.dragging = false;
                pointerEventData.useDragThreshold = true;
                pointerEventData.pressPosition = pointerEventData.position;
                pointerEventData.pointerPressRaycast = pointerEventData.pointerCurrentRaycast;

                DeselectIfSelectionChanged(currentOverGo, pointerEventData);

                if (pointerEventData.pointerEnter != currentOverGo)
                {
                    HandlePointerExitAndEnter(pointerEventData, currentOverGo);
                    pointerEventData.pointerEnter = currentOverGo;
                }

                var newPress =
                    ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEventData, ExecuteEvents.pointerDownHandler);

                //NOTE, 找不到的时候，设为为clickhandler，这样就可以处理click事件了
                if (newPress == null)
                {
                    newPress = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);
                }

                float time = Time.unscaledTime;
                if (newPress == pointerEventData.lastPress)
                {
                    var diffTime = time - pointerEventData.clickTime;
                    if (diffTime < 0.3f)
                        ++pointerEventData.clickCnt;
                    else
                        pointerEventData.clickCnt = 1;
                }
                else
                {
                    pointerEventData.clickCnt = 1;
                }

                pointerEventData.pointerPress = newPress;
                pointerEventData.rawPointerPress = currentOverGo;
                pointerEventData.clickTime = time;

                pointerEventData.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);

                if (pointerEventData.pointerDrag != null)
                    ExecuteEvents.Execute(pointerEventData.pointerDrag, pointerEventData,
                        ExecuteEvents.initializePotentialDrag);
            }

            if (released)
            {
                ExecuteEvents.Execute(pointerEventData.pointerPress, pointerEventData, ExecuteEvents.pointerUpHandler);

                var pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);
                //Note, 也就是说，如果clickhandler在downhandler的上层，那么clickhandler是不会被触发的, liuhao
                if (pointerEventData.pointerPress == pointerUpHandler && pointerEventData.eligibleForClick)
                {
                    ExecuteEvents.Execute(pointerEventData.pointerPress, pointerEventData,
                        ExecuteEvents.pointerClickHandler);
                }
                else if (pointerEventData.pointerDrag != null && pointerEventData.dragging)
                {
                    ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEventData, ExecuteEvents.dropHandler);
                }

                pointerEventData.eligibleForClick = false;
                pointerEventData.pointerPress = null;
                pointerEventData.rawPointerPress = null;

                if (pointerEventData.pointerDrag != null && pointerEventData.dragging)
                    ExecuteEvents.Execute(pointerEventData.pointerDrag, pointerEventData, ExecuteEvents.endDragHandler);

                pointerEventData.dragging = false;
                pointerEventData.pointerDrag = null;

                ExecuteEvents.ExecuteHierarchy(pointerEventData.pointerEnter, pointerEventData,
                    ExecuteEvents.pointerExitHandler);
                pointerEventData.pointerEnter = null;
            }
        }

        public override void UpdateModule()
        {
            m_LastMousePosition = m_MousePosition;
            m_MousePosition = input.mousePosition;
        }

        public override bool IsModuleSupported()
        {
            return m_ForceModuleActive || input.mousePresent || input.touchSupported;
        }

        public override bool ShouldActiveModule()
        {
            if (!base.ShouldActiveModule())
                return false;

            var shouldActivate = m_ForceModuleActive;
            shouldActivate |= input.GetButtonDown(m_SubmitButton);
            shouldActivate |= input.GetButtonDown(m_CancelButton);
            shouldActivate |= !Mathf.Approximately(input.GetAxisRaw(m_HorizontalAxis), 0.0f);
            shouldActivate |= !Mathf.Approximately(input.GetAxisRaw(m_VerticalAxis), 0.0f);
            shouldActivate |= (m_MousePosition - m_LastMousePosition).sqrMagnitude > 0.0f;
            shouldActivate |= input.GetMouseButtonDown(0);

            if (input.touchCount > 0)
                shouldActivate = true;
            return shouldActivate;
        }

        public override void ActivateModule()
        {
            base.ActivateModule();
            m_MousePosition = input.mousePosition;
            m_LastMousePosition = input.mousePosition;

            var toSelect = eventSystem.currentSelectedGameObject;
            if (toSelect == null)
                toSelect = eventSystem.firstSelectedGameObject;
            
            eventSystem.SetSelectedGameObject(toSelect, GetBaseEventData());
        }

        public override void DeactivateModule()
        {
            base.DeactivateModule();
            ClearSelection();
        }

        private bool SendUpdateEventToSelectedObject()
        {
            if (eventSystem.currentSelectedGameObject == null)
                return false;

            var data = GetBaseEventData();
            ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.updateSelectedHandler);
            return data.used;
        }

        protected bool SendSubmitEventToSelectedObject()
        {
            if (eventSystem.currentSelectedGameObject == null)
                return false;
            var data = GetBaseEventData();
            if (input.GetButtonDown(m_SubmitButton))
                ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.submitHandler);

            if (input.GetButtonDown(m_CancelButton))
                ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.candelHandler);

            return data.used;
        }

        protected bool SendMoveEventToSelectedObject()
        {
            float time = Time.unscaledTime;
            Vector2 movement = GetRawMoveVector();

            if (Mathf.Approximately(movement.x, 0f) && Mathf.Approximately(movement.y, 0f))
            {
                m_ConsecutiveMoveCount = 0;
                return false;
            }

            bool allow = input.GetButtonDown(m_HorizontalAxis) || input.GetButtonDown(m_VerticalAxis);
            bool similarDir = (Vector2.Dot(movement, m_LastMoveVector) > 0);
            if (!allow)
            {
                if (similarDir && m_ConsecutiveMoveCount == 1)
                    allow = time > (m_PrevActionTime + m_RepeatDelay);
                else
                    allow = time > (m_PrevActionTime + 1f / m_InputActionsPerSecond);
            }

            if (!allow)
                return false;


            AxisEventData axisEventData = GetAxisEventData(movement.x, movement.y, 0.6f);
            if (axisEventData.moveDir != MoveDirection.None)
            {
                ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, axisEventData, ExecuteEvents.moveHandler);
                if (!similarDir)
                    m_ConsecutiveMoveCount = 0;
                m_ConsecutiveMoveCount++;
                m_PrevActionTime = time;
                m_LastMoveVector = movement;
            }
            else
            {
                m_ConsecutiveMoveCount = 0;
            }

            return axisEventData.used;
        }

        private Vector2 GetRawMoveVector()
        {
            Vector2 move = Vector2.zero;
            move.x = input.GetAxisRaw(m_HorizontalAxis);
            move.y = input.GetAxisRaw(m_VerticalAxis);
            if (input.GetButtonDown(m_HorizontalAxis))
            {
                if (move.x < 0)
                    move.x = -1f;
                if (move.x > 0)
                    move.x = 1;
            }

            if (input.GetButtonDown(m_VerticalAxis))
            {
                if (move.y < 0)
                    move.y = -1;
                if (move.y > 0)
                    move.y = 1;
            }

            return move;
        }
    }
}