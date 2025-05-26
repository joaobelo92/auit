using AUIT.AdaptationObjectives.Definitions;
using AUIT.ContextSources;
using UnityEngine;

namespace AUIT.PropertyTransitions
{
    public class CoordinateSystemTransition : PropertyTransition
    {
        // TODO: Add mechanism to search for context sources 
        // TODO: Run adapt only if different from current coordinate system
        
        public TransformContextSource TorsoContextSource;
        public TransformContextSource HeadContextSource;
        
        public override void Adapt(Layout layout)
        {
            Debug.Log("changing coordinate system to " + layout.CoordinateSystem);
            switch (layout.CoordinateSystem)
            {
                case CoordinateSystem.World:
                    gameObject.transform.SetParent(null, worldPositionStays: true);
                    break;

                case CoordinateSystem.Head:
                    // Assuming a head context source is available
                    gameObject.transform.SetParent(HeadContextSource.GetValue(), worldPositionStays: true);
                    break;

                case CoordinateSystem.Torso:
                    // Assuming a torso context source is available
                    gameObject.transform.SetParent(TorsoContextSource.GetValue(), worldPositionStays: true);
                    break;

                default:
                    Debug.LogWarning("Unknown coordinate system specified in layout.");
                    break;
                    
            }
        }
    }
}
