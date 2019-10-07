using System;
using System.Collections.Generic;

namespace UnityEngine.EventSystem
{
    public abstract class BaseInputModule : UIBehaviour
    {
        [NonSerialized]
        protected List<RaycastResult> m_RaycastResultCache = new List<RaycastResult>();

        private EventSystem m_EventSystem;
        private BaseEventData m_BaseEventData;
        private AxisEventData m_AxisEventData;

        protected BaseInput m_InputOverride;
        protected BaseInput m_DefaultInput;

        public BaseInput input
        {
            get
            {
                if (m_InputOverride != null)
                    return m_InputOverride;
                if (m_DefaultInput == null)
                {
                    var inputs = GetComponents<BaseInput>();
                    for (int i = 0; i < inputs.Length; i++)
                    {
                        var baseInput = inputs[i];
                        if (baseInput != null && baseInput.GetType() == typeof(BaseInput))
                        {
                            m_DefaultInput = baseInput;
                            break;
                        }
                    }

                    if (m_DefaultInput == null)
                        m_DefaultInput = gameObject.AddComponent<BaseInput>();
                }

                return m_DefaultInput;
            }
        }
        
        protected EventSystem eventSystem
        {
            get => m_EventSystem;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            m_EventSystem = GetComponent<EventSystem>();
            m_EventSystem.UpdateModules();
        }

        protected override void OnDisable()
        {
            m_EventSystem.UpdateModules();
            base.OnDisable();
        }

        public abstract void Process();

        protected static RaycastResult FindFirstRaycast(List<RaycastResult> candidates)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                if(candidates[i].gameObject == null)
                    continue;
                return candidates[i];
            }
            return new RaycastResult();
        }

        protected static MoveDirection DetermineMoveDirection(float x, float y)
        {
            return DetermineMoveDirection(x, y, 0.6f);
        }

        protected static MoveDirection DetermineMoveDirection(float x, float y, float deadZone)
        {
            if (new Vector2(x, y).sqrMagnitude < deadZone * deadZone)
                return MoveDirection.None;
            if (Mathf.Abs(x) > Mathf.Abs(y))
            {
                if (x > 0)
                    return MoveDirection.Right;
                return MoveDirection.Left;
            }
            else
            {
                if (y > 0)
                    return MoveDirection.Up;
                return MoveDirection.Down;
            }
        }

        protected static GameObject FindCommonRoot(GameObject g1, GameObject g2)
        {
            if (g1 == null || g2 == null)
                return null;
            var t1 = g1.transform;
            while (t1.parent != null)
            {
                var parent = t1.parent;
                var t2 = g2.transform;
                while (t2.parent != null)
                {
                    if (t2.parent == parent)
                        return parent.gameObject;
                    t2 = t2.parent;
                }
            }

            return null;
        }

        protected void HandlePointerExitAndEnter(PointerEventData currentPointerData, GameObject newEnterTarget)
        {
            if (newEnterTarget == null || currentPointerData.pointerEnter == null)
            {
                for (int i = 0; i < currentPointerData.hovered.Count; i++)
                {
                    ExecuteEvents.Execute(currentPointerData.hovered[i], currentPointerData,
                        ExecuteEvents.pointerExitHandler);
                }
                currentPointerData.hovered.Clear();

                //TODO, 反了吧？
                if (newEnterTarget == null)
                {
                    currentPointerData.pointerEnter = newEnterTarget;
                    return;
                }
            }

            if (currentPointerData.pointerEnter == newEnterTarget && newEnterTarget)
            {
                return;
            }

            GameObject commonRoot = FindCommonRoot(currentPointerData.pointerEnter, newEnterTarget);
            if (currentPointerData.pointerEnter != null)
            {
                Transform t = currentPointerData.pointerEnter.transform;
                while (t != null)
                {
                    if (commonRoot != null && commonRoot.transform == t)
                        break;
                    ExecuteEvents.Execute(t.gameObject, currentPointerData, ExecuteEvents.pointerExitHandler);
                    currentPointerData.hovered.Remove(t.gameObject);
                    t = t.parent;
                }
            }

            currentPointerData.pointerEnter = newEnterTarget;
            if (newEnterTarget != null)
            {
                Transform t = newEnterTarget.transform;
                while (t != null && t.gameObject != commonRoot)
                {
                    ExecuteEvents.Execute(t.gameObject, currentPointerData, ExecuteEvents.pointerExitHandler);
                    currentPointerData.hovered.Add(t.gameObject);
                    t = t.parent;
                }
            }
        }

        protected virtual BaseEventData GetBaseEventData()
        {
            if(m_BaseEventData == null)
                m_BaseEventData = new BaseEventData(eventSystem);
            m_BaseEventData.Reset();
            return m_BaseEventData;
        }

        protected virtual AxisEventData GetAxisEventData(float x, float y, float moveDeadZone)
        {
            if(m_AxisEventData == null)
                m_AxisEventData = new AxisEventData(eventSystem);
            
            m_AxisEventData.Reset();
            m_AxisEventData.moveVector = new Vector2(x, y);
            m_AxisEventData.moveDir = DetermineMoveDirection(x, y, moveDeadZone);
            return m_AxisEventData;
        }

        public virtual bool IsPointerOverGameObject(int pointerId)
        {
            return false;
        }

        public virtual bool ShouldActiveModule()
        {
            return enabled && gameObject.activeInHierarchy;
        }

        public virtual void DeactivateModule()
        {
        }

        public virtual void ActivateModule()
        {
        }

        public virtual void UpdateModule()
        {
        }

        public virtual bool IsModuleSupported()
        {
            return true;
        }
    }
}