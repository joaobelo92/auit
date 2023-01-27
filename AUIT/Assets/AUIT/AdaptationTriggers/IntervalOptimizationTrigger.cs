using UnityEngine;

namespace AUIT.AdaptationTriggers
{
    public class IntervalOptimizationTrigger : AdaptationTrigger
    {

        [SerializeField]
        private float interval = 5f;

        protected void Start()
        {
            InvokeRepeating("ApplyStrategy", 0.5f, interval);
        }

        public override void ApplyStrategy()
        {
            if (this.enabled == false)
                return;

            Debug.Log("Interval Optimization Running...");
            
            var (layouts, _) = AdaptationManager.OptimizeLayout();
            if (AdaptationManager.isGlobal)
            {
                for (int i = 0; i < AdaptationManager.UIElements.Count; i++)
                {
                    AdaptationManager.UIElements[i].GetComponent<AdaptationManager>().layout = layouts[i];
                    AdaptationManager.UIElements[i].GetComponent<AdaptationManager>().Adapt(layouts[i]);
                }
            }
            else
            {
                // AdaptationManager.layout = layouts[0];
                // AdaptationManager.Adapt(layouts[0]);
            }
        }
    }
}