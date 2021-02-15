/* 
    Class: INF3D
    Matthijs Booman - 0964703
    Bram Gussekloo - 0966476
*/

using Sequential;
using System;
//todo [Assignment]: add required namespaces
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;

namespace Concurrent
{
    public class ConcurrentServer : SequentialServer
    {
        // todo [Assignment]: implement required attributes specific for concurrent server
        private List<Thread> threads;
        private List<string> votes;
        private readonly object clientCountLock;
        private readonly object threadCountLock;
        private readonly object votesLock;

        public ConcurrentServer(Setting settings) : base(settings)
        {
            // todo [Assignment]: implement required code
            threads = new List<Thread>();
            votes = new List<string>();
            clientCountLock = new object();
            threadCountLock = new object();
            votesLock = new object();
        }
        public override void prepareServer()
        {
            // todo [Assignment]: implement required code
            Console.WriteLine("[Server] is ready to start ...");
    	    try{
                localEndPoint = new IPEndPoint(this.ipAddress, settings.serverPortNumber);
                listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(localEndPoint);
                listener.Listen(settings.serverListeningQueue);

                while (true)
                {
                    Console.WriteLine("Waiting for incoming connections ... ");

                    Socket connection = listener.Accept();

                    Thread clientThread = new Thread(() => {
                        lock(clientCountLock){ this.numOfClients++; }
                        this.handleClient(connection);
                    });

                    lock(threadCountLock){
                        threads.Add(clientThread);
                    }

                    clientThread.Start();
                }
            } catch (Exception e) { Console.Out.WriteLine("[Server] Preparation: {0}", e.Message); }
        }
        public override string processMessage(String msg)
        {
            // todo [Assignment]: implement required code
            Thread.Sleep(settings.serverProcessingTime);
            string replyMsg = Message.confirmed;

            try
            {
                switch (msg)
                {
                    case Message.terminate:
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.WriteLine("[Server] received from the client -> {0} ", msg);
                        Console.ResetColor();
                        lock(clientCountLock){
                            Console.WriteLine("[Server] END : number of clients communicated -> {0} ", this.numOfClients);
                        }

                        lock (threadCountLock){
                            int count = threads.Count - 1;
                            for(int i = 0; i < count; i++){
                                threads[0].Join();
                                threads.RemoveAt(0);
                            }
                        }

                        Dictionary<string, int> countedVotes = new Dictionary<string, int>();
                        lock (votesLock)
                        {
                            foreach(string vote in votes){
                                Console.WriteLine("[Server] Counted vote: {0}", vote);
                                if (countedVotes.ContainsKey(vote)) {
                                    countedVotes[vote] += 1;
                                } else {
                                    countedVotes.Add(vote, 1);
                                }
                            }
                        }

                        KeyValuePair<string, int> maximumVote = new KeyValuePair<string, int>("", 0);

                        Dictionary<string, int>.KeyCollection keys = countedVotes.Keys;
                        foreach(string s in keys) {
                            int value = countedVotes[s];
                            Console.WriteLine("[Server] '{0}' has {1} votes", s, value);
                            if (maximumVote.Value < value) {
                                maximumVote = new KeyValuePair<string, int>(s, value);
                            }
                        }
                        Console.WriteLine("[Server] maximum vote is '{0}' with {1} votes.", maximumVote.Key, maximumVote.Value);
                        Process cmd = new Process();

                        //var TestString ="echo hello teacher";

                        cmd.StartInfo.FileName = "cmd.exe";
                        cmd.StartInfo.CreateNoWindow = true;
                        cmd.StartInfo.RedirectStandardInput = true;
                        cmd.StartInfo.RedirectStandardOutput = true;
                        cmd.StartInfo.UseShellExecute = false;
                        cmd.Start();

                        cmd.StandardInput.WriteLine(maximumVote.Key);
                        cmd.StandardInput.Flush();
                        cmd.StandardInput.Close();
                        cmd.WaitForExit();
                        Console.WriteLine(cmd.StandardOutput.ReadToEnd());
                        Console.ReadKey();

                        
                        lock (threadCountLock) {
                            threads.Clear();
                        }
                        lock (votesLock) {
                            votes.Clear();
                        }
                        lock(clientCountLock){
                            numOfClients = 0;
                        }
                        break;
                    default:
                        replyMsg = Message.confirmed;
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        Console.WriteLine("[Server] received from the client -> {0} ", msg);
                        Console.ResetColor();
                        string newVote = msg.Split(settings.command_msg_sep)[1];
                        lock (votesLock) {
                            votes.Add(newVote);
                        }
                        break;
                }
            }
            catch (Exception e) { Console.Out.WriteLine("[Server] Process Message {0}", e.Message); }

            return replyMsg;
        }
    }
}