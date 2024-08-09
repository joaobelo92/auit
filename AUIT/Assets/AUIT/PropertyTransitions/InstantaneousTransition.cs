using System.Collections;
using System.Collections.Generic;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine;

namespace AUIT.PropertyTransitions
{
    public class InstantaneousTransition : PropertyTransition
    {

        public override void Adapt(Layout layout)
        {
            StartCoroutine(transitionInstantaneously(layout));
        }

        private IEnumerator transitionInstantaneously(Layout layout)
        {
            transform.position = layout.Position;
            transform.rotation = layout.Rotation;
            transform.localScale = layout.Scale;
        }
    }
}