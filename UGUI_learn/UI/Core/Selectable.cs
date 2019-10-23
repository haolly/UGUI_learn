using System.Collections.Generic;
using UnityEngine.EventSystem;

namespace UnityEngine.UI
{
    public class Selectable : UIBehaviour, IMoveHandler, IPointerDownHandler, IPointerUpHandler, 
        IPointerEnterHandler, IPointerExitHandler,
        ISelectHandler, IDeselectHandler
    {
        private static List<Selectable> s_List = new List<Selectable>();
        public static List<Selectable> allSelectables
        {
            get { return s_List; }
        }

        private Navigation m_Navigation = Navigation.defaultNavigation;
        public enum Transition
        {
            None,
            ColorTint,
            SpriteSwap,
            Animation,
        }

        private Transition m_Transition = Transition.None;





        private bool m_Interactable = true;
        private Graphic m_TargetGraphic;
        private bool m_GroupsAllowInteraction = true;

        public Navigation navigation
        {
            get { return m_Navigation; }
            set
            {
                if(SetPropertyUtility.SetStruct(ref m_Navigation, value))
                    OnSetProperty();
            }
        }

        public Graphic targetGraphic
        {
            get { return m_TargetGraphic; }
            set
            {
                if (SetPropertyUtility.SetClass(ref m_TargetGraphic, value))
                    OnSetProperty();
            }
        }

        public bool interactable
        {
            get { return m_Interactable; }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_Interactable, value))
                {
                    if(!m_Interactable && EventSystem.EventSystem.current != null && EventSystem.EventSystem.current.currentSelectedGameObject == gameObject)
                        EventSystem.EventSystem.current.SetSelectedGameObject(null);
                    OnSetProperty();
                }
            }
        }
        
        private bool isPointerInside { get; set; } 
        private bool isPointerDown { get; set; }
        private bool hasSelection { get; set; }

        protected Selectable()
        {
        }

        public Image image
        {
            get { return m_TargetGraphic as Image; }
            set { m_TargetGraphic = value; }
        }

        protected override void Awake()
        {
            if (m_TargetGraphic == null)
                m_TargetGraphic = GetComponent<Graphic>();
        }
        
        private readonly List<CanvasGroup> m_CanvasGroupCache = new List<CanvasGroup>();
        protected override void OnCanvasGroupChanged()
        {
            var groupAllInteraction = true;
            Transform t = transform;
            while (t != null)
            {
                t.GetComponents(m_CanvasGroupCache);
                bool shouldBreak = false;
                for (int i = 0; i < m_CanvasGroupCache.Count; i++)
                {
                    if (!m_CanvasGroupCache[i].interactable)
                    {
                        groupAllInteraction = false;
                        shouldBreak = true;
                    }

                    if (m_CanvasGroupCache[i].ignoreParentGroups)
                        shouldBreak = true;
                }
                
                if(shouldBreak)
                    break;

                t = t.parent;
            }

            if (groupAllInteraction != m_GroupsAllowInteraction)
            {
                m_GroupsAllowInteraction = groupAllInteraction;
                OnSetProperty();
            }
        }

        public virtual bool IsInteractable()
        {
            return m_GroupsAllowInteraction && m_Interactable;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            s_List.Add(this);
            //todo
        }

        private void OnSetProperty()
        {
            InternalEvaluateAndTransitionToSelectionState(false);
        }

        private void InternalEvaluateAndTransitionToSelectionState(bool instant)
        {
            var transitionState = m_CurrentSelectionState;
            //todo
        }

        private void EvaluateAndTransitionToSelectionState(BaseEventData eventData)
        {
            if (!IsActive() || !IsInteractable())
                return;
            UpdateSelctionState(eventData);
            InternalEvaluateAndTransitionToSelectionState(false);
        }

        public virtual void OnMove(AxisEventData eventData)
        {
            throw new System.NotImplementedException();
        }

        public virtual void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;
            
            if(IsInteractable() && navigation.mode != Navigation.Mode.None && EventSystem.EventSystem.current != null)
                EventSystem.EventSystem.current.SetSelectedGameObject(gameObject, eventData);
            isPointerDown = true;
            EvaluateAndTransitionToSelectionState(eventData);
        }

        public virtual void OnPointerUp(PointerEventData eventData)
        {
            throw new System.NotImplementedException();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            throw new System.NotImplementedException();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            throw new System.NotImplementedException();
        }

        public void OnSelect(BaseEventData eventData)
        {
            throw new System.NotImplementedException();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            throw new System.NotImplementedException();
        }
    }
}