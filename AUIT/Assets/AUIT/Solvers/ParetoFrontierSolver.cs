using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AUIT.AdaptationObjectives;
using AUIT.AdaptationObjectives.Definitions;
using AUIT.Extras;
using Cysharp.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using UnityEngine;

namespace AUIT.Solvers
{
    public class ParetoFrontierSolver : IAsyncSolver
    {
        // private Thread _serverThread;
        private PythonServer _pythonServer;

        public AdaptationManager AdaptationManager { get; set; }
        public (List<List<Layout>>, float, float) Result { get; private set; }

        public void Destroy()
        {
            _pythonServer.UnbindSolver(this);
        }

        public void Initialize()
        {
            _pythonServer = PythonServer.GetInstance();
            _pythonServer.BindSolver(this);
        }

        public async UniTask<OptimizationResponse> OptimizeCoroutine(List<Layout> initialLayouts, List<List<LocalObjective>> objectives, List<float> hyperparameters)
        {
            Result = (null, 0f, 0f);
            
            Debug.Log($"sending optimization request");
            // Check number of objectives across layouts
            int nObjectives = objectives.Sum(layout => layout.Count);
            var optimizationRequest = new
            OptimizationRequest {
                managerId = AdaptationManager.Id,
                initialLayout = UIConfiguration.FromLayout(initialLayouts),
                nObjectives = nObjectives
            };
            
            // string result = "";
            // Client();
            
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
            
            // if (clientThread.IsAlive)
            //     clientThread.Join();
            
            Debug.Log("O resp " + result.Substring(1));
            OptimizationResponse optimizationResponse = JsonConvert.DeserializeObject<OptimizationResponse>(result.Substring(1), new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });
            // var optimizationResponse = JsonUtility.FromJson<OptimizationResponse>(result.Substring(1));
            // var solutions = JsonUtility.FromJson<Wrapper<string>>(optimizationResponse.solutions);
            // List<List<Layout>> suggestedUIConfigurations = new List<List<Layout>>(); // List of UI configurations to store suggested adaptations
            // // For each adaptation (i.e., new UI configuration) in the returned solutions
            // foreach (var suggestedUIConfigurationString in solutions.items)
            // {
            //     // Convert the string to a list of Layout objects and add it to the list of suggested UI configurations
            //     var suggestedUIConfiguration = JsonUtility.FromJson<Wrapper<Layout>>(suggestedUIConfigurationString);
            //     suggestedUIConfigurations.Add(suggestedUIConfiguration.items.ToList());
            // }

            // Suggested layout for next active adaptation
            // var suggestedAdaptation = JsonUtility.FromJson<Wrapper<Layout>>(optimizationResponse.suggested);

            // If suggestedAdaptation is in suggestedUIConfigurations, move it to the first position
            // if (suggestedUIConfigurations.Contains(suggestedAdaptation.items.ToList()))
            // {
            //     suggestedUIConfigurations.Remove(suggestedAdaptation.items.ToList());
            //     suggestedUIConfigurations.Insert(0, suggestedAdaptation.items.ToList());
            // }
            // else
            // {
            //     // If suggestedAdaptation is not in suggestedUIConfigurations, add it to the first position
            //     suggestedUIConfigurations.Insert(0, suggestedAdaptation.items.ToList());
            // }

            Debug.Log(optimizationResponse);

            return optimizationResponse;
        }
    }
}