using System.Collections.Generic;

namespace UnityEngine.EventSystem
{
    public class EventSystem : UIBehaviour
    {
        private List<BaseInputModule> m_SystemInputModules = new List<BaseInputModule>();
        private BaseInputModule m_CurrentInputModule;
        
        public static EventSystem current { get; set; }
    }
}