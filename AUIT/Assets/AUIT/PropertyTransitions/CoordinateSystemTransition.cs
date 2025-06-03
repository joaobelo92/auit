using AUIT.AdaptationObjectives.Definitions;
using AUIT.ContextSources;
using UnityEngine;

namespace AUIT.PropertyTransitions
{
    public class CoordinateSystemTransition : PropertyTransition
    {
        // TODO: Add mechanism to search for context sources 
        // TODO: Run adapt only if different from current coordinate system
        protected override TransitionType TransitionType => TransitionType.Position;
        
        public TransformContextSource TorsoContextSource;
        public TransformContextSource HeadContextSource;
        
        public GameObject debugObject;
        
        public override void Adapt(Layout layout)
        {
            
            switch (layout.CoordinateSystem)
            {
                case CoordinateSystem.World:
                    // debugObject.GetComponent<Renderer>().material.color = Color.green;
                    gameObject.transform.SetParent(null);
                    break;

                case CoordinateSystem.Head:
                    // Assuming a head context source is available
                    // debugObject.GetComponent<Renderer>().material.color = Color.red;
                    Debug.Log("changing coordinate system to " + HeadContextSource.GetValue().name);
                    gameObject.transform.SetParent(HeadContextSource.GetValue());
                    break;

                case CoordinateSystem.Torso:
                    // Assuming a torso context source is available
                    gameObject.transform.SetParent(TorsoContextSource.GetValue());
                    break;
            }
        }
    }
}
