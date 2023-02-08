using System.Collections.Generic;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine;

namespace AUIT.PropertyTransitions
{
    public class InstantRotationTransition : PropertyTransition, IRotationAdaptation
    {
        public void Adapt(Transform objectTransform, Quaternion target)
        {
            transform.rotation = target;
        }

        public void Adapt(GameObject ui, List<Layout> target)
        {
            throw new System.NotImplementedException();
        }
    }
}