using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using ProcessCreationService;
using System.Drawing;
using GameApi;
using Server;
using PacmanGame;
using System.Threading;
using RoundState;
using System.Timers;
using System.Collections.Concurrent;

namespace Server
{
    class Program
    {
        static int MSEC_PER_ROUND = 0;
        public static int NUM_PLAYERS = 0;
        public static String PID_init = "";
        public static String URL_init = "";
        static int TIMEDELAY = 2000;
        static string[] url_servers_List = null;
        static IServerApi server = null;
        private static System.Timers.Timer timer;
        static Game game = null;
        static AutoResetEvent waitToBePrimary = new AutoResetEvent(false);
        static int TIMEOUT = 0;

        static void Main(string[] args)
        {
            PID_init = args[0];
            URL_init = args[1];
            RegistRemoteObject(args[1]);
            MSEC_PER_ROUND = Int32.Parse(args[2]);
            TIMEOUT = MSEC_PER_ROUND * 5;
            NUM_PLAYERS = Int32.Parse(args[3]);
            if (args.Length > 4)
            {
                RemoteServer.isPrimary = false;
                RemoteServer.gameStarted = false;
                RemoteServer.changeServer = false;
                RemoteServer.isFroozen = false;
                url_servers_List = args[4].Split(';');
                RemoteServer.serversPossiblePrimary = url_servers_List.Where(w => w != "").ToList();
                serverSet(RemoteServer.isPrimary);
            }
            else
            {
                RemoteServer.isPrimary = true;
                RemoteServer.gameStarted = false;
                RemoteServer.changeServer = false;
                RemoteServer.isFroozen = false;
                serverSet(RemoteServer.isPrimary);
            }
        }

        private static void serverSet(bool primary)
        {
            if (primary)
            {
                Console.WriteLine("Server Up: Primary");
            }
            else
            {
                registAsSecundary();
                Console.WriteLine("Server Up: Secondary");
                timerActivate();
                waitToBePrimary.WaitOne();
                timer.Close();
                changeServerOnClients();
            }
            if (!RemoteServer.gameStarted)
                startGame();
            Console.WriteLine("Game in progress");
            gameCycle();
        }

        private static bool registAsSecundary()
        {
            try
            {
                server = (IServerApi)Activator.GetObject(typeof(IServerApi), RemoteServer.serversPossiblePrimary.First());
                Task.Run(() => server.open());
                server.registerServer(PID_init, URL_init);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void startGame()
        {
            Console.WriteLine("Waiting for {0} players to join..", NUM_PLAYERS);
            for (; ; )
            {
                if (RemoteServer.isFroozen)
                    RemoteServer.frozen[2].WaitOne();
                if (!RemoteServer.isPrimary)
                {
                    serverSet(RemoteServer.isPrimary);
                }
                if (RemoteServer.clients.Count == NUM_PLAYERS)
                {
                    lock (RemoteServer.clients) lock (RemoteServer.clientsURL)
                        {
                            Parallel.ForEach(RemoteServer.clients, (x) =>
                            {
                                x.Value.Item1.ReceiveClientsUrls(RemoteServer.clientsURL);
                            });
                        }
                    game = new Game(RemoteServer.getPIDClients());
                    sendStartGame(game);
                    Thread.Sleep(3000);
                    RemoteServer.gameStarted = true;
                    break;
                }
            }
            Console.WriteLine("Game started");
        }

        private static void gameCycle()
        {
            for (; ; )
            {
                if (RemoteServer.isFroozen)
                {
                    Console.WriteLine("freezed");
                    RemoteServer.frozen[4].WaitOne();
                }

                if (!RemoteServer.isPrimary)
                {
                    serverSet(RemoteServer.isPrimary);
                }

                Thread sendStates;
                Thread sendUpdates;
                //espera time round
                if (RemoteServer.roundQueue != null)
                    game.GameLoop(RemoteServer.roundQueue);

                int x = RemoteServer.round;
                RemoteServer.round++;

                lock (RemoteServer.roundQueue)
                    RemoteServer.roundQueue.Clear();

                GameState gs = game.GetState(x);
                sendUpdates = new Thread(() => sendUpdatesToServer(gs))
                {
                    Priority = ThreadPriority.Highest
                };
                sendUpdates.Start();
                sendStates = new Thread(() => sendGameStateToClients(game, x, gs))
                {
                    Priority = ThreadPriority.Highest
                };
                sendStates.Start();

                System.Threading.Thread.Sleep(MSEC_PER_ROUND + 10);
            }
        }


        private static void sendStartGame(Game pacman)
        {
            Task[] tasks = new Task[RemoteServer.clients.Count];
            int i = 0;
            foreach (var x in RemoteServer.clients)
            {
                tasks[i] = Task.Factory.StartNew(() =>
                {
                    GameState gs = pacman.GetState(-1);
                    try
                    {
                        x.Value.Item1.ReadytoStart(gs);

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                    lock (RemoteServer.gameHistory)
                    {
                        if (!RemoteServer.gameHistory.ContainsKey(-1))
                            RemoteServer.gameHistory.Add(-1, gs);
                    }
                });
                i++;
            }
            Task.WaitAll(tasks);
        }

        private static void sendUpdatesToServer(GameState gs)
        {
            foreach (var x in RemoteServer.servers.ToList())
            {
                Thread t2 = new Thread(() =>
                {
                    if (!x.Value.Item3)
                    {
                        return;
                    }

                    if (InjectDelay(x.Key))
                    {
                        Thread.Sleep(TIMEDELAY);
                        if (!sendGameStateToServer(gs, x.Key, 0))
                        {
                            lock (RemoteServer.servers)
                            {
                                if (RemoteServer.servers[x.Key].Item3)
                                {
                                    Console.WriteLine("Server {0} disconnected", x.Key);
                                    RemoteServer.servers[x.Key] = new Tuple<IServerApi, string, bool>(RemoteServer.servers[x.Key].Item1, RemoteServer.servers[x.Key].Item2, false);
                                }
                            }
                        }
                        else
                        {
                            lock (RemoteServer.servers)
                            {
                                RemoteServer.servers[x.Key] = new Tuple<IServerApi, string, bool>(RemoteServer.servers[x.Key].Item1, RemoteServer.servers[x.Key].Item2, true);
                            }
                        }

                    }
                    else
                    {
                        if (!sendGameStateToServer(gs, x.Key, 0))
                        {
                            lock (RemoteServer.servers)
                            {
                                if (RemoteServer.servers[x.Key].Item3)
                                {
                                    Console.WriteLine("Server {0} disconnected", x.Key);
                                    RemoteServer.servers[x.Key] = new Tuple<IServerApi, string, bool>(RemoteServer.servers[x.Key].Item1, RemoteServer.servers[x.Key].Item2, false);
                                }
                            }
                        }
                        else
                        {
                            lock (RemoteServer.servers)
                            {
                                RemoteServer.servers[x.Key] = new Tuple<IServerApi, string, bool>(RemoteServer.servers[x.Key].Item1, RemoteServer.servers[x.Key].Item2, true);
                            }
                        }
                    }
                });
                t2.Start();
            };
        }

        private static void timerActivate()
        {
            timer = new System.Timers.Timer
            {
                Interval = 100
            };
            timer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            timer.Enabled = true;

        }

        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            Thread t1 = new Thread(DoWork);
            t1.Start();
        }

        private static void DoWork()
        {
            //int numberServers = RemoteServer.serversPossiblePrimary.Count(); //Regist number of server at exception handling         
            bool changed = RemoteServer.changeServer;
            //bool isprimary = RemoteServer.isPrimary;
            int numberServers = RemoteServer.serversPossiblePrimary.Count();
            IAsyncResult result;
            Action action = () =>
            {
                server.ping(PID_init);
                RemoteServer.changeServer = false;
            };

            result = action.BeginInvoke(null, null);

            if (!result.AsyncWaitHandle.WaitOne(TIMEOUT))
            {
                if (!changed && !RemoteServer.changeServer && numberServers == RemoteServer.serversPossiblePrimary.Count()) //Check if this exception have been handling by comparing number of server
                {
                    RemoteServer.serversPossiblePrimary.RemoveAt(0);//TODO NECESSARIO?
                    RemoteServer.changeServer = true;
                    Console.WriteLine("Primary Server failed!:" + RemoteServer.serversPossiblePrimary.Count().ToString());
                    if (RemoteServer.serversPossiblePrimary.Count() == 0)
                    {//is primary
                        RemoteServer.isPrimary = true;
                        changeServerToNewPrimary();
                        if (RemoteServer.gameHistory.Count() == 0)//game havent started
                            RemoteServer.gameStarted = false;
                        else
                        {
                            game = new Game(RemoteServer.gameHistory.Last().Value);
                            RemoteServer.round = RemoteServer.gameHistory.Last().Key;
                            RemoteServer.gameStarted = true;
                        }
                        waitToBePrimary.Set();
                    }
                    else
                    {//is secondary                        
                        RemoteServer.isPrimary = false;
                        registAsSecundary();
                        //serverSet(RemoteServer.isPrimary);
                    }
                }
            }
        }

        private static void changeServerToNewPrimary()
        {
            IAsyncResult result;
            Action action = () =>
            {
                server.ChangeServer(PID_init);
            };

            result = action.BeginInvoke(null, null);
            result.AsyncWaitHandle.WaitOne(TIMEOUT);
        }

        private static void sendGameStateToClients(Game pacman, int round, GameState gs)
        {
            foreach (var x in RemoteServer.clients.ToList())
            {
                Thread t1 = new Thread(() =>
                {
                    if (!x.Value.Item2)
                    {
                        return;
                    }

                    if (InjectDelay(x.Key))
                    {
                        Thread.Sleep(TIMEDELAY);
                        if (!sendGameState(gs, x.Key))
                        {
                            lock (RemoteServer.clients)
                            {
                                if (RemoteServer.clients[x.Key].Item2)
                                {
                                    Console.WriteLine("Player {0} disconnected", x.Key);
                                    RemoteServer.clients[x.Key] = new Tuple<IClientApi, bool>(RemoteServer.clients[x.Key].Item1, false);
                                }
                            }
                        }
                        else
                        {
                            lock (RemoteServer.clients)
                            {
                                RemoteServer.clients[x.Key] = new Tuple<IClientApi, bool>(RemoteServer.clients[x.Key].Item1, true);
                            }
                        }
                    }
                    else
                    {
                        if (!sendGameState(gs, x.Key))
                        {
                            lock (RemoteServer.clients)
                            {
                                if (RemoteServer.clients[x.Key].Item2)
                                {
                                    Console.WriteLine("Player {0} disconnected", x.Key);
                                    RemoteServer.clients[x.Key] = new Tuple<IClientApi, bool>(RemoteServer.clients[x.Key].Item1, false);
                                }
                            }
                        }
                        else
                        {
                            lock (RemoteServer.clients)
                            {
                                RemoteServer.clients[x.Key] = new Tuple<IClientApi, bool>(RemoteServer.clients[x.Key].Item1, true);
                            }
                        }
                    }
                });
                t1.Start();
            };


            pacman.updateDirection();
            lock (RemoteServer.gameHistory)
            {
                if (!RemoteServer.gameHistory.ContainsKey(round))
                    RemoteServer.gameHistory.Add(round, gs);
            }
        }

        private static bool sendGameState(GameState gs, string pid)
        {
            try
            {
                RemoteServer.clients[pid].Item1.StateRound(gs);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool sendGameStateToServer(GameState gs, string pid, int tries)
        {
            try
            {
                IAsyncResult result;
                Action action = () =>
                {
                    RemoteServer.servers[pid].Item1.sendUpdates(gs, RemoteServer.gameStarted);
                };

                result = action.BeginInvoke(null, null);
                if (!result.AsyncWaitHandle.WaitOne(TIMEOUT))
                {
                    if (tries == 3)
                        return false;
                    return sendGameStateToServer(gs, pid, ++tries);
                }

                else
                    return true;
            }
            catch (Exception)
            {
                return false;
            }

        }

        private static void RegistRemoteObject(string url)
        {
            string[] urlSplitted = url.Split('/', ':');
            try
            {
                TcpChannel channel = new TcpChannel(Int32.Parse(urlSplitted[4]));
                ChannelServices.RegisterChannel(channel, false);
                RemotingConfiguration.RegisterWellKnownServiceType(
                    typeof(RemoteServer),
                    urlSplitted[5],
                    WellKnownObjectMode.Singleton);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error regist Remote Server! Exception: ");
                Console.WriteLine(e.ToString());
            }
        }

        private static bool InjectDelay(string PID)
        {
            if (RemoteServer.delayPID.Contains(PID))
            {
                return true;
            }
            return false;
        }


        private static bool changeServer(string pid)
        {
            try
            {
                RemoteServer.clients[pid].Item1.ReplacePrimaryServer(PID_init, URL_init);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void changeServerOnClients()
        {
            foreach (var client in RemoteServer.clients.ToList())
            {
                Thread t1 = new Thread(() =>
                {
                    if (InjectDelay(client.Key))
                    {
                        Thread.Sleep(TIMEDELAY);
                        if (!changeServer(client.Key))
                        {
                            Console.WriteLine("Player {0} disconnected", client.Key);
                            lock (RemoteServer.clients)
                            {
                                RemoteServer.clients[client.Key] = new Tuple<IClientApi, bool>(RemoteServer.clients[client.Key].Item1, false);
                            }
                        }
                        else
                        {
                            lock (RemoteServer.clients)
                            {
                                RemoteServer.clients[client.Key] = new Tuple<IClientApi, bool>(RemoteServer.clients[client.Key].Item1, true);
                            }
                        }
                    }
                    else
                    {
                        if (!changeServer(client.Key))
                        {
                            Console.WriteLine("Player {0} disconnected", client.Key);
                            lock (RemoteServer.clients)
                            {
                                RemoteServer.clients[client.Key] = new Tuple<IClientApi, bool>(RemoteServer.clients[client.Key].Item1, false);
                            }
                        }
                        else
                        {
                            lock (RemoteServer.clients)
                            {
                                RemoteServer.clients[client.Key] = new Tuple<IClientApi, bool>(RemoteServer.clients[client.Key].Item1, true);
                            }
                        }
                    }
                });
                t1.Start();
            }
        }
    }
}



  