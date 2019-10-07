namespace UnityEngine.UI
{
    internal class ReflectionMethodsCache
    {
        private static ReflectionMethodsCache s_ReflectionMethodsCache = null;
        public static ReflectionMethodsCache Singleton
        {
            get
            {
                if(s_ReflectionMethodsCache == null)
                    s_ReflectionMethodsCache = new ReflectionMethodsCache();
                return s_ReflectionMethodsCache;
            }
        }

        public delegate bool Raycast3DCallback(Ray r, out RaycastHit hit, float f, int i);

        public delegate RaycastHit2D Raycast2DCallback(Vector2 p1, Vector2 p2, float f, int i);

        public Raycast3DCallback raycast3D = null;
        public Raycast2DCallback raycast2D = null;

        public ReflectionMethodsCache()
        {
            var raycast3DMethodInfo = typeof(Physics).GetMethod("Raycast",
                new[] {typeof(Ray), typeof(RaycastHit).MakeByRefType(), typeof(float), typeof(int)});
            if (raycast3DMethodInfo != null)
                raycast3D = (Raycast3DCallback) UnityEngineInternal.ScriptingUtils.CreateDelegate(
                    typeof(Raycast2DCallback), raycast3DMethodInfo);
        }
    }
}