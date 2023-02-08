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

            StartCoroutine(AdaptationManager.OptimizeLayoutAndAdapt(timeoutThreshold, AdaptationLogic));

            // var (layouts, _) = AdaptationManager.OptimizeLayout();
            // if (AdaptationManager.isGlobal)
            // {
            //     for (int i = 0; i < AdaptationManager.UIElements.Count; i++)
            //     {
            //         AdaptationManager.UIElements[i].GetComponent<AdaptationManager>().layout = layouts[i];
            //         AdaptationManager.UIElements[i].GetComponent<AdaptationManager>().Adapt(layouts[i]);
            //     }
            // }
            // else
            // {
            //     AdaptationManager.layout = layouts[0];
            //     AdaptationManager.Adapt(layouts[0]);
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