﻿using System.Collections.Generic;

namespace UnityEngine.EventSystem
{
    internal static class RaycasterManager
    {
        private static readonly List<BaseRaycaster> s_Raycasters = new List<BaseRaycaster>();

        public static void AddRaycaster(BaseRaycaster baseRaycaster)
        {
            if (s_Raycasters.Contains(baseRaycaster))
                return;
            s_Raycasters.Add(baseRaycaster);
        }

        public static List<BaseRaycaster> GetRaycasters()
        {
            return s_Raycasters;
        }

        public static void RemoveRaycaster(BaseRaycaster baseRaycaster)
        {
            if (!s_Raycasters.Contains(baseRaycaster))
                return;
            s_Raycasters.Remove(baseRaycaster);
        }
    }
}