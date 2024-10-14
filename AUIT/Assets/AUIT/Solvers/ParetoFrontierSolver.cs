using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AUIT.AdaptationObjectives;
using AUIT.AdaptationObjectives.Definitions;
using AUIT.Extras;
using Cysharp.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;
using UnityEngine;

namespace AUIT.Solvers
{
    public class ParetoFrontierSolver : IAsyncSolver
    {
        // private NetMQRuntime _serverRuntime;
        private NetMQRuntime _clientRuntime;
        // private Thread _serverThread;
        private PythonServer _pythonServer;
        private Thread _clientThread;
        
        public new void Destroy()
        {
            _pythonServer.UnbindSolver(this);
        }

        public new void Initialize()
        {
            _pythonServer = PythonServer.GetInstance();
            _pythonServer.BindSolver(this);
        }

        public override async UniTask<(List<List<Layout>>, float)> OptimizeCoroutine(List<Layout> initialLayouts, List<List<LocalObjective>> objectives)
        {
            Debug.Log($"sending optimization request");
            // Check number of objectives across layouts
            int nObjectives = objectives.Sum(layout => layout.Count);
            var optimizationRequest = new
            OptimizationRequest {
                managerId = "-1",
                initialLayout = UIConfiguration.FromLayout(initialLayouts),
                nObjectives = nObjectives
            };
            
            string result = "";
            Client();
            
            // var clientThread = new Thread(Client);
            // clientThread.Start();
            // string result = "";
            
            void Client()
            {
                using (_clientRuntime = new NetMQRuntime())
                {
                    Debug.Log("attempting to start client");
                    _clientRuntime.Run(ClientAsync()); 
                
                    async Task ClientAsync() {
                        var requestSocket = new RequestSocket();
                        requestSocket.Connect("tcp://localhost:5555");
                
                        requestSocket.SendFrame("O" + JsonUtility.ToJson(optimizationRequest));
                    
                        // Debug.Log("request sent: " + "O" + JsonUtility.ToJson(optimizationRequest));
                        (result, _) = await requestSocket.ReceiveFrameStringAsync();
                        _clientRuntime.Dispose();
                        requestSocket.Close();
                    }
                }
            }
            
            await UniTask.WaitUntil(() => result != "");
            
            // if (clientThread.IsAlive)
            //     clientThread.Join();
            
            var optimizationResponse = JsonUtility.FromJson<OptimizationResponse>(result.Substring(1));
            var solutions = JsonUtility.FromJson<Wrapper<string>>(optimizationResponse.solutions);
            List<List<Layout>> suggestedUIConfigurations = new List<List<Layout>>(); // List of UI configurations to store suggested adaptations
            // For each adaptation (i.e., new UI configuration) in the returned solutions
            foreach (var suggestedUIConfigurationString in solutions.items)
            {
                // Convert the string to a list of Layout objects and add it to the list of suggested UI configurations
                var suggestedUIConfiguration = JsonUtility.FromJson<Wrapper<Layout>>(suggestedUIConfigurationString);
                suggestedUIConfigurations.Add(suggestedUIConfiguration.items.ToList());
            }

            // Suggested layout for next active adaptation
            var suggestedAdaptation = JsonUtility.FromJson<Wrapper<Layout>>(optimizationResponse.suggested);

            // If suggestedAdaptation is in suggestedUIConfigurations, move it to the first position
            if (suggestedUIConfigurations.Contains(suggestedAdaptation.items.ToList()))
            {
                suggestedUIConfigurations.Remove(suggestedAdaptation.items.ToList());
                suggestedUIConfigurations.Insert(0, suggestedAdaptation.items.ToList());
            }
            else
            {
                // If suggestedAdaptation is not in suggestedUIConfigurations, add it to the first position
                suggestedUIConfigurations.Insert(0, suggestedAdaptation.items.ToList());
            }

            return (suggestedUIConfigurations, 0f);
        }
    }
}