using System.Collections.Generic;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine;

namespace AUIT.AdaptationTriggers
{
    public class OnRequestOptimizationTrigger : AdaptationTrigger
    {
        
        [SerializeField]
        private float timeoutThreshold = 100000f;
        
        public override void ApplyStrategy()
        {
            Debug.Log("Interval Optimization Running...");
        }

        public void OptimizationRequest()
        {
            Debug.Log("You asked for it!");
            StartCoroutine(AdaptationManager.OptimizeLayoutAndAdapt(timeoutThreshold, AdaptationLogic));
        }
        
        private void AdaptationLogic(List<List<Layout>> layouts, float cost)
        {
            if (AdaptationManager.isGlobal)
            {
                for (int i = 0; i < AdaptationManager.UIElements.Count; i++)
                {
                    AdaptationManager.UIElements[i].GetComponent<AdaptationManager>().layouts = layouts[i];
                    AdaptationManager.UIElements[i].GetComponent<AdaptationManager>().Adapt(layouts[i]);
                }
            }
            else
            {
                List<Layout> elementLayouts = new List<Layout>();
                foreach (var layout in layouts)
                {
                    elementLayouts.Add(layout[0]);
                }
                AdaptationManager.layouts = elementLayouts;
                AdaptationManager.Adapt(elementLayouts);
            }
        }
    }
}