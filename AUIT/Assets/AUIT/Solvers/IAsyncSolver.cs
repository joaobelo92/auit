using System.Collections.Generic;
using UnityEngine;
using AUIT.AdaptationObjectives.Definitions;
using AUIT.Extras;
using Cysharp.Threading.Tasks;
using AUIT.AdaptationObjectives;
using AUIT.Constraints;


namespace AUIT.Solvers
{
    [System.Serializable]
    public abstract class IAsyncSolver
    {
        [SerializeReference]
        public List<Constraint> constraints;
        public void Initialize() {}
        public void Destroy() {}
        public abstract UniTask<OptimizationResponse> OptimizeCoroutine(
            List<Layout> initialLayouts,
            List<List<LocalObjective>> objectives
        );
        
        // TODO: clean merge rests
        AdaptationManager AdaptationManager { set; get; }
        (List<List<Layout>>, float, float) Result { get; }
    }
}