using System.Collections.Generic;
using AUIT.AdaptationObjectives.Definitions;
using AUIT.Extras;
using UnityEngine;

namespace AUIT.SelectionStrategies
{
    [RequireComponent(typeof(AUIT))]
    public abstract class SelectionStrategy : MonoBehaviour
    {
        private AUIT auit;
        
         
        #region MonoBehaviour Implementation

        
        protected virtual void Awake()
        {
            if (auit == null)
                auit = GetComponent<AUIT>();
        }

        protected virtual void OnEnable()
        {
            if (auit == null)
                auit = GetComponent<AUIT>();
            auit.RegisterSelectionStrategy(this);
        }

        protected virtual void OnDisable()
        {
            if (auit != null)
                auit.UnregisterSelectionStrategy();
        }

        #endregion

        public abstract void Adapt(UIConfiguration[] layouts);
    }
   
}