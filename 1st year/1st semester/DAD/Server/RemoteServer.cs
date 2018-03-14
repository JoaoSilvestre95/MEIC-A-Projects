using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Net.Sockets;
using System.Collections.Generic;
using GameApi;
using RoundState;
using System.Threading.Tasks;
using System.Threading;

namespace Server
{
    class RemoteServer : MarshalByRefObject, IServerApi
    {
        public static int round = 0;
        public static Dictionary<int, GameState> gameHistory = new Dictionary<int, GameState>();
        public static Dictionary<string,string> roundQueue = new Dictionary<string, string>();

        public static Dictionary<string, Tuple<IClientApi,bool>> clients = new Dictionary<string, Tuple<IClientApi,bool>>();
        public static Dictionary<String, String> clientsURL = new Dictionary<string, string>();

        public static Dictionary<string, Tuple<IServerApi,string, bool>> servers = new Dictionary<string, Tuple<IServerApi,string, bool>>();        
        public static List<string> serversPossiblePrimary = new List<string>();

        public static bool gameStarted = false;
        public static bool isFroozen = false;
        public static AutoResetEvent[] frozen = new AutoResetEvent[10];
        
        public static List<String> delayPID = new List<string>();

        public static bool changeServer = false; //Changing servers, handle only one exception
        public static bool isPrimary = false;



        public RemoteServer()
        {       
            for(int i = 0; i < frozen.Length;i++)
                frozen[i] = new AutoResetEvent(false);
    }

        public bool RegisterClient(String PID, String url){
            if (!RemoteServer.isPrimary)
                return false;
            try
            {
                lock (clients)
                {
                    if (!gameStarted && !clients.ContainsKey(PID))
                    {
                        IClientApi client = (IClientApi)Activator.GetObject(typeof(IClientApi), url);
                        //Task.Run(() => client.open());
                        lock (clientsURL)
                        {
                            clientsURL.Add(PID, url);
                        }   
                        clients.Add(PID, new Tuple<IClientApi, bool>(client, true));
                        Console.WriteLine("Player {0} connected", PID);
                    }
                    else if (gameStarted && clients.ContainsKey(PID))
                    {
                        IClientApi client = (IClientApi)Activator.GetObject(typeof(IClientApi), url);
                        //Task.Run(() => client.open());
                        lock (clientsURL)
                        {
                            clientsURL[PID] = url;
                        }
                        clients[PID] = new Tuple<IClientApi, bool>(client, true);
                        Console.WriteLine("Player {0} reconnected", PID);
                    }
                }
                Task.Run(() => {
                    List<KeyValuePair<String, String>> list = processClientsListToSend();
                    Parallel.ForEach(servers.Values, (sv) =>
                    {
                       if (sv.Item3)
                       {
                           sv.Item1.sendClientsList(list);
                       }
                    });
                });
            }
            catch (RemotingException) {
                Console.WriteLine("Problems getting Object IClientApi from: " + url);
                return false;
            }
            catch (ArgumentNullException)
            {
                Console.WriteLine("Problems trying to regist client: " + url +" arguments null.");
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine("Problems trying to regist client: " + url + " exception: ");
                Console.WriteLine(e.ToString());
                return false;
            }
            return true;
        }

        public void SendMovement(string PID, int roundId, string direction)
        {
            if (round == roundId && !roundQueue.ContainsKey(PID))
            {
                lock (roundQueue)
                {
                    try
                    {
                        roundQueue.Add(PID, direction);
                    }
                    catch (ArgumentNullException)
                    {
                        Console.WriteLine("Problems sending movement. Direction sent by "+ PID +" is NULL.");
                    }
                    catch (ArgumentException)
                    {
                        Console.WriteLine("Problems sending movement. Some argument provided is not valid.");
                    }
                }
            }
        }

        public static List<String> getPIDClients()
        {
            List<string> a = new List<string>();
            foreach (var x in clients)
            {
                a.Add(x.Key);
            }
            return a;
        }

        public void freeze(bool freeze)
        {
            isFroozen = freeze;
            if(freeze == false)
            {
                for (int i = 0; i < frozen.Length; i++)
                    frozen[i].Set();
            }
        }

        public bool crash()
        {
            try
            {
                Environment.Exit(-1);
                return true;
            }
            catch (System.Security.SecurityException se)
            {
                Console.WriteLine("Security error was detected, when method crash was invoked.");
                Console.WriteLine(se.ToString());
            }
            return false;
        }

        public bool InjectDelay(string server, string PID)
        {
            if (clients.ContainsKey(PID) || servers.ContainsKey(PID))
            {
                delayPID.Add(PID);
                return true;
            }
            return false;
        }

        public String LocalState(int round)
        {
            String roundState = "";
            if (gameHistory.ContainsKey(round))
            {
                roundState += "M, " + gameHistory[round].getXRedGhost() + ", " + gameHistory[round].getYRedGhost() + Environment.NewLine; 
                roundState += "M, " + gameHistory[round].getXYellowGhost() + ", " + gameHistory[round].getYYellowGhost() + Environment.NewLine;
                roundState += "M, " + gameHistory[round].getXPinkGhost() + ", " + gameHistory[round].getXPinkGhost() + Environment.NewLine;

                for (int i = 0; i < gameHistory[round].getCountWalls(); i++)
                {
                    roundState += "W, " + gameHistory[round].getXWall(i) + ", ";
                    roundState += gameHistory[round].getYWall(i) + Environment.NewLine;
                }
                for (int i = 0, player = 1; i < gameHistory[round].getCountPacmans(); i++, player++)
                {
                    roundState += "P" + player + ", ";
                    if (gameHistory[round].getState(gameHistory[round].getPacmanPID(i)) == 0)
                    {
                        roundState += "P, ";
                    }
                    else if (gameHistory[round].getState(gameHistory[round].getPacmanPID(i)) == -1)
                    {
                        roundState += "L, ";
                    }

                    roundState += gameHistory[round].getXPacman(gameHistory[round].getPacmanPID(i)).ToString() + ", ";
                    roundState += gameHistory[round].getYPacman(gameHistory[round].getPacmanPID(i)).ToString() + Environment.NewLine;
                }

                for (int i = 0; i < gameHistory[round].getCountCoins(); i++)
                {
                    if (gameHistory[round].getVisibleCoin(gameHistory[round].getCoinID(i)))
                    {
                        roundState += "C, " + gameHistory[round].getXCoin(gameHistory[round].getCoinID(i)).ToString() + ", ";
                        roundState += "C, " + gameHistory[round].getYCoin(gameHistory[round].getCoinID(i)).ToString() + Environment.NewLine;
                    }
                }
            }
            return roundState;
        }


        public string globalStatus()
        {
            String status = "Server: " + Program.PID_init;
            if(isPrimary)
                status += Environment.NewLine + "Server Role: Primary" ;
            else
                status += Environment.NewLine + "Game started: Secundary" ;
            status += Environment.NewLine + "Game started: " + gameStarted;
            status += Environment.NewLine + "Number of Players: " + Program.NUM_PLAYERS;
            status += Environment.NewLine + "Round Number: " + round;
            status += Environment.NewLine;
            
            return status;
        }

        public bool ping(string pid)
        {
            if (isFroozen) frozen[1].WaitOne();
            if (RemoteServer.servers.ContainsKey(pid))
            {
                RemoteServer.servers[pid] = new Tuple<IServerApi,string, bool>(RemoteServer.servers[pid].Item1, RemoteServer.servers[pid].Item2, true);
            }
            return true;               
        }

        public bool registerServer(string PID, string url)
        {
            
            try
            {                
                lock (servers)
                    {
                        if (!servers.ContainsKey(PID))
                        {
                            IServerApi server = (IServerApi)Activator.GetObject(typeof(IServerApi), url);
                            Task.Run(() => server.open());                         
                            servers.Add(PID, new Tuple<IServerApi,string, bool>(server, url, true));
                            server.sendClientsList(processClientsListToSend());
                        }
                    }             
                return true;
            }
            catch (RemotingException)
            {
                Console.WriteLine("Problems getting Object IServerApi from: " + url);
                return false;
            }
            catch (ArgumentNullException)
            {
                Console.WriteLine("Problems trying to regist server on master server: " + url + " arguments null.");
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine("Problems trying to regist server on master server: " + url + " exception: ");
                Console.WriteLine(e.ToString());
                return false;
            }            
        }

        public bool sendUpdates(GameState gs, bool started)
        {            
            if (!gameHistory.ContainsKey(gs.getRound()))
                gameHistory.Add(gs.getRound(), gs);
            if (gameStarted != started)
            {
                gameStarted = started;
            }
            round = gs.getRound();
            return true;
        }

        public void sendClientsList(List<KeyValuePair<String, String>> dictionary)
        {
            lock(clients) lock (clientsURL)
                {
                    clientsURL = new Dictionary<string, string>();
                    clients = new Dictionary<string, Tuple<IClientApi, bool>>();

                    foreach (KeyValuePair<String, String> x in dictionary)
                    {
                        if (!clients.ContainsKey(x.Key))
                        {
                            IClientApi client = (IClientApi)Activator.GetObject(typeof(IClientApi), x.Value);
                            clientsURL.Add(x.Key, x.Value);
                            clients.Add(x.Key, new Tuple<IClientApi, bool>(client, true));
                        }
                    }
                }
        }

        private List<KeyValuePair<String, String>> processClientsListToSend()
        {
            List<KeyValuePair<String, String>> a = new List<KeyValuePair<String, String>>();
            foreach (KeyValuePair < String, String > x in clientsURL)
            {
                a.Add(x);
            }
            return a;
        }

        public void open()
        {
        }

        public void ChangeServer(string PID)//TODO add freeze
        {
            if (isFroozen) frozen[0].WaitOne();
            isPrimary = false;
            List<string> newurls = new List<string>();
            foreach (var x in RemoteServer.servers)
                newurls.Add(x.Value.Item2);
            RemoteServer.serversPossiblePrimary = newurls;                       
        }
    }

}
