using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Net.Sockets;
using GameApi;
using System.Collections.Generic;
using RoundState;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace pacman
{
    class RemoteClient : MarshalByRefObject, IClientApi
    {
        public static Form1 form;
        public static bool isFroozen = false;
        public static GameState updateRound = null;
        public static Dictionary<String,IClientApi> clientsObject = new Dictionary<string, IClientApi>();
        public static Dictionary<int, GameState> gameHistory = new Dictionary<int, GameState>();
        public static List<String> delayPID = new List<string>();
        public static bool gameStarted = false;
        public static string serverPID;


        public RemoteClient()
        {
            Console.WriteLine("Client up");
        }

        //recebe todos os urls e guarda a lista dos urls
        public void ReceiveClientsUrls(Dictionary<String,String> clientsURLS)
        {
            foreach (var client in clientsURLS)
            {
                try
                {
                    if (client.Value != Form1.CLIENT_URL)
                    {
                        IClientApi cli = (IClientApi)Activator.GetObject(typeof(IServerApi), client.Value);
                        if (!clientsObject.ContainsKey(client.Key))
                            clientsObject.Add(client.Key, cli);
                        Task.Run(() => cli.open());
                    }
                    if (!Form1.vector_clock.ContainsKey(client.Key))
                        Form1.vector_clock.Add(client.Key, 0);
                }
                catch (Exception)
                {
                }
            }
        }

        public void receiveMessage(String pid, Dictionary<String, int> vector, String msg)
        {
            // thread-safe access to form
            form.Invoke(new MessageDelegate(form.receiveMessageFromClient), pid, vector, msg);
        }

        public bool InjectDelay(string server, string PID)
        {
            serverPID = server;
            if (clientsObject.ContainsKey(PID) || serverPID.Equals(PID))
            {
                delayPID.Add(PID);
                return true;
            }
            return false;
        }

        public void StateRound(GameState state)
        {   
            updateRound = state;
            lock (gameHistory)
            {
                if (!gameHistory.ContainsKey(state.getRound()))
                    gameHistory.Add(state.getRound(), state);
            }
            if (!gameStarted)
            {               
                ReadytoStart(state);
            }
            if (!isFroozen)
                form.Invoke(new updateDelegate(form.ClientGameLoop));
        }

        public void ReadytoStart(GameState state)
        {
            lock (gameHistory)
            {
                if(!gameHistory.ContainsKey(state.getRound()))
                    gameHistory.Add(state.getRound(), state);
            }
            if (!gameStarted)
                gameStarted = true;
            form.Invoke(new startDelegate(form.StartScreen));
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

        public void freeze(bool freeze)
        {
            //se esta congelado e vem ordem para descongelar entao invocar o metodo para fazer update a ronda
            if(isFroozen && freeze == false)
            {
                isFroozen = freeze;
                form.Invoke(new updateDelegate(form.ClientGameLoop));
            }
            else
                isFroozen = freeze;
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
                Console.WriteLine("Security error was detected, when method crash was invoked");
                Console.WriteLine(se.ToString());
            }
            return false;
        }

        public string globalStatus()
        {
            String status = "Client: " + Form1.PID_init;
            status += Environment.NewLine + "Player Round: " + form.round;

            return status;
        }

        public void ReplacePrimaryServer(string PID, string url)
        {
            serverPID = PID;
            form.Invoke(new changeServer(form.connectServer),url);
        }

        public void open()
        {
        }
    }
}
