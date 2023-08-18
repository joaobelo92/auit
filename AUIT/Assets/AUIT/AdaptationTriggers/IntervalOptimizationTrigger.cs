using System.Collections.Generic;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine;

namespace AUIT.AdaptationTriggers
{
    public class IntervalOptimizationTrigger : AdaptationTrigger
    {

        [SerializeField]
        private float interval = 5f;
        
        [SerializeField]
        private float timeoutThreshold = 10f;
        

        protected void Start()
        {
            InvokeRepeating("ApplyStrategy", 0.5f, interval);
        }

        public override void ApplyStrategy()
        {
            if (enabled == false)
                return;

            Debug.Log("Interval Optimization Running...");

            Debug.Log(AdaptationManager);
            
            StartCoroutine(AdaptationManager.OptimizeLayoutAndAdapt(timeoutThreshold, AdaptationLogic));
        }

        private void AdaptationLogic(List<List<Layout>> layouts, float cost)
        {
            AdaptationManager.Adapt(layouts);
        }
    }
}