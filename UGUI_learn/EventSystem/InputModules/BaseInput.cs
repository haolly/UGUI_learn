﻿namespace UnityEngine.EventSystem
{
    public class BaseInput : UIBehaviour
    {
        /// <summary>
        /// NOTE: it will contain the english characters you type.
        /// For example, if you type "ni hao ma", you will see english characters at first,
        /// but as they get interpreted into an eastern language the composition string gets cleared.,
        /// ref https://forum.unity.com/threads/input-compositionstring-how-to-use-it.119548/
        /// ref https://developer.mozilla.org/en-US/docs/Mozilla/IME_handling_guide
        /// </summary>
        public virtual string compositionString
        {
            get { return Input.compositionString; }
        }

        public virtual IMECompositionMode imeCompositionMode
        {
            get { return Input.imeCompositionMode; }
            set { Input.imeCompositionMode = value; }
        }

        public virtual Vector2 compositionCursorPos
        {
            get { return Input.compositionCursorPos; }
            set { Input.compositionCursorPos = value; }
        }

        public virtual bool mousePresent
        {
            get { return Input.mousePresent; }
        }

        public virtual bool GetMouseButtonDown(int button)
        {
            return Input.GetMouseButtonDown(button);
        }

        public virtual bool GetMouseButtonUp(int button)
        {
            return Input.GetMouseButtonUp(button);
        }

        public virtual bool GetMouseButton(int button)
        {
            return Input.GetMouseButton(button);
        }

        public virtual Vector2 mousePosition
        {
            get { return Input.mousePosition; }
        }

        public virtual Vector2 mouseScrollDelta
        {
            get { return Input.mouseScrollDelta; }
        }

        public virtual bool touchSupported
        {
            get { return Input.touchSupported; }
        }

        public virtual int touchCount
        {
            get { return Input.touchCount; }
        }

        public virtual Touch GetTouch(int index)
        {
            return Input.GetTouch(index);
        }

        public virtual float GetAxisRaw(string axisName)
        {
            return Input.GetAxisRaw(axisName);
        }

        public virtual bool GetButtonDown(string buttonName)
        {
            return Input.GetButtonDown(buttonName);
        }
    }
}