using System;
using System.Configuration;

namespace UnityEngine.UI
{
    [Serializable]
    public struct Navigation : IEquatable<Navigation>
    {
        [Flags]
        public enum Mode
        {
            None = 0,
            Horizontal = 1,
            Vertical = 2,
            Automatic = 3,
            Explicit = 4,
        }

        private Mode m_Mode;
        public Mode mode
        {
            get { return m_Mode; }
            set { m_Mode = value; }
        }
        private Selectable m_SelectOnUp;

        public Selectable selectOnUp
        {
            get { return m_SelectOnUp; }
            set { m_SelectOnUp = value; }
        }
        private Selectable m_SelectOnDown;

        public Selectable selectOnDown
        {
            get { return m_SelectOnDown; }
            set { m_SelectOnDown = value; }
        }
        private Selectable m_SelectOnLeft;

        public Selectable selectOnLeft
        {
            get { return m_SelectOnLeft; }
            set { m_SelectOnLeft = value; }
        }
        private Selectable m_SelectOnRight;

        public Selectable selectOnRight
        {
            get { return m_SelectOnRight; }
            set { m_SelectOnRight = value; }
        }


        static public Navigation defaultNavigation
        {
            get
            {
                var defaultNav = new Navigation();
                defaultNav.m_Mode = Mode.None;
                return defaultNav;
            }
        }
        
        public bool Equals(Navigation other)
        {
            return mode == other.m_Mode &&
                   selectOnUp == other.selectOnUp &&
                   selectOnDown == other.selectOnDown &&
                   selectOnLeft == other.selectOnLeft &&
                   selectOnRight == other.selectOnRight;
        }
    }
}