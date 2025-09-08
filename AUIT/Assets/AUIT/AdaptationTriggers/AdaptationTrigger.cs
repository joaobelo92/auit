using System;
using AUIT.Extras;
using UnityEngine;

namespace AUIT.AdaptationTriggers
{

    [RequireComponent(typeof(AUIT))]
    public abstract class AdaptationTrigger : MonoBehaviour
    {
        [HideInInspector]
        protected AUIT Auit;


        public event Action<UIConfiguration[]> WhenAdaptationTriggered = delegate
        {
        };

        protected virtual void Awake()
        {
            if (Auit == null)
            {
                Auit = GetComponent<AUIT>();
            }
        }

        protected virtual void OnEnable()
        {
            if (Auit == null)
                return;

            Auit.RegisterTrigger(this);
        }

        protected virtual void OnDisable()
        {
            if (Auit == null)
                return;

            Auit.UnregisterTrigger(this);
        }

        // Current idea: Manager knows how to invoke solver and keeps track of update rate
        // Update rate should be dependent on strategy tho, for now that could be updated when a strategy is registered,
        // making it possible to support multiple triggers in the future
        public abstract void ApplyStrategy();

        protected void OnApplyAdaptation(UIConfiguration[] solutions)
        {
            WhenAdaptationTriggered(solutions);
        }
    }
}