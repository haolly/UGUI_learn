﻿namespace UnityEngine.EventSystem
{
    public abstract class UIBehaviour : MonoBehaviour
    {
        protected virtual void Awake()
        {
        }

        protected virtual void OnEnable()
        {

        }

        protected virtual void Start()
        {

        }

        protected virtual void OnDisable()
        {

        }

        protected virtual void OnDestroy()
        {

        }

        public virtual bool IsActive()
        {
            return isActiveAndEnabled;
        }

        protected virtual void OnRectTransformDimensionsChange()
        {

        }

        protected virtual void OnBeforeTransformParentChanged()
        {

        }

        protected virtual void OnTransformparentChanged()
        {
        }

        protected virtual void OnDidApplyAnimationProperties()
        {

        }

        protected virtual void OnCanvasGroupChanged()
        {

        }

        protected virtual void OnCanvasHierarchyChanged()
        {

        }

        public bool IsDestroyed()
        {
            // Workaround for Unity native side of the object
            // having been destroyed but accessing via interface
            // won't call the overloaded ==
            // todo liuhao
            return this == null;
        }
    }
}
