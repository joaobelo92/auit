using System.Collections.Generic;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine;

namespace AUIT.PropertyTransitions
{
    public class InstantMovementTransition : PropertyTransition, IPositionAdaptation
    {
        public void Adapt(Transform objectTransform, Vector3 target)
        {
            transform.position = target;
        }

        public void Adapt(GameObject ui, List<Layout> target)
        {
            if (target.Count > 0)
            {
                ui.transform.position = target[0].Position;
            }

            foreach (var layout in target.GetRange(1, target.Count-1))
            {
                Debug.Log("creating sphere");
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.position = layout.Position;
                sphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            }
        }
    }
}