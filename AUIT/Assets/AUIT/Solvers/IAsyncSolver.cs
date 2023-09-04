using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using AUIT.AdaptationObjectives;
using AUIT.AdaptationObjectives.Definitions;

namespace AUIT.Solvers
{
    public interface IAsyncSolver
    {
        AdaptationManager AdaptationManager { set; }
        (List<List<Layout>>, float, float) Result { get; }
        
        void Initialize();
        IEnumerator OptimizeCoroutine(List<Layout> initialLayouts, List<List<LocalObjective>> objectives, List<float> hyperparameters);
    }
}