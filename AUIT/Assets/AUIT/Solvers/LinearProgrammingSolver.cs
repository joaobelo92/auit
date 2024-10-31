//
// namespace AUIT.Solvers
// {
//     public class LinearProgrammingSolver: IAsyncSolver
//     {
//         private PythonServer _pythonServer;
//
//         public new void Destroy()
//         {
//             _pythonServer.UnbindSolver(this);
//         }
//
//         public new void Initialize()
//         {
//             _pythonServer = PythonServer.GetInstance();
//             _pythonServer.BindSolver(this);
//         }
//
//         public override async UniTask<OptimizationResponse> OptimizeCoroutine(
//             List<Layout> initialLayouts,
//             List<List<LocalObjective>> objectives
//         )
//         {
//
//         }
//     }
// }