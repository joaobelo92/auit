using System.Collections.Generic;
using AUIT.AdaptationObjectives.Definitions;
using AUIT.Extras;
using UnityEngine;

namespace AUIT.SelectionStrategies
{
    public class InPlaceSelectionStrategy : SelectionStrategy
    {
        public override void Adapt(UIConfiguration[] layouts)
        {
            foreach (UIConfiguration layout in layouts)
            {
                Debug.Log($"Trying to apply layout: {layout.elements[0].Position}");
            }
        }
    }
}