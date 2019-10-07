
using System;
using System.Collections.Generic;
using UnityEngine.UI;

namespace UnityEngine.EventSystem
{
    public static class ExecuteEvents
    {
        public delegate void EventFunction<T1>(T1 handler, BaseEventData eventData);

        public static T ValidateEventData<T>(BaseEventData data) where T : class
        {
            if ((data as T) == null)
                throw new ArgumentException(String.Format("Invalid type:{0} passed to event, expecting {1}",
                    data.GetType(), typeof(T)));
            return data as T;
        }

        private static readonly EventFunction<IPointerEnterHandler> s_PointerEnterHandler = Execute;

        private static void Execute(IPointerEnterHandler handler, BaseEventData eventData)
        {
            handler.OnPointerEnter(ValidateEventData<PointerEventData>(eventData));
        }

        private static readonly EventFunction<IPointerExitHandler> s_PointerExitHandler = Execute;
        private static void Execute(IPointerExitHandler handler, BaseEventData eventData)
        {
            handler.OnPointerExit(ValidateEventData<PointerEventData>(eventData));
        }

        private static void Execute(IPointerDownHandler handler, BaseEventData eventData)
        {
            handler.OnPointerDown(ValidateEventData<PointerEventData>(eventData));
        }

        private static void Execute(IPointerUpHandler handler, BaseEventData eventData)
        {
            handler.OnPointerUp(ValidateEventData<PointerEventData>(eventData));
        }

        private static readonly EventFunction<ISelectHandler> s_SelectHandler = Execute;

        private static void Execute(ISelectHandler handler, BaseEventData eventData)
        {
            handler.OnSelect(eventData);
        }

        private static readonly EventFunction<IDeselectHandler> s_DeselectHandler = Execute;

        private static void Execute(IDeselectHandler handler, BaseEventData eventData)
        {
            handler.OnDeselect(eventData);
        }

        private static readonly EventFunction<IUpdateSelectedHandler> s_UpdateSelectedHandler = Execute;

        private static void Execute(IUpdateSelectedHandler handler, BaseEventData eventData)
        {
            handler.OnUpdateSelected(eventData);
        }

        public static EventFunction<IUpdateSelectedHandler> updateSelectedHandler
        {
            get { return s_UpdateSelectedHandler; }
        }

        public static EventFunction<IPointerEnterHandler> pointerEnterHandler
        {
            get { return s_PointerEnterHandler; }
        }

        public static EventFunction<IPointerExitHandler> pointerExitHandler
        {
            get { return s_PointerExitHandler; }
        }

        public static EventFunction<ISelectHandler> selectHandler
        {
            get { return s_SelectHandler; }
        }

        public static EventFunction<IDeselectHandler> deselectHandler => s_DeselectHandler;

        private static readonly ObjectPool<List<IEventSystemHandler>> s_HandlerListPool =
            new ObjectPool<List<IEventSystemHandler>>(null, l => l.Clear());

        public static bool Execute<T>(GameObject target, BaseEventData eventData, EventFunction<T> functor)
            where T : IEventSystemHandler
        {
            var internalHandlers = s_HandlerListPool.Get();
            GetEventList<T>(target, internalHandlers);
            for (int i = 0; i < internalHandlers.Count; i++)
            {
                T arg;
                try
                {
                    arg = (T) internalHandlers[i];
                }
                catch (Exception e)
                {
                    var temp = internalHandlers[i];
                    Debug.LogException(new Exception(string.Format("Type {0} expected, {1} received", typeof(T).Name, 
                        temp.GetType().Name), e));
                    continue;
                }

                try
                {
                    functor(arg, eventData);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            var handlerCount = internalHandlers.Count;
            s_HandlerListPool.Release(internalHandlers);
            return handlerCount > 0;
        }

        private static void GetEventList<T>(GameObject go, IList<IEventSystemHandler> results)
            where T : IEventSystemHandler
        {
            if(results == null)
                throw new ArgumentException("result array is null");

            if (go == null || !go.activeInHierarchy)
                return;

            var components = ListPool<Component>.Get();
            go.GetComponents(components);
            //Note, component implement IEventSystemHandler
            for (int i = 0; i < components.Count; i++)
            {
                if(!ShouldSendToComponent<T>(components[i]))
                    continue;
                results.Add(components[i] as IEventSystemHandler);
            }
            ListPool<Component>.Release(components);
        }

        private static bool ShouldSendToComponent<T>(Component component) where T : IEventSystemHandler
        {
            var valide = component is T;
            if (!valide)
                return false;
            var behaviour = component as Behaviour;
            if (behaviour != null)
                return behaviour.isActiveAndEnabled;
            return true;
        }
    }
}