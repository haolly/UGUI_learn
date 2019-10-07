using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace UnityEngine.EventSystem
{
    public class EventSystem : UIBehaviour
    {
        private List<BaseInputModule> m_SystemInputModules = new List<BaseInputModule>();
        private BaseInputModule m_CurrentInputModule;
        
        public static EventSystem current { get; set; }

        [SerializeField] private GameObject m_FirstSelected;
        [SerializeField] private bool m_sendNavigationEvents = true;

        public bool sendNavigationEvents
        {
            get { return m_sendNavigationEvents; }
            set { m_sendNavigationEvents = value; }
        }

        [SerializeField] private int m_DragThreshold = 5;

        public int pixelDragThresHold
        {
            get { return m_DragThreshold; }
            set { m_DragThreshold = value; }
        }

        private GameObject m_CurrentSelected;

        public BaseInputModule currentInputModule
        {
            get { return m_CurrentInputModule; }
        }

        public GameObject firstSelectedGameObject
        {
            get { return m_FirstSelected; }
            set { m_FirstSelected = value; }
        }

        public GameObject currentSelectedGameObject
        {
            get { return m_CurrentSelected; }
        }

        private bool m_Paused;

        protected EventSystem()
        {
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (EventSystem.current == null)
                EventSystem.current = this;
            else
            {
                Debug.LogWarning("Mutiple EventSystem in scene, this is not supported");
            }
        }

        protected override void OnDisable()
        {
            if (m_CurrentInputModule != null)
            {
                m_CurrentInputModule.DeactivateModule();
                m_CurrentInputModule = null;
            }

            if (EventSystem.current == this)
                EventSystem.current = null;
            
            base.OnDisable();
        }

        private void TickModules()
        {
            for (int i = 0; i < m_SystemInputModules.Count; i++)
            {
                if(m_SystemInputModules[i] != null)
                    m_SystemInputModules[i].UpdateModule();
            }
        }

        public void UpdateModules()
        {
            GetComponents(m_SystemInputModules);
            for (int i = m_SystemInputModules.Count - 1; i >=0; i--)
            {
                if(m_SystemInputModules[i] && m_SystemInputModules[i].IsActive())
                    continue;
                m_SystemInputModules.RemoveAt(i);
            }
        }

        private bool m_SelectionGuard;

        public bool alreadySelecting
        {
            get { return m_SelectionGuard; }
        }

        public void SetSelectedGameObject(GameObject selected, BaseEventData pointer)
        {
            if (m_SelectionGuard)
            {
                Debug.LogError("Attempting to select " + selected +"while already selecting an object");
                return;
            }

            m_SelectionGuard = true;
            if (selected == m_CurrentSelected)
            {
                m_SelectionGuard = false;
                return;
            }

            ExecuteEvents.Execute(m_CurrentSelected, pointer, ExecuteEvents.deselectHandler);
            m_CurrentSelected = selected;
            ExecuteEvents.Execute(m_CurrentSelected, pointer, ExecuteEvents.selectHandler);
            m_SelectionGuard = false;
        }

        private BaseEventData m_DummyData;

        private BaseEventData baseEventDataCache
        {
            get
            {
                if(m_DummyData == null)
                    m_DummyData = new BaseEventData(this);

                return m_DummyData;
            }
        }

        public void SetSelectedGameObject(GameObject selected)
        {
            SetSelectedGameObject(selected, baseEventDataCache);
        }

        /// <summary>
        /// return 1 means lhs > rhs, so lhs will be AFTER rhs
        /// DESCENT order by depth/sortOrderPriority/sortingLayer/sortingOrder/depth/distance
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        private static int RaycastComparer(RaycastResult lhs, RaycastResult rhs)
        {
            if (lhs.module != rhs.module)
            {
                if (lhs.module.eventCamera != null && rhs.module.eventCamera != null &&
                    lhs.module.eventCamera.depth != rhs.module.eventCamera.depth)
                {
/*
                    if (lhs.module.eventCamera.depth < rhs.module.eventCamera.depth)
                        return 1;
                    if (lhs.module.eventCamera.depth == rhs.module.eventCamera.depth)
                        return 0;
                    return -1;
*/
                    // descent order with depth
                    return rhs.module.eventCamera.depth.CompareTo(lhs.module.eventCamera.depth);
                }

                if (lhs.module.sortOrderPriority != rhs.module.sortOrderPriority)
                    return rhs.module.sortOrderPriority.CompareTo(lhs.module.sortOrderPriority);
                if (lhs.module.renderOrderPriority != rhs.module.renderOrderPriority)
                    return rhs.module.renderOrderPriority.CompareTo(lhs.module.renderOrderPriority);
            }

            if (lhs.sortingLayer != rhs.sortingLayer)
            {
                var rid = SortingLayer.GetLayerValueFromID(rhs.sortingLayer);
                var lid = SortingLayer.GetLayerValueFromID(lhs.sortingLayer);
                return rid.CompareTo(lid);
            }

            if (lhs.sortingOrder != rhs.sortingOrder)
                return rhs.sortingOrder.CompareTo(lhs.sortingOrder);
            
            if (lhs.depth != rhs.depth)
                return rhs.depth.CompareTo(lhs.depth);
            
            if (lhs.distance != rhs.distance)
                return rhs.distance.CompareTo(lhs.distance);
            return lhs.index.CompareTo(rhs.index);
        }

        private static readonly Comparison<RaycastResult> s_RaycastComparer = RaycastComparer;

        public void RaycastAll(PointerEventData eventData, List<RaycastResult> raycastResults)
        {
            raycastResults.Clear();
            var modules = RaycasterManager.GetRaycasters();
            for (int i = 0; i < modules.Count; i++)
            {
                var module = modules[i];
                if(module == null || !module.IsActive())
                    continue;
                
                module.Raycast(eventData, raycastResults);
            }
            raycastResults.Sort(s_RaycastComparer);
        }


        public bool IsPointerOverGameObject(int pointerId)
        {
            if (m_CurrentInputModule == null)
                return false;
            return m_CurrentInputModule.IsPointerOverGameObject(pointerId);
        }
        
        protected virtual void Update()
        {
            if (current != this || m_Paused)
                return;
            TickModules();
            bool changeModule = false;
            for (int i = 0; i < m_SystemInputModules.Count; i++)
            {
                var module = m_SystemInputModules[i];
                if (module.IsModuleSupported() && module.ShouldActiveModule())
                {
                    if (m_CurrentInputModule != module)
                    {
                        ChangeEventModule(module);
                        changeModule = true;
                    }
                    break;
                }
            }

            if (m_CurrentInputModule == null)
            {
                for (int i = 0; i < m_SystemInputModules.Count; i++)
                {
                    var module = m_SystemInputModules[i];
                    if (module.IsModuleSupported())
                    {
                        ChangeEventModule(module);
                        changeModule = true;
                        break;
                    }
                }
            }

            if (!changeModule && m_CurrentInputModule != null)
            {
                m_CurrentInputModule.Process();
            }
        }

        private void ChangeEventModule(BaseInputModule module)
        {
            if (m_CurrentInputModule == module)
                return;
            if(m_CurrentInputModule != null)
                m_CurrentInputModule.DeactivateModule();

            if(module != null)
                module.ActivateModule();
            m_CurrentInputModule = module;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("<b>Selected:</b>" + currentSelectedGameObject);
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine(m_CurrentInputModule != null ? m_CurrentInputModule.ToString() : "No module");
            return sb.ToString();
        }
    }
}