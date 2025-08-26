using System;
using System.Collections.Generic;
using System.Linq;
using AUIT.AdaptationObjectives;
using AUIT.AdaptationObjectives.Definitions;
using AUIT.Constraints;
using AUIT.Extras;
using Cysharp.Threading.Tasks;
using Numpy;
using UnityEngine;

namespace AUIT.Solvers
{
    public class ExhaustiveSearchSolver : IAsyncSolver
    {
        public float interval = 0.05f;
        [Tooltip("If true, the solver will consider interdependencies between GameObjects, this will have complexity " +
                 "O(n^m), where m is the number of GameObjects")]
        // TODO: make this dynamic if using global objectives
        public bool gameObjectInterdependencies = false;

        public int yieldRate = 2000;
        private Constraint xConstraint;
        private Constraint yConstraint;
        private Constraint zConstraint;
        
        public override void Initialize(List<Constraint> constraints = null)
        {
            Debug.Log("Initializing Exhaustive Search Solver w/ constraints");

            try
            {
                xConstraint = constraints.Single(i => i.type == ConstraintType.SpatialXAxis);
                yConstraint = constraints.Single(i => i.type == ConstraintType.SpatialYAxis);
                zConstraint = constraints.Single(i => i.type == ConstraintType.SpatialZAxis);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Constraint assignment failed: {exception.Message}\\n{exception.StackTrace}");
            }
        }

        public ExhaustiveSearchSolver(float interval=0.05f)
        {
            this.interval = interval;
        }
        
        public override async UniTask<(OptimizationResponse, NDarray , NDarray)> OptimizeCoroutine(
            List<Layout> initialLayouts, 
            List<List<LocalObjective>> objectives,
            List<MultiElementObjective> multiElementObjectives,
            bool saveCosts=false)
        {
            if (!gameObjectInterdependencies)
            {
                Layout placeholderlayout = new Layout();
            
                NDarray x_range = np.arange(xConstraint.minimum, xConstraint.maximum, interval, dtype: np.float32);
                NDarray y_range = np.arange(yConstraint.minimum, yConstraint.maximum, interval, dtype: np.float32);
                NDarray z_range = np.arange(zConstraint.minimum, zConstraint.maximum, interval, dtype: np.float32);
            
                NDarray[] xyz = { x_range, y_range, z_range };
                NDarray[] grid = np.meshgrid(xyz, indexing: "ij");
                grid[0] = grid[0].ravel();
                grid[1] = grid[1].ravel();
                grid[2] = grid[2].ravel();
            

                NDarray points = np.vstack(grid).T;
                NDarray costs = np.empty((points.len, initialLayouts.Count));
                NDarray costsPerObjective = null;

                if (saveCosts)
                {
                    if (objectives.Count != 1)
                        throw new Exception("Can only save costs for single a single element at the moment");
                    costsPerObjective = np.empty((points.len, objectives.First().Count));
                }
            
                // Search time grows exponentially for each additional layout :(
                // For now, we do it the slow way - need to vectorize objective functions to improve efficiency
                for (int i = 0; i < points.len; i++)
                {
                    // another potential bottleneck
                    float[] currentPos = points[i].GetData<float>();
                    placeholderlayout.Position = new Vector3(currentPos[0], currentPos[1], currentPos[2]);
                    for (int j = 0; j < initialLayouts.Count; j++)
                    {
                        float cost = 0;
                        for (int k = 0; k < objectives[j].Count; k++)
                        {
                            float objectiveCost = objectives[j][k].CostFunction(placeholderlayout);
                            if (saveCosts)
                            {
                                costsPerObjective[i][k] = np.array(objectiveCost);
                            }
                            cost += objectives[j][k].Weight * objectiveCost;
                        }
                        costs[i][j] = np.array(cost);
                    }
                    
                    if (i % yieldRate == 0)
                    {
                        await UniTask.Yield();
                    }
                }

                NDarray minCost = np.argmin(costs, axis: 0);

                List<Layout> result = initialLayouts.Select(item => item.Clone()).ToList();
                for (int i=0; i < initialLayouts.Count; i++)
                {
                    float[] bestPos = points[minCost[i]].GetData<float>();
                    result[i].Position = new Vector3(bestPos[0], bestPos[1], bestPos[2]);
                }

                
                UIConfiguration configResult = new UIConfiguration(result.ToArray());
                return (new OptimizationResponse(configResult), points, costsPerObjective);
                
            }
            
            // TODO: Implement when we decide on how to handle global objectives
            throw new Exception("Global objectives not currently supported");
            // Stack<Layout> layoutStack = new Stack<Layout>(initialLayouts);
            
        }

        // private float RecursiveCostFunction(Stack<Layout> layoutStack)
        // {
        //     Layout currentLayout = layoutStack.Pop();
        //     
        //     if (layoutStack.Count == 0)
        //     {
        //         return 0;
        //     }
        //             
        // }
        
    }
    
    
}