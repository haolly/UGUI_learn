﻿namespace UnityEngine.EventSystem
{
    public interface IEventSystemHandler
    {
        
    }

    public interface IPointerEnterHandler : IEventSystemHandler
    {
        void OnPointerEnter(PointerEventData eventData);
    }

    public interface IPointerExitHandler : IEventSystemHandler
    {
        void OnPointerExit(PointerEventData eventData);
    }

    public interface IPointerDownHandler : IEventSystemHandler
    {
        void OnPointerDown(PointerEventData eventData);
    }

    public interface IPointerUpHandler : IEventSystemHandler
    {
        void OnPointerUp(PointerEventData eventData);
    }

    public interface IPointerClickHandler : IEventSystemHandler
    {
        void OnPointerClick(PointerEventData eventData);
    }

    public interface ISelectHandler : IEventSystemHandler
    {
        void OnSelect(BaseEventData eventData);
    }

    public interface IDeselectHandler : IEventSystemHandler
    {
        void OnDeselect(BaseEventData eventData);
    }

    public interface IUpdateSelectedHandler : IEventSystemHandler
    {
        void OnUpdateSelected(BaseEventData eventData);
    }
}