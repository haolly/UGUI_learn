namespace UnityEngine.UI
{
    public enum CanvasUpdate
    {
        PreLayout = 0,
        Layout = 1,
        PostLayout = 2,
        PreRender = 3,
        LateRender = 4,
        MaxUpdateValue = 5,
    }

    public interface ICanvasElement
    {
        void Rebuild(CanvasUpdate executing);
        Transform transform { get; }
        void LayoutComplete();
        void GraphicUpdateComplete();
        bool IsDestroyed();
    }
}