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
            // AdaptationManager.uiElements[i].GetComponent<AdaptationManager>().Adapt(layouts[i]);
        }
    }
}