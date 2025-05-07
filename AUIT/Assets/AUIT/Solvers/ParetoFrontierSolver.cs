using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AUIT.AdaptationObjectives;
using AUIT.AdaptationObjectives.Definitions;
using AUIT.Constraints;
using AUIT.Extras;
using Cysharp.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using Numpy;
using UnityEngine;

namespace AUIT.Solvers
{
    public class ParetoFrontierSolver : IAsyncSolver
    {
        private PythonServer _pythonServer;
        
        public override void Destroy()
        {
            _pythonServer.UnbindSolver(this);
        }

        public override void Initialize(List<Constraint> constraints=null)
        {
            Debug.Log("Pareto FrontierSolver initializing");
            _pythonServer = PythonServer.GetInstance();
            _pythonServer.BindSolver(this);
        }

        public override async UniTask<(OptimizationResponse, NDarray, NDarray)> OptimizeCoroutine(
            List<Layout> initialLayouts, 
            List<List<LocalObjective>> objectives,
            List<MultiElementObjective> multiElementObjectives,
            bool saveCosts=false
        )
        {
            Debug.Log($"sending optimization request");
            Debug.Log("adaptationManagerRefereneId: " + Auit.Id);
            Debug.Log("initialLayouts: " + initialLayouts);
            // Check number of objectives across layouts
            int nObjectives = objectives.Sum(layout => layout.Count);
            Debug.Log("nObjectives: " + nObjectives);
            var optimizationRequest = new OptimizationRequest {
                managerId = Auit.Id, // TODO: decouple from manager object "-1",
                initialLayout = UIConfiguration.FromLayout(initialLayouts),
                nObjectives = nObjectives
            };
            
            var clientThread = new Thread(Client);
            clientThread.Start();
            string result = "";
            
            void Client()
            {
                using (NetMQRuntime clientRuntime = new NetMQRuntime())
                {
                    Debug.Log("attempting to start client");
                    clientRuntime.Run(ClientAsync()); 
                
                    async Task ClientAsync() {
                        var requestSocket = new RequestSocket();
                        requestSocket.Connect("tcp://localhost:5555");

                        try
                        {
                            string payload = "O" + JsonConvert.SerializeObject(
                                optimizationRequest, new JsonSerializerSettings
                                {
                                    ReferenceLoopHandling =
                                        ReferenceLoopHandling.Ignore
                                });
                            requestSocket.SendFrame(payload);
                            Debug.Log("request sent: " + payload);
                            (result, _) = await requestSocket.ReceiveFrameStringAsync();
                            requestSocket.Close();
                        }
                        catch (Exception e)
                        {
                            Debug.LogError("Error during JSON deserialization: " + e.Message);
                        }
                    }
                }
            }
            
            await UniTask.WaitUntil(() => result != "");
            
            Debug.Log("O resp " + result.Substring(1));
            OptimizationResponse optimizationResponse = JsonConvert.DeserializeObject<OptimizationResponse>(result.Substring(1), new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });

            Debug.Log(optimizationResponse);

            return (optimizationResponse, null, null);
        }
    }
}