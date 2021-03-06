namespace Test
{
    using System;
    using System.Threading.Tasks;
    using RoRamu.Utils.Logging;
    using RoRamu.Utils.Messaging;
    using RoRamu.WebSocket;
    using RoRamu.WebSocket.Service;

    public class Program
    {
        private static readonly ConsoleLogger Logger = RoRamu.Utils.Logging.Logger.Default;

        static void Main()
        {
            Logger.LogExtraInfo = true;
            Logger.LogLevel = LogLevel.Info;

            var service = new TestService();
            service.Start().Wait();

            // Console.ReadLine();

            TestClient client = new TestClient(new WebSocketConnectionInfo("ws://localhost:80"));

            PreTest(client).GetAwaiter().GetResult();

            while (true)
            {
                string input = Console.ReadLine();
                if (input == "exit")
                {
                    break;
                }

                foreach (WebSocketClientProxy proxy in service.Connections.Values)
                {
                    //var proxy = client;
                    Request request = new Request("test_request", new
                    {
                        TestRequestMessage = input,
                    });

                    client.SendRequest(request).ContinueWith(task =>
                    {
                        if (task.IsFaulted)
                        {
                            Logger?.Log(LogLevel.Error, $"Request '{request.Id}' to failed", task.Exception);
                        }
                        else
                        {
                            RequestResult requestResult = task.Result;
                            if (requestResult.IsSuccessful)
                            {
                                Logger?.Log(LogLevel.Info, $"Request '{request.Id}' succeeded!", requestResult.Response);
                            }
                            else
                            {
                                object extraInfo = requestResult.Exception ?? ((object)requestResult.Response);
                                Logger?.Log(LogLevel.Warning, $"Request '{request.Id}' received an error", extraInfo);
                            }
                        }
                    });
                }
            }

            client.Close().Wait();
            service.Stop().Wait();
        }

        private static async Task PreTest(TestClient client)
        {
            { // Simple test
                RequestResult result = await client.SendRequest(new Request("test", "this is a test"), TimeSpan.FromSeconds(10));
                if (!result.IsSuccessful)
                {
                    throw result.Exception;
                }
            }

            { // Make sure exceptions are thrown correctly
                try
                {
                    RequestResult result = await client.SendRequest(new Request("Exception", null), TimeSpan.FromSeconds(10));
                    if (!result.IsSuccessful)
                    {
                        throw result.Exception;
                    }
                }
                catch (ErrorResponseException ex)
                {
                    Logger?.Log(LogLevel.Info, "Received expected exception.", ex.ErrorInfo);
                }
            }
        }
    }
}
