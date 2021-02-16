/*
    Full name: Guan Hao Wu
    Student code: 0976154
    Class: INF3D

    Full name: Bilal Azrioual
    Student code: 0966189
    Class: INF3D

*/

using Sequential;
using System;
//todo [Assignment]: add required namespaces
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Concurrent
{
    public class ConcurrentServer : SequentialServer
    {
        // todo [Assignment]: implement required attributes specific for concurrent server
        public List<Thread> threadList;
        private List<string> voteList;
        private readonly object clientCounterLock;
        private readonly object threadCounterLock;
        private readonly object votesLock;

        public ConcurrentServer(Setting settings) : base(settings)
        {
            // todo [Assignment]: implement required code
            threadList = new List<Thread>();
            voteList = new List<string>();
            clientCounterLock = new object();
            threadCounterLock = new object();
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
                        lock(clientCounterLock){
                            this.numOfClients++; // The only shared resource being written during the execution that needs a Lock.
                        }
                        this.handleClient(connection);
                    });

                    lock(threadCounterLock){
                        threadList.Add(clientThread);
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
                        lock(clientCounterLock){
                            Console.WriteLine("[Server] END : number of clients communicated -> {0} ", this.numOfClients); // Locking here to read the proper value.
                        }

                        // Join each thread and remove from the Thread list.
                        lock (threadCounterLock){
                            for(int i = 0; i < threadList.Count - 1; i++){
                                threadList[0].Join();
                                threadList.RemoveAt(0);
                            }
                        }

                        // Creates an empty dictionary to have command as key and the value is the number of votes for the said command.
                        Dictionary<string, int> countedVotes = new Dictionary<string, int>();

                        // For each command, if exists, +1 to the value, else add new key with value 1 to the dictionary.
                        foreach(string vote in voteList){
                            lock (votesLock){
                                vote.Trim();
                                Console.WriteLine("[Server] Vote received: {0}.", vote);
                                if (countedVotes.ContainsKey(vote)){
                                    countedVotes[vote] += 1;
                                } else {
                                    countedVotes.Add(vote, 1);
                                }
                            }
                        }

                        // Initiate variable for top vote at 0.
                        KeyValuePair<string, int> topVote = new KeyValuePair<string, int>("", 0);
                        Dictionary<string, int>.KeyCollection keys = countedVotes.Keys;
                        
                        // Overwrite the variable topVote with the new bigger value. 
                        // Does not account for edge case where there are 2 equal value of topVote as it isn't described in the assignment.
                        foreach(string _key in keys) {
                            int value = countedVotes[_key];
                            //Log every type of command with its total votes.
                            Console.WriteLine("[Server] '{0}' has {1} votes.", _key, value);
                            if (topVote.Value < value){
                                topVote = new KeyValuePair<string, int>(_key, value);
                            }
                        }
                        // Show in terminal the top voted command.
                        Console.WriteLine("[Server] Top vote is: '{0}' with {1} votes.", topVote.Key, topVote.Value);

                        // Execute command based on the OS.

                        string cmdLine;
                        if (OperatingSystem.IsWindows()){
                            // Windows machine
                            Console.WriteLine("Executing the top command...");
                            cmdLine = "/C " + topVote.Key;
                            try{
                                System.Diagnostics.Process.Start("CMD.exe",cmdLine);
                            }
                            catch{
                                Console.WriteLine("Unable to execute command {0}", topVote.Key);
                            }
                            
                        }
                        else if (OperatingSystem.IsLinux()){
                            // Linux or Unix
                            Console.WriteLine("Executing the top command...");
                            cmdLine = "-c " + topVote.Key;
                            try{
                                System.Diagnostics.Process.Start("/bin/sh", cmdLine);
                            }
                            catch{
                                Console.WriteLine("Unable to execute command {0}", topVote.Key);
                            }

                        }
                        else if (OperatingSystem.IsMacOS()){
                            // MacOS
                            Console.WriteLine("Executing the top command...");
                            cmdLine = "-c " + topVote.Key;
                            try{
                                System.Diagnostics.Process.Start("/Applications/Utilities/Terminal.app/Contents/MacOS/Terminal", cmdLine);
                            }
                            catch{
                                Console.WriteLine("Unable to execute command {0}", topVote.Key);
                            }
                        }
                        else {
                            Console.WriteLine("Unknown OS...");
                        }
                        

                        // Reset all the lists and number of clients so the client can be run again.
                        lock (threadCounterLock){
                            threadList.Clear();
                        }
                        lock (votesLock) {
                            voteList.Clear();
                        }
                        lock(clientCounterLock){
                            numOfClients = 0;
                        }

                        break;
                    default:
                        replyMsg = Message.confirmed;
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        Console.WriteLine("[Server] received from the client -> {0} ", msg);
                        Console.ResetColor();
                        string newVote = msg.Split(settings.command_msg_sep)[1];
                        lock (votesLock){
                            voteList.Add(newVote);
                        }
                        break;
                }
            }
            catch (Exception e) { Console.Out.WriteLine("[Server] Process Message {0}", e.Message); }

            return replyMsg;
        }
    }

    public static class OperatingSystem{
        public static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static bool IsMacOS() => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        public static bool IsLinux() => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    }
}