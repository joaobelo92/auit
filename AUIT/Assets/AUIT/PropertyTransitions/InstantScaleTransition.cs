using System.Collections.Generic;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine;

namespace AUIT.PropertyTransitions
{
    public class InstantScaleTransition : PropertyTransition, IScaleAdaptation
    {
        public void Adapt(Transform objectTransform, Vector3 target)
        {
            transform.localScale = target;
        }

        public void Adapt(GameObject ui, List<Layout> target)
        {
            throw new System.NotImplementedException();
        }
    }
}