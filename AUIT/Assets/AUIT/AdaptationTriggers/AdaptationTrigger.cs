using AUIT.AdaptationObjectives.Definitions;
using UnityEngine;

namespace AUIT.AdaptationTriggers
{
    [RequireComponent(typeof(AdaptationManager))]
    public abstract class AdaptationTrigger : MonoBehaviour
    {
        [HideInInspector]
        protected AdaptationManager AdaptationManager;

        protected virtual void Awake()
        {
            if (AdaptationManager == null)
            {
                AdaptationManager = GetComponent<AdaptationManager>();
            }
        }

        protected virtual void OnEnable()
        {
            if (AdaptationManager == null)
                return;

            AdaptationManager.RegisterTrigger(this);
        }

        protected virtual void OnDisable()
        {
            if (AdaptationManager == null)
                return;
                
            AdaptationManager.UnregisterTrigger(this);
        }

        // Current idea: Manager knows how to invoke solver and keeps track of update rate
        // Update rate should be dependent on strategy tho, for now that could be updated when a strategy is registered,
        // making it possible to support multiple triggers in the future
        public abstract void ApplyStrategy(); 
    }
}