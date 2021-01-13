using System;
using System.Diagnostics;
using System.Threading;

namespace NeoRTS
{
    namespace Server
    {
        class ServerProgram
        {
            static void Main(string[] args)
            {
                Console.WriteLine("Hello World!");
                Console.WriteLine("Starting Server...");

                // Tools initialization
                // TODO : Move that to a specific class ? We might be building a lot of different tools in the future

                Tools.Random.Initialize(0);

                ServerManager server = new ServerManager();
                server.Start();

                // TODO : Find a way to allow the user to type in commands in the console.
                ServerThread();
            }

            static void ServerThread()
            {
                Stopwatch watch = new Stopwatch();
                watch.Start();

                float currentTime = 0f;
                while (true)
                {
                    float deltaTime = ((float)watch.Elapsed.TotalSeconds) - currentTime;
                    currentTime = (float)watch.Elapsed.TotalSeconds;
                    
                    ServerManager.Instance.Update(deltaTime);
                }
            }
        }
    }

}
