using System.Collections;
using System.Configuration;
using UnityEngine.Events;
using UnityEngine.EventSystem;
using UnityEngine.WiiU;

namespace UnityEngine.UI
{
    public class Scrollbar : Selectable, IBeginDragHandler, IDragHandler, IInitializePotentialDragHandler, ICanvasElement    
    {
        public enum Direction
        {
            LeftToRight,
            RightToLeft,
            BottomToTop,
            TopToBottom,
        }
        
        public class ScrollEvent : UnityEvent<float> {}

        private RectTransform m_HandleRect;

        public RectTransform HandleRect
        {
            get => m_HandleRect;
            set
            {
                if (SetPropertyUtility.SetClass(ref m_HandleRect, value))
                {
                    UpdateCachedReferences();
                    UpdateVisuals();
                }
            }
        }
        private ScrollEvent m_OnValueChanged = new ScrollEvent();

        public ScrollEvent OnValueChanged
        {
            get => m_OnValueChanged;
            set => m_OnValueChanged = value;
        }

        private RectTransform m_ContainerRect;

        private Direction m_Direction = Direction.LeftToRight;

        public Direction direction
        {
            get { return m_Direction; }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_Direction, value))
                {
                    UpdateVisuals();
                }
            }
        }
        
        private float m_Size = 0.2f;
        public float Size
        {
            get => m_Size;
            set
            {
                if(SetPropertyUtility.SetStruct(ref m_Size, Mathf.Clamp01(value)))
                    UpdateVisuals();
            }
        }
        
        private float m_Value;
        public float value
        {
            get
            {
                float value = m_Value;
                // Note, clamp to (N *  1/(steps -1))
                if (m_NumberOfSteps > 1)
                    value = Mathf.Round(value * (NumberOfSteps - 1)) / (NumberOfSteps - 1);
                return value;
            }
            set { Set(value); }
        }

        private int m_NumberOfSteps = 0;
        public int NumberOfSteps
        {
            get => m_NumberOfSteps;
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_NumberOfSteps, value))
                {
                    Set(m_Value);
                    UpdateVisuals();
                }
            }
        }


        private Vector2 m_Offset = Vector2.zero;

        float stepSize
        {
            get { return (m_NumberOfSteps > 1 ? 1f / (NumberOfSteps - 1) : 0.1f); }
        }
        private DrivenRectTransformTracker m_Tracker;
        private Coroutine m_PointerDownRepeat;
        private bool isPointerDownAndNotDragging = false;

        private RectTransform.Axis axis
        {
            get {
                return ((m_Direction == Direction.LeftToRight || m_Direction == Direction.RightToLeft)
                    ? RectTransform.Axis.Horizontal
                    : RectTransform.Axis.Vertical);
            }
        }
        bool reverseValue
        {
            get { return m_Direction == Direction.RightToLeft || m_Direction == Direction.BottomToTop; }
        }
        
        protected Scrollbar()
        {}

        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateCachedReferences();
            Set(m_Value, false);
            UpdateVisuals();
        }

        protected override void OnDisable()
        {
            m_Tracker.Clear();
            base.OnDisable();
        }

        void UpdateCachedReferences()
        {
            if (m_HandleRect && m_HandleRect.parent != null)
                m_ContainerRect = m_HandleRect.parent.GetComponent<RectTransform>();
            else
                m_ContainerRect = null;
        }

        void Set(float input)
        {
            Set(input, true);
        }

        void Set(float input, bool sendCallback)
        {
            float currentValue = m_Value;
            m_Value = Mathf.Clamp01(input);
            if (currentValue == value)
                return;
            UpdateVisuals();
            if (sendCallback)
                m_OnValueChanged.Invoke(value);
        }

        private void UpdateVisuals()
        {
            m_Tracker.Clear();
            if (m_ContainerRect != null)
            {
                m_Tracker.Add(this, m_HandleRect, DrivenTransformProperties.Anchors);
                Vector2 anchorMin = Vector2.zero;
                Vector2 anchorMax = Vector2.one;

                float movement = value * (1 - Size);
                if (reverseValue)
                {
                    anchorMin[(int) axis] = 1 - movement - Size;
                    anchorMax[(int) axis] = 1 - movement;
                }
                else
                {
                    anchorMin[(int) axis] = movement;
                    anchorMax[(int) axis] = movement + Size;
                }

                m_HandleRect.anchorMin = anchorMin;
                m_HandleRect.anchorMax = anchorMax;
            }
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            if (!IsActive())
                return;
            UpdateVisuals();
        }

        private bool MayDrag(PointerEventData eventData)
        {
            return IsActive() && IsInteractable() && eventData.button == PointerEventData.InputButton.Left;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            isPointerDownAndNotDragging = false;
            if (!MayDrag(eventData))
                return;
            if (m_ContainerRect == null)
                return;

            m_Offset = Vector2.zero;
            if (RectTransformUtility.RectangleContainsScreenPoint(m_HandleRect, eventData.position,
                eventData.enterEventCamera))
            {
                Vector2 localMousePos;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_HandleRect, eventData.position,
                    eventData.pressEventCamera, out localMousePos))
                {
                    m_Offset = localMousePos - m_HandleRect.rect.center;
                }
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
                return;
            if (m_ContainerRect != null)
                UpdateDrag(eventData);
        }

        void UpdateDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;
            
            if(m_ContainerRect == null)
                return;

            Vector2 localCursor;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(m_ContainerRect, eventData.position,
                eventData.pressEventCamera, out localCursor))
            {
                return;
            }

            //TODO, 看不懂
            Vector2 handleCenterRelativeToContainerCorner = localCursor - m_Offset - m_ContainerRect.rect.position;
            Vector2 handleCorner = handleCenterRelativeToContainerCorner -
                                   (m_HandleRect.rect.size - m_HandleRect.sizeDelta) * 0.5f;
            float parentSize = axis == 0 ? m_ContainerRect.rect.width : m_ContainerRect.rect.height;
            float remainingSize = parentSize * (1 - Size);
            if(remainingSize <= 0)
                return;

            switch (m_Direction)
            {
                case Direction.LeftToRight:
                    Set(handleCorner.x / remainingSize);
                    break;
                case Direction.RightToLeft:
                    Set(1f - (handleCorner.x/ remainingSize));
                    break;
                case Direction.BottomToTop:
                    Set(handleCorner.y/ remainingSize);
                    break;
                case Direction.TopToBottom:
                    Set(1f - (handleCorner.y / remainingSize));
                    break;
            }
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
                return;
            base.OnPointerDown(eventData);
            isPointerDownAndNotDragging = true;
            m_PointerDownRepeat = StartCoroutine(ClickRepeat(eventData));
        }

        protected IEnumerator ClickRepeat(PointerEventData eventData)
        {
            while (isPointerDownAndNotDragging)
            {
                if (!RectTransformUtility.RectangleContainsScreenPoint(m_HandleRect, eventData.position,
                    eventData.enterEventCamera))
                {
                    Vector2 localMousePos;
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_HandleRect, eventData.position,
                        eventData.pressEventCamera, out localMousePos))
                    {
                        var axisCoordinate = axis == 0 ? localMousePos.x : localMousePos.y;
                        if (axisCoordinate < 0)
                            value -= Size;
                        else
                            value += Size;
                    }
                }
                yield return new WaitForEndOfFrame();
            }
            StopCoroutine(m_PointerDownRepeat);
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            isPointerDownAndNotDragging = false;
        }

        public override void OnMove(AxisEventData eventData)
        {
            if (!IsActive() || !IsInteractable())
            {
                base.OnMove(eventData);
                return;
            }

            switch (eventData.moveDir)
            {
                case MoveDirection.Left:
                    if(axis == RectTransform.Axis.Horizontal && FindSelectableOnLeft() == null)
                        Set(reverseValue ? value + stepSize : value - stepSize);
                    else
                        base.OnMove(eventData);
                    break;
            }
        }

        public override Selectable FindSelectableOnLeft()
        {
            
        }


        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            eventData.useDragThreshold = false;
        }

        public void Rebuild(CanvasUpdate executing)
        {
        }

        public void LayoutComplete()
        {
        }

        public void GraphicUpdateComplete()
        {
        }
    }
}