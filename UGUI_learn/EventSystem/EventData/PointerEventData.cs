﻿using System.Collections.Generic;
using UnityEngine.EventSystem.EventData;

namespace UnityEngine.EventSystem
{
    public class PointerEventData : BaseEventData
    {
        public enum InputButton
        {
            Left = 0,
            Right = 1,
            Middle = 2
        }
        
        public enum FramePressState
        {
            Pressed,
            Released,
            PressedAndReleased,
            NotChanged,
        }
        public GameObject pointerEnter { get; set; }
        private GameObject m_PointerPress;
        public GameObject lastPress { get; private set; }
        public GameObject rawPointerPress { get; set; }
        public GameObject pointerDrag { get; set; }
        
        public RaycastResult pointerCurrentRaycast { get; set; }
        public RaycastResult pointerPressRaycast { get; set; }
        
        //TODO: tracked object ?
        public List<GameObject> hovered = new List<GameObject>();
        
        public bool eligibleForClick { get; set; }

        public int pointerId { get; set; }
        
        public Vector2 position { get; set; }
        public Vector2 delta { get; set; }
        public Vector2 pressPosition { get; set; }
        public float clickTime { get; set; }
        public int clickCnt { get; set; }
        
        public Vector2 scrollDelta { get; set; }
        public bool useDragThreshold { get; set; }
        public bool dragging { get; set; }
        
        public InputButton button { get; set; }

        public PointerEventData(EventSystem eventSystem) : base(eventSystem)
        {
            eligibleForClick = false;

            pointerId = -1;
            position = Vector2.zero;
            pressPosition = Vector2.zero;
            delta = Vector2.zero;
            clickTime = 0.0f;
            clickCnt = 0;
            
            scrollDelta = Vector2.zero;
            useDragThreshold = true;
            dragging = false;
            button = InputButton.Left;
        }
    }
}