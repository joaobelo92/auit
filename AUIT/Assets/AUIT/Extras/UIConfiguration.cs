using System;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine.Serialization;

namespace AUIT.Extras
{
    [Serializable]
    public class UIConfiguration
    {
        public Layout[] elements;

        public static UIConfiguration FromLayout(Layout layout)
        {
            UIConfiguration config = new UIConfiguration();
            config.elements = new Layout[1];
            config.elements[0] = layout;
            return config;
        }
    }
}