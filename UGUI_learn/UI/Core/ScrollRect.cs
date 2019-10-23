using System;
using System.Data;
using UnityEngine.Events;
using UnityEngine.EventSystem;

namespace UnityEngine.UI
{
    public class ScrollRect : UIBehaviour,
        IInitializePotentialDragHandler, IBeginDragHandler, IEndDragHandler,
        IDragHandler, IScrollHandler, ICanvasElement, ILayoutElement, ILayoutGroup
    {
        public enum MovementType
        {
            Unrestricted,
            Elastic,
            Clamped,
        }

        public enum ScrollbarVisibility
        {
            Permanent,
            AutoHide,
            AutoHideAndExpandViewport,
        }
        
        public class ScrollRectEvent : UnityEvent<Vector2> {}


        private bool m_Horizontal = true;

        public bool Horizontal
        {
            get => m_Horizontal;
            set => m_Horizontal = value;
        }

        private bool m_Vertical = true;

        public bool Vertical
        {
            get => m_Vertical;
            set => m_Vertical = value;
        }

        private MovementType m_MovementType = MovementType.Elastic;

        public MovementType movementType
        {
            get => m_MovementType;
            set => m_MovementType = value;
        }

        private float m_Elasticity = 0.1f;

        public float Elasticity
        {
            get => m_Elasticity;
            set => m_Elasticity = value;
        }

        private bool m_Inertia = true;

        public bool Inertia
        {
            get => m_Inertia;
            set => m_Inertia = value;
        }

        private float m_DecelerationRate = 0.135f;

        public float DecelerationRate
        {
            get => m_DecelerationRate;
            set => m_DecelerationRate = value;
        }

        private float m_ScrollSensitivity = 1.0f;

        public float ScrollSensitivity
        {
            get => m_ScrollSensitivity;
            set => m_ScrollSensitivity = value;
        }

        private RectTransform m_ViewPort;

        public RectTransform ViewPort
        {
            get => m_ViewPort;
            set
            {
                m_ViewPort = value;
                SetDirtyCaching();
            }
        }

        private Scrollbar m_HorizontalScrollbar;

        public Scrollbar horizontalScrollbar
        {
            get { return m_HorizontalScrollbar; }
            set
            {
                if(m_HorizontalScrollbar)
                    m_HorizontalScrollbar.OnValueChanged.RemoveListener(SetHorizontalNormalizedPosition);
                m_HorizontalScrollbar = value;
                if(m_HorizontalScrollbar)
                    m_HorizontalScrollbar.OnValueChanged.AddListener(SetHorizontalNormalizedPosition);

                SetDirtyCaching();
            }
        }

        private Scrollbar m_VerticalScrollbar;

        public Scrollbar verticalScrollbar
        {
            get { return m_VerticalScrollbar; }
            set
            {
                if(m_VerticalScrollbar)
                    m_VerticalScrollbar.OnValueChanged.RemoveListener(SetVerticalNormalizedPosition);
                m_VerticalScrollbar = value;
                if(m_VerticalScrollbar)
                    m_VerticalScrollbar.OnValueChanged.AddListener(SetVerticalNormalizedPosition);

                SetDirtyCaching();
            }
        }
        
        private ScrollRectEvent m_OnValueChanged = new ScrollRectEvent();

        public ScrollRectEvent onValueChanged
        {
            get { return m_OnValueChanged; }
            set { m_OnValueChanged = value; }
        }

        private Vector2 m_PointerStartLocalCursor = Vector2.zero;
        protected Vector2 m_ContentStartPosition = Vector2.zero;

        private RectTransform m_ViewRect;

        protected RectTransform viewRect
        {
            get
            {
                if (m_ViewRect == null)
                    m_ViewRect = m_ViewPort;
                if (m_ViewRect == null)
                    m_ViewRect = (RectTransform) transform;
                return m_ViewRect;
            }
        }
        private RectTransform m_Content;

        public RectTransform Content
        {
            get => m_Content;
            set => m_Content = value;
        }

        protected Bounds m_ContentBounds;
        private Bounds m_ViewBounds;
        //todo

        private RectTransform m_Rect;
        private RectTransform rectTransform
        {
            get
            {
                if (m_Rect == null)
                    m_Rect = GetComponent<RectTransform>();
                return m_Rect;
            }
        }

        private RectTransform m_HorizontalScrollbarRect;
        private RectTransform m_VerticalScrollbarRect;

        private Vector2 m_Velocity;

        public Vector2 Velocity
        {
            get => m_Velocity;
            set => m_Velocity = value;
        }

        private bool m_Dragging;

        private Vector2 m_PrevPosition = Vector2.zero;
        private Bounds m_PrevContentBounds;
        private Bounds m_PrevViewBounds;
        private bool m_HasRebuiltLayout = false;

        private DrivenRectTransformTracker m_Tracker;

        protected ScrollRect()
        {
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if(m_HorizontalScrollbar)
                m_HorizontalScrollbar.OnValueChanged.AddListener(SetHorizontalNormalizedPosition);
            if(m_VerticalScrollbar)
                m_VerticalScrollbar.OnValueChanged.AddListener(SetVerticalNormalizedPosition);

            CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);
        }

        protected override void OnDisable()
        {
            CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(this);
            if(m_HorizontalScrollbar)
                m_HorizontalScrollbar.OnValueChanged.RemoveListener(SetHorizontalNormalizedPosition);
            if(m_VerticalScrollbar)
                m_VerticalScrollbar.OnValueChanged.RemoveListener(SetVerticalNormalizedPosition);

            m_HasRebuiltLayout = false;
            m_Tracker.Clear();
            //todo
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
            base.OnDisable();
        }

        public override bool IsActive()
        {
            return base.IsActive() && m_Content != null;
        }

        public virtual void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;
            m_Velocity = Vector2.zero;
        }

        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;
            if (!IsActive())
                return;
            UpdateBounds();
            m_PointerStartLocalCursor = Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewRect, eventData.position,
                eventData.pressEventCamera,
                out m_PointerStartLocalCursor);
            m_ContentStartPosition = m_Content.anchoredPosition;
            m_Dragging = true;
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;
            m_Dragging = false;
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;
            if (!IsActive())
                return;

            Vector2 localCursor;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewRect, eventData.position,
                eventData.pressEventCamera, out localCursor))
                return;
            UpdateBounds();
            var pointerDelta = localCursor - m_PointerStartLocalCursor;
            Vector2 position = m_ContentStartPosition + pointerDelta;

            Vector2 offset = CalculateOffset(position - m_Content.anchoredPosition);
            position += offset;

            if (movementType == MovementType.Elastic)
            {
                if (offset.x != 0)
                    position.x = position.x - RubberDelta(offset.x, m_ViewBounds.size.x);
                if (offset.y != 0)
                    position.y = position.y - RubberDelta(offset.y, m_ViewBounds.size.y);
            }

            SetContentAnchoredPosition(position);
        }

        protected virtual void SetContentAnchoredPosition(Vector2 position)
        {
            if (!m_Horizontal)
                position.x = m_Content.anchoredPosition.x;
            if (!m_Vertical)
                position.y = m_Content.anchoredPosition.y;

            if (position != m_Content.anchoredPosition)
            {
                m_Content.anchoredPosition = position;
                UpdateBounds();
            }
        }

        private Vector2 CalculateOffset(Vector2 delta)
        {
            return InternalCalculateOffset(ref m_ViewBounds, ref m_ContentBounds, m_Horizontal, m_Vertical,
                m_MovementType,
                ref delta);
        }

        /// <summary>
        /// Offset to get content into place in the view
        /// </summary>
        /// <param name="viewBounds"></param>
        /// <param name="contentBounds"></param>
        /// <param name="horizontal"></param>
        /// <param name="vertical"></param>
        /// <param name="movementType"></param>
        /// <param name="delta"></param>
        /// <returns></returns>
        internal static Vector2 InternalCalculateOffset(ref Bounds viewBounds, ref Bounds contentBounds,
            bool horizontal,
            bool vertical, MovementType movementType, ref Vector2 delta)
        {
            Vector2 offset = Vector2.zero;
            if (movementType == MovementType.Unrestricted)
                return offset;

            Vector2 min = contentBounds.min;
            Vector2 max = contentBounds.max;

            if (horizontal)
            {
                min.x += delta.x;
                max.x += delta.x;
                if (min.x > viewBounds.min.x)
                    offset.x = viewBounds.min.x - min.x;
                else if (max.x < viewBounds.max.x)
                    offset.x = viewBounds.max.x - max.x;
            }

            if (vertical)
            {
                min.y += delta.y;
                max.y += delta.y;
                if (max.y < viewBounds.max.y)
                    offset.y = viewBounds.max.y - max.y;
                else if (min.y > viewBounds.min.y)
                    offset.y = viewBounds.min.y - min.y;
            }

            return offset;
        }

        public virtual void OnScroll(PointerEventData eventData)
        {
            if (!IsActive())
                return;
            
            EnsureLayoutHasRebuilt();
            UpdateBounds();

            Vector2 delta = eventData.scrollDelta;
            //todo
        }

        public virtual void Rebuild(CanvasUpdate executing)
        {
            if (executing == CanvasUpdate.PreLayout)
            {
                UpdateCachedData();
            }

            if (executing == CanvasUpdate.PostLayout)
            {
                UpdateBounds();
                UpdateScrollbars(Vector2.zero);
                UpdatePrevData();
                m_HasRebuiltLayout = true;
            }
        }

        protected void UpdatePrevData()
        {
            if (m_Content == null)
                m_PrevPosition = Vector2.zero;
            else
                m_PrevPosition = m_Content.anchoredPosition;
            m_PrevViewBounds = m_ViewBounds;
            m_PrevContentBounds = m_ContentBounds;
        }

        private void UpdateScrollbars(Vector2 offset)
        {
            if (m_HorizontalScrollbar)
            {
                //TODO, 这里是不是应该为 m_ContentBounds.size.x - Mathf.Abs(offset.x)
                if (m_ContentBounds.size.x > 0)
                    m_HorizontalScrollbar.Size =
                        Mathf.Clamp01((m_ViewBounds.size.x - Mathf.Abs(offset.x)) / m_ContentBounds.size.x);
                else
                    m_HorizontalScrollbar.Size = 1;

                m_HorizontalScrollbar.value = horizontalNormalizedPosition;
            }

            if (m_VerticalScrollbar)
            {
                if (m_ContentBounds.size.y > 0)
                    m_VerticalScrollbar.Size =
                        Mathf.Clamp01((m_ViewBounds.size.y - Mathf.Abs(offset.y)) / m_ContentBounds.size.y);
                else
                    m_VerticalScrollbar.Size = 1;

                m_VerticalScrollbar.value = verticalNormalizedPosition;
            }
        }

        public float horizontalNormalizedPosition
        {
            get
            {
                UpdateBounds();
                if (m_ContentBounds.size.x <= m_ViewBounds.size.x)
                    return (m_ViewBounds.min.x > m_ContentBounds.min.x) ? 1 : 0;
                
                //NOTE: offset / scrollable size
                return (m_ViewBounds.min.x - m_ContentBounds.min.x) / (m_ContentBounds.size.x - m_ViewBounds.size.x);
            }
            set
            {
                SetNormalizedPosition(value, 0);
            }
        }

        public float verticalNormalizedPosition
        {
            get
            {
                UpdateBounds();
                if (m_ContentBounds.size.y <= m_ViewBounds.size.y)
                    return m_ViewBounds.min.y > m_ContentBounds.min.y ? 1 : 0;
                return (m_ViewBounds.min.y - m_ContentBounds.min.y) / (m_ContentBounds.size.y - m_ViewBounds.size.y);
            }
            set
            {
                SetNormalizedPosition(value, 1);
            }
        }

        void UpdateCachedData()
        {
            Transform transform = this.transform;
            m_HorizontalScrollbarRect =
                m_HorizontalScrollbar == null ? null : m_HorizontalScrollbar.transform as RectTransform;
            m_VerticalScrollbarRect =
                m_VerticalScrollbar == null ? null : m_VerticalScrollbar.transform as RectTransform;

            bool viewIsChild = (viewRect.parent == transform);
            bool hScrollbarIsChild = (!m_HorizontalScrollbarRect || m_HorizontalScrollbarRect.parent == transform);
            bool vScrollbarIsChild = (!m_VerticalScrollbarRect || m_VerticalScrollbarRect.parent == transform);
            bool allAreChildren = (viewIsChild && hScrollbarIsChild && vScrollbarIsChild);
            
            //todo
        }
        
        private readonly Vector3[] m_Corners = new Vector3[4];

        private Bounds GetBounds()
        {
            if(m_Content == null)
                return new Bounds();
            m_Content.GetWorldCorners(m_Corners);
            var viewWorldToLocalMatrix = viewRect.worldToLocalMatrix;
            return InternalGetBounds(m_Corners, ref viewWorldToLocalMatrix);
        }

        internal static Bounds InternalGetBounds(Vector3[] corners, ref Matrix4x4 viewWorldToLocalMatrix)
        {
            var vMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var vMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            for (int i = 0; i < 4; i++)
            {
                Vector3 v = viewWorldToLocalMatrix.MultiplyPoint3x4(corners[i]);
                vMin = Vector3.Min(v, vMin);
                vMax = Vector3.Max(v, vMax);
            }
            var bounds = new Bounds(vMin, Vector3.zero);
            bounds.Encapsulate(vMax);
            return bounds;
        }

        protected void UpdateBounds()
        {
            m_ViewBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);
            m_ContentBounds = GetBounds();

            if (m_Content == null)
                return;

            Vector3 contentSize = m_ContentBounds.size;
            Vector3 contentPos = m_ContentBounds.center;
            var contentPivot = m_Content.pivot;
            AdjustBounds(ref m_ViewBounds, ref contentPivot, ref contentSize, ref contentPos);
            m_ContentBounds.size = contentSize;
            m_ContentBounds.center = contentPos;

            // TODO: 没看懂
            if (movementType == MovementType.Clamped)
            {
                Vector3 delta = Vector3.zero;
                if (m_ViewBounds.max.x > m_ContentBounds.max.x)
                {
                    delta.x = Math.Min(m_ViewBounds.min.x - m_ContentBounds.min.x,
                        m_ViewBounds.max.x - m_ContentBounds.max.x);
                }
                else if (m_ViewBounds.min.x < m_ContentBounds.min.x)
                {
                    delta.x = Math.Max(m_ViewBounds.min.x - m_ContentBounds.min.x,
                        m_ViewBounds.max.x - m_ContentBounds.max.x);
                }
                //todo
            }
        }

        internal static void AdjustBounds(ref Bounds viewBounds, ref Vector2 contentPivot, ref Vector3 contentSize,
            ref Vector3 contentPos)
        {
            Vector3 excess = viewBounds.size - contentSize;
            if (excess.x > 0)
            {
                // NOTE: 根据pivot 的值来决定从哪个方向扩大 content
                // TODO, 是不是应该为 contentPos.x += excess.x * (contentPivot.x - 0.5f)
                contentPos.x -= excess.x * (contentPivot.x - 0.5f);
                contentSize.x = viewBounds.size.x;
            }

            if (excess.y > 0)
            {
                contentPos.y -= excess.y * (contentPivot.y - 0.5f);
                contentSize.y = viewBounds.size.y;
            }
        }

        public void LayoutComplete()
        {
            throw new System.NotImplementedException();
        }

        public void GraphicUpdateComplete()
        {
            throw new System.NotImplementedException();
        }

        public void CalculateLayoutInputHorizontal()
        {
            throw new System.NotImplementedException();
        }

        public void CalculateLayoutInputVertical()
        {
            throw new System.NotImplementedException();
        }

        public float mainWidth { get; }
        public float preferredWidth { get; }
        public float flexibleWidth { get; }
        public float mainHeight { get; }
        public float preferredHeight { get; }
        public float flexibleHeight { get; }
        public int layoutPriority { get; }
        public void SetLayoutHorizontal()
        {
            throw new System.NotImplementedException();
        }

        public void SetLayoutVertical()
        {
            throw new System.NotImplementedException();
        }

        private void SetHorizontalNormalizedPosition(float value)
        {
            SetNormalizedPosition(value, 0);
        }

        private void SetVerticalNormalizedPosition(float value)
        {
            SetNormalizedPosition(value, 1);
        }

        protected virtual void SetNormalizedPosition(float value, int axis)
        {
            EnsureLayoutHasRebuilt();
            UpdateBounds();
            float hiddenLength = m_ContentBounds.size[axis] - m_ViewBounds.size[axis];
            float contentBoundsMinPosition = m_ViewBounds.min[axis] - value * hiddenLength;
            float newLocalPosition =
                m_Content.localPosition[axis] + contentBoundsMinPosition - m_ContentBounds.min[axis];

            Vector3 localPosition = m_Content.localPosition;
            if (Mathf.Abs(localPosition[axis] - newLocalPosition) > 0.01f)
            {
                localPosition[axis] = newLocalPosition;
                m_Content.localPosition = localPosition;
                m_Velocity[axis] = 0;
                UpdateBounds();
            }
        }
        
        protected void SetDirtyCaching()
        {
            if (!IsActive())
                return;
            CanvasUpdateRegistry.TryRegisterCanvasElementForLayoutRebuild(this);
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

        private void EnsureLayoutHasRebuilt()
        {
            if(!m_HasRebuiltLayout && !CanvasUpdateRegistry.IsRebuildingLayout())
                Canvas.ForceUpdateCanvases();
        }
    }
}