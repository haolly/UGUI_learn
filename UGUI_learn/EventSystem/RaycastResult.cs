namespace UnityEngine.EventSystem
{
    public struct RaycastResult
    {
        private GameObject m_GameObject;

        public GameObject gameObject
        {
            get { return m_GameObject; }
            set { m_GameObject = value; }
        }

        public BaseRaycaster module;
        public float distance;
        public float index;
        public int depth;
        public int sortingLayer;
        public int sortingOrder;
        public Vector3 worldPosition;
        public Vector3 worldNormal;

        public Vector2 screenPosition;

        public bool isValid
        {
            get { return module != null && gameObject != null;  }
        }

        public void Clear()
        {
            gameObject = null;
            module = null;
            distance = 0;
            index = 0;
            depth = 0;
            sortingLayer = 0;
            sortingOrder = 0;
            worldNormal = Vector3.up;
            worldPosition = Vector3.zero;
            screenPosition = Vector2.zero;
        }

        public override string ToString()
        {
            if (!isValid)
                return "";

            return "Name:" + gameObject + "\n" +
                   "module:" + module + "\n" +
                   "distance:" + distance + "\n" +
                   "index:" + index + "\n" +
                   "depth:" + depth + "\n" +
                   "worldNormal:" + worldNormal + "\n" +
                   "worldPosition:" + worldPosition + "\n" +
                   "screenPosition:" + screenPosition + "\n" +
                   "sortingLayer:" + sortingLayer + "\n" +
                   "sortingOrder:" + sortingOrder + "\n";
        }
    }
}