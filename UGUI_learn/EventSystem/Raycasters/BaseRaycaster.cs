using System.Collections.Generic;

namespace UnityEngine.EventSystem
{
    public abstract class BaseRaycaster : UIBehaviour
    {
        public abstract void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList);
        public abstract Camera eventCamera { get; }

        public virtual int sortOrderPriority
        {
            get { return int.MinValue; }
        }

        public virtual int renderOrderPriority
        {
            get { return int.MinValue;  }
        }

        public override string ToString()
        {
            return "Name:" + gameObject + "\n" +
                   "eventCamera:" + eventCamera + "\n" +
                   "sortOrderPriority:" + sortOrderPriority + "\n" +
                   "renderOrderPriority:" + renderOrderPriority;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            RaycasterManager.AddRaycaster(this);
        }

        protected override void OnDisable()
        {
            RaycasterManager.RemoveRaycaster(this);
            base.OnDisable();
        }
    }
}