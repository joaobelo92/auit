using System.Collections.Generic;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine;

namespace AUIT.SelectionStrategies
{
    public class InPlaceSelectionStrategy : SelectionStrategy
    {
        public override void Adapt(List<List<Layout>> layouts)
        {
            foreach (List<Layout> layout in layouts)
            {
                Debug.Log($"Trying to apply layout: {layout[0].Position}");
            }
        }
    }
}