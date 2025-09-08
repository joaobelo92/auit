using System.Collections.Generic;
using AUIT.AdaptationObjectives.Definitions;
using AUIT.ContextSources;
using UnityEngine;

namespace AUIT.PropertyTransitions
{
    public class CoordinateSystemTransition : PropertyTransition
    {
        // TODO: Add mechanism to search for context sources 
        // TODO: Run adapt only if different from current coordinate system
        protected override TransitionType TransitionType => TransitionType.CoordinateSystem;

        public TransformContextSource TorsoContextSource;
        public TransformContextSource HeadContextSource;
        public TransformContextSource LeftLimbContextSource;
        public TransformContextSource RightLimbContextSource;


        public GameObject debugObject;
        public CoordinateSystem onStartCoordinateSystem = CoordinateSystem.World;

        [HideInInspector]
        public CoordinateSystem CurrentCoordinateSystem;

        protected override void Start()
        {
            base.Start();
            CurrentCoordinateSystem = onStartCoordinateSystem;
            Adapt(new Layout() { CoordinateSystem = onStartCoordinateSystem });
        }

        public override void Adapt(Layout layout)
        {
            CurrentCoordinateSystem = layout.CoordinateSystem;
            switch (layout.CoordinateSystem)
            {
                case CoordinateSystem.World:
                    // debugObject.GetComponent<Renderer>().material.color = Color.green;
                    gameObject.transform.SetParent(null);
                    break;

                case CoordinateSystem.Head:
                    // Assuming a head context source is available
                    // debugObject.GetComponent<Renderer>().material.color = Color.red;
                    gameObject.transform.SetParent(HeadContextSource.GetValue());
                    break;

                case CoordinateSystem.Torso:
                    // Assuming a torso context source is available
                    gameObject.transform.SetParent(TorsoContextSource.GetValue());
                    break;

                case CoordinateSystem.LimbLeft:
                    // Assuming a left limb context source is available
                    gameObject.transform.SetParent(LeftLimbContextSource.GetValue());
                    break;

                case CoordinateSystem.LimbRight:
                    // Assuming a right limb context source is available
                    gameObject.transform.SetParent(RightLimbContextSource.GetValue());
                    break;

            }
        }
        
    }
}
