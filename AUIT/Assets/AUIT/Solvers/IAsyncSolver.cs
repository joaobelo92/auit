using System;
using System.Collections.Generic;
using UnityEngine;
using AUIT.AdaptationObjectives.Definitions;
using AUIT.Extras;
using Cysharp.Threading.Tasks;
using AUIT.AdaptationObjectives;
using AUIT.Constraints;


namespace AUIT.Solvers
{
    [Serializable]
    public abstract class IAsyncSolver
    {
        public virtual void Initialize(List<Constraint> constraints=null) {}
        public virtual void Destroy() {}
        // TODO: initialize objectives and constraints once
        public abstract UniTask<OptimizationResponse> OptimizeCoroutine(
            List<Layout> initialLayouts,
            List<List<LocalObjective>> objectives
        );
        public AUIT Auit { set; get; } 
    }
}