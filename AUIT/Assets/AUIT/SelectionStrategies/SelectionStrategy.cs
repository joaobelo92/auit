using System.Collections.Generic;
using AUIT.AdaptationObjectives.Definitions;
using AUIT.Extras;
using UnityEngine;

namespace AUIT.SelectionStrategies
{
    [RequireComponent(typeof(AdaptationManager))]
    public abstract class SelectionStrategy : MonoBehaviour
    {
        private AdaptationManager _adaptationManager;
        
         
        #region MonoBehaviour Implementation

        
        protected virtual void Awake()
        {
            if (_adaptationManager == null)
                _adaptationManager = GetComponent<AdaptationManager>();
        }

        protected virtual void OnEnable()
        {
            if (_adaptationManager == null)
                _adaptationManager = GetComponent<AdaptationManager>();
            _adaptationManager.RegisterSelectionStrategy(this);
        }

        protected virtual void OnDisable()
        {
            if (_adaptationManager != null)
                _adaptationManager.UnregisterSelectionStrategy();
        }

        #endregion

        public abstract void Adapt(UIConfiguration[] layouts);
    }
   
}