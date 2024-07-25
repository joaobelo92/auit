using System.Collections.Generic;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine;

namespace AUIT.PropertyTransitions
{
    public abstract class PropertyTransition : MonoBehaviour
    {
        [HideInInspector]
        protected AdaptationManager AdaptationManager;
        
        // TODO: refactor to work with local handler
        protected virtual void Awake()
        {
            if (AdaptationManager == null)
            {
                AdaptationManager = GetComponent<AdaptationManager>();
            }
        }
        
        protected virtual void Start()
        {
            if (AdaptationManager != null)
            {
                AdaptationManager.RegisterTransition(this);
            }
        }
        protected virtual void OnDestroy()
        {
            if (AdaptationManager != null)
            {
                AdaptationManager.UnregisterTransition(this);
            }
        }
        
        
        public abstract void Adapt(Layout layout);
    }
}