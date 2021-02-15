using System;
using System.Threading;
using Sequential;
using System.Collections.Generic;

namespace Concurrent
{
    public class ConcurrentClient : SimpleClient
    {
        public Thread workerThread;

        public ConcurrentClient(int id, Setting settings) : base(id, settings)
        {
            // todo [Assignment]: implement required code

        }
        public void run()
        {
            this.prepareClient();
            this.communicate();
        }
    }
    public class ConcurrentClientsSimulator : SequentialClientsSimulator
    {
        private ConcurrentClient[] clients;

        public ConcurrentClientsSimulator() : base()
        {
            Console.Out.WriteLine("\n[ClientSimulator] Concurrent simulator is going to start with {0}", settings.experimentNumberOfClients);
            clients = new ConcurrentClient[settings.experimentNumberOfClients];
        }

        public void ConcurrentSimulation()
        {
            List<Thread> threads = new List<Thread>();

            // todo [Assignment]: implement required code
            for (int i = 0; i < settings.experimentNumberOfClients; i++)
            {
                clients[i] = new ConcurrentClient(i + 1, settings); // id>0 means this is not a terminating client
            }

            foreach (ConcurrentClient client in clients)
            {
                threads.Add(new Thread(() => client.run()));
            }
            foreach (Thread thread in threads)
            {
                thread.Start();
            }

            foreach (Thread thread in threads)
            {
                thread.Join();
            }

            Console.Out.WriteLine("\n[ClientSimulator] All clients finished with their communications ... ");

            Thread.Sleep(settings.delayForTermination);

            ConcurrentClient endClient = new ConcurrentClient(-1, settings); // this is a terminating client: it will terminate the whole simulation
            endClient.prepareClient();
            // todo 13: check what happens in server side after this client.
            endClient.communicate();
        }
    }
}
