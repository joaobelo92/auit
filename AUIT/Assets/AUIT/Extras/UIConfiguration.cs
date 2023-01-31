using System;
using System.Linq;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine.Serialization;

namespace AUIT.Extras
{
    [Serializable]
    public class UIConfiguration
    {
        public Layout[] items;

        public static UIConfiguration FromLayout(Layout layout)
        {
            UIConfiguration config = new UIConfiguration();
            config.items = new Layout[1];
            config.items[0] = layout;
            return config;
        }

        public override string ToString()
        {
            return items.First().ToString();
        }
    }
}