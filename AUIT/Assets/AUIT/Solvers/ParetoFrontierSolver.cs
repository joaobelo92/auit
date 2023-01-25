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
        private NetMQRuntime runtime;
        private RequestSocket requestSocket;
        
        Func<(string, string), Task<string>> requestFunc;
                           
        public async Task Initialize()
        {
            
            var serverThread = new Thread(Server);
            serverThread.Start();
            var clientThread = new Thread(Client);
            clientThread.Start();
            
            while(requestFunc == null) {}

            void Client()
            {
                using (runtime = new NetMQRuntime())
                {
                    Debug.Log("attempting to start client");
                    runtime.Run(ClientAsync()); 
                    
                }
                    
                
                async Task ClientAsync() {
                    requestSocket = new RequestSocket();
                    // "tcp://192.168.0.104:5555"
                    requestSocket.Connect("tcp://localhost:5555");

                    async Task<string> MakeRequest(string endpoint, string request)
                    {
                        requestSocket.SendFrame(endpoint + request);
                        
                        Debug.Log("request sent!");
                        var (res, _) = await requestSocket.ReceiveFrameStringAsync();

                        Debug.Log("got the reply!");
                        Debug.Log(res);

                        return res;
                    }

                    requestFunc = e => MakeRequest(e.Item1, e.Item2);   
                    Debug.Log("recFunc assigned");
                }
                
                
                
            }
            
            void Server()
            {
                using (var runtime = new NetMQRuntime())
                {
                    Debug.Log("attempting to start server");
                    runtime.Run(ServerAsync()); 
                }
            
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
                                    Debug.Log($"Got this: {payload}");
                                    // response = JsonConvert.SerializeObject(adaptationManager.EvaluateLayout(payload));
                                    response = "server test";
                                    Debug.Log("Sending evaluation response: " + response);
                                    server.SendFrame(response);
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
                layout = initialLayout,
                nObjectives = objectives.Count
            };
            Task<string> res = requestFunc(("O", JsonUtility.ToJson(optimizationRequest)));
            res.RunSynchronously();
            Debug.Log($"in main: ${res.Result}");
            yield break;
        }

        public IEnumerator OptimizeCoroutine(List<Layout> initialLayouts, List<List<LocalObjective>> objectives, List<float> hyperparameters)
        {
            throw new System.NotImplementedException();
        }
    }
}