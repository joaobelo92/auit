using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using AUIT.AdaptationObjectives;
using AUIT.AdaptationObjectives.Definitions;
using Cysharp.Threading.Tasks;

namespace AUIT.Solvers
{
    public interface IAsyncSolver
    {
        AdaptationManager AdaptationManager { set; get; }
        (List<List<Layout>>, float, float) Result { get; }
        
        void Initialize();
        UniTask<(List<List<Layout>>, float)> OptimizeCoroutine(List<Layout> initialLayouts, List<List<LocalObjective>> objectives, List<float> hyperparameters);
    }
}