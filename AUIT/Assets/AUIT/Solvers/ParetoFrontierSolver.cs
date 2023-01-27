using System;
using System.Collections;
using System.Collections.Generic;
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
        public (List<Layout>, float, float) Result { get; }
        
        // Func<(string, string), Task<string>> requestFunc;

        public void Initialize()
        {
            
            var serverThread = new Thread(Networking);
            serverThread.Start();
            
            // while(requestFunc == null) {}

            void Networking()
            {
                using (var runtime = new NetMQRuntime())
                {
                    Debug.Log("attempting to start server");
                    runtime.Run(ServerAsync()); 
                }
                
                // async Task ClientAsync() {
                //     
                //     requestSocket = new RequestSocket();
                //     // "tcp://192.168.0.104:5555"
                //     requestSocket.Connect("tcp://localhost:5555");
                //
                //     async Task<string> MakeRequest(string endpoint, string request)
                //     {
                //         requestSocket.SendFrame(endpoint + request);
                //         
                //         Debug.Log("request sent!");
                //         var (res, _) = await requestSocket.ReceiveFrameStringAsync();
                //
                //         Debug.Log("got the reply!");
                //         Debug.Log(res);
                //
                //         return res;
                //     }
                //
                //     requestFunc = e => MakeRequest(e.Item1, e.Item2);   
                //     Debug.Log("recFunc assigned");
                // }
                
                
                async Task ServerAsync()
                {
                    using (var server = new ResponseSocket("tcp://*:5556"))
                    {
                        Debug.Log("server started");

                        while (true)
                        {
                            string message;
                            (message, _) = await server.ReceiveFrameStringAsync();
                            Debug.Log($"Received a request at endpoint: {message[0]}");

                            string response;
                            switch (message[0])
                            {
                                case 'P':
                                    response = "I heard that";
                                    server.SendFrame(response);
                                    break;
                                case 'E':
                                    string payload = message.Substring(1);
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
                using (var runtime = new NetMQRuntime())
                {
                    Debug.Log("attempting to start client");
                    runtime.Run(ClientAsync()); 
                
                    async Task ClientAsync() {
                    
                        var requestSocket = new RequestSocket();
                        requestSocket.Connect("tcp://localhost:5555");
                
                        requestSocket.SendFrame("O" + JsonUtility.ToJson(optimizationRequest));
                    
                        Debug.Log("request sent: " + "O" + JsonUtility.ToJson(optimizationRequest));
                        (result, _) = await requestSocket.ReceiveFrameStringAsync();
            
                        Debug.Log("got the reply!");
                        Debug.Log(result);
                    }
                }
            }
            
            
            // Debug.Log(JsonUtility.ToJson(optimizationRequest));
            // requestSocket.SendFrame("O" + JsonUtility.ToJson(optimizationRequest));
            // var message = requestSocket.ReceiveFrameString();
            // //
            // Debug.Log($"in main: ${message}");
            while (result == "")
            {
                yield return null;
            }
        }

        public IEnumerator OptimizeCoroutine(List<Layout> initialLayouts, List<List<LocalObjective>> objectives, List<float> hyperparameters)
        {
            throw new System.NotImplementedException();
        }
    }
}