using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AUIT.AdaptationObjectives.Definitions;
using AUIT.Solvers;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AUIT.Extras
{
    public class PythonServer
    {
        private static PythonServer _instance;
        private List<IAsyncSolver> _solvers;
        
        private NetMQRuntime _serverRuntime;
        private NetMQRuntime _clientRuntime;
        private readonly Thread _serverThread;
        private Thread _clientThread;

        private Dictionary<string, AdaptationManager> _managers;
        
        private PythonServer()
        {
            _solvers = new List<IAsyncSolver>();
            _managers = new Dictionary<string, AdaptationManager>();
            _serverThread = new Thread(Networking);
            _serverThread.Start();
            
        }

        public static PythonServer GetInstance()
        {
            return _instance ??= new PythonServer();
        }

        public void BindSolver(IAsyncSolver solver)
        {
            _solvers.Add(solver);
        }

        public void UnbindSolver(IAsyncSolver solver)
        {
            _solvers.Remove(solver);
            if (_solvers.Count == 0)
            {
                _serverRuntime.Dispose();
                NetMQConfig.Cleanup(false);
                if (_serverThread.IsAlive)
                    _serverThread.Join();
            }
        }
        
        void Networking()
        {
            using (_serverRuntime = new NetMQRuntime())
            {
                Debug.Log("attempting to start server");
                _serverRuntime.Run(ServerAsync()); 
            }
                
            async Task ServerAsync()
            {
                AsyncIO.ForceDotNet.Force();
                using var server = new ResponseSocket("tcp://*:5556");
                Debug.Log("server started");
                        
                while (true)
                {
                    string message;
                    (message, _) = await server.ReceiveFrameStringAsync();
                    // Debug.Log($"Received a request at endpoint: {message[0]}");
                        
                    switch (message[0])
                    {
                        case 'E':
                            string payload = message.Substring(1);
                            // Debug.Log("computing costs: " + payload);
                            try
                            {
                                EvaluationRequest evaluationRequest = JsonConvert.DeserializeObject<EvaluationRequest>(payload, new JsonSerializerSettings
                                {
                                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                                });
                            
                                string managerId = evaluationRequest.manager_id;
                                AdaptationManager manager = _solvers.First(s => s.AdaptationManager.Id == managerId).AdaptationManager;
                                var evaluationResponse = new EvaluationResponse
                                {
                                    costs = manager.EvaluateLayouts(evaluationRequest)
                                };
                                string response = JsonConvert.SerializeObject(
                                    evaluationResponse,
                                    new JsonSerializerSettings
                                    {
                                        ReferenceLoopHandling =
                                            ReferenceLoopHandling.Ignore
                                    });
                                // Debug.Log("Sending evaluation response: " + response);
                                server.SendFrame("e" + response);
                            }
                            catch (Exception e)
                            {
                                Debug.Log(e);
                            }
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