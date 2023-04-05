using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AUIT.AdaptationObjectives;
using AUIT.AdaptationObjectives.Definitions;
using AUIT.Extras;
using AUIT.Solvers.Experimental;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using UnityEngine;

namespace AUIT.Solvers
{
    public class ParetoFrontierSolver : IAsyncSolver
    {
        public AdaptationManager adaptationManager { get; set; }
        public NetMQRuntime serverRuntime;
        public NetMQRuntime clientRuntime;
        public Thread serverThread;

        public (List<List<Layout>>, float, float) Result { get; set; }

        public void Initialize()
        {
            
            serverThread = new Thread(Networking);
            serverThread.Start();

            void Networking()
            {
                // AsyncIO.ForceDotNet.Force();
                // using (var responseSocket = new ResponseSocket("@tcp://*:5556"))
                // {
                //     while (true)
                //     {
                //         var message = responseSocket.ReceiveFrameString();
                //         Console.WriteLine("responseSocket : Server Received '{0}'", message);                //             Debug.Log($"Received a request at endpoint: {message[0]}");
                //     
                //         string response;
                //         switch (message[0])
                //         {
                //             case 'P':
                //                 response = "I heard that";
                //                 responseSocket.SendFrame(response);
                //                 break;
                //             case 'E':
                //                 string payload = message.Substring(1);
                //                 // Debug.Log("computing costs: " + payload);
                //                 var evaluationResponse = new EvaluationResponse
                //                 {
                //                     costs = adaptationManager.EvaluateLayouts(payload)
                //                 };
                //                 response = JsonConvert.SerializeObject(evaluationResponse);
                //                 // Debug.Log("Sending evaluation response: " + response);
                //                 responseSocket.SendFrame("e" + response);
                //                 break;
                //             default:
                //                 Debug.Log("Unknown request");
                //                 responseSocket.SendFrame("Unknown request");
                //                 break;
                //         }
                //     }
                // }
                using (serverRuntime = new NetMQRuntime())
                {
                     Debug.Log("attempting to start server");
                     serverRuntime.Run(ServerAsync()); 
                }
                
                async Task ServerAsync()
                {
                    AsyncIO.ForceDotNet.Force();
                    using (var server = new ResponseSocket("tcp://*:5556"))
                    {
                        Debug.Log("server started");
                        
                        while (true)
                        {
                            string message;
                            (message, _) = await server.ReceiveFrameStringAsync();
                            // Debug.Log($"Received a request at endpoint: {message[0]}");
                        
                            string response;
                            switch (message[0])
                            {
                                case 'P':
                                    response = "I heard that";
                                    server.SendFrame(response);
                                    break;
                                case 'E':
                                    string payload = message.Substring(1);
                                    // Debug.Log("computing costs: " + payload);
                                    var evaluationResponse = new EvaluationResponse
                                    {
                                        costs = adaptationManager.EvaluateLayouts(payload)
                                    };
                                    response = JsonConvert.SerializeObject(evaluationResponse);
                                    // Debug.Log("Sending evaluation response: " + response);
                                    server.SendFrame("e" + response);
                                    break;
                                default:
                                    Debug.Log("Unknown request");
                                    server.SendFrame("Unknown request");
                                    break;
                            }
                        }
                    }
                }
            }
        }

        public IEnumerator OptimizeCoroutine(Layout initialLayout, List<LocalObjective> objectives, List<float> hyperparameters)
        {
            Result = (null, 0f, 0f);
            
            Debug.Log($"sending optimization request");
            var optimizationRequest = new
            OptimizationRequest {
                initialLayout = UIConfiguration.FromLayout(initialLayout),
                nObjectives = objectives.Count
            };
            
            var clientThread = new Thread(Client);
            clientThread.Start();
            string result = "";
            
            void Client()
            {
                using (clientRuntime = new NetMQRuntime())
                {
                    Debug.Log("attempting to start client");
                    clientRuntime.Run(ClientAsync()); 
                
                    async Task ClientAsync() {
                        var requestSocket = new RequestSocket();
                        requestSocket.Connect("tcp://localhost:5555");
                
                        requestSocket.SendFrame("O" + JsonUtility.ToJson(optimizationRequest));
                    
                        // Debug.Log("request sent: " + "O" + JsonUtility.ToJson(optimizationRequest));
                        (result, _) = await requestSocket.ReceiveFrameStringAsync();
                        clientRuntime.Dispose();
                    }
                }
            }
            
            
            // Debug.Log(JsonUtility.ToJson(optimizationRequest));
            // requestSocket.SendFrame("O" + JsonUtility.ToJson(optimizationRequest));
            // var message = requestSocket.ReceiveFrameString();
            // Debug.Log($"in main: ${message}");
            while (result == "")
            {
                yield return null;
            }

            // Debug.Log(result);
            
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

            // todo: add costs
            Result = (suggestedUIConfigurations, 0f, 0f);

        }

        public IEnumerator OptimizeCoroutine(List<Layout> initialLayouts, List<List<LocalObjective>> objectives, List<float> hyperparameters)
        {
            throw new System.NotImplementedException();
        }
    }
}