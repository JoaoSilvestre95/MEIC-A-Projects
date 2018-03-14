using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;
using ProcessCreationService;
using System.Runtime.Remoting;
using System.Threading;

namespace PuppetMaster
{
    class Program
    {
        private static char[] delimiterChars = { ' ' };

        static void Main(string[] args)
        {
            Dictionary<string, string> pid_Client = new Dictionary<string, string>(); //PID,PCS_URL
            Dictionary<string, string> pid_Server = new Dictionary<string, string>(); //PID,PCS_URL
            Dictionary<string, Tuple<bool,string>> url_Client = new Dictionary<string, Tuple<bool, string>>(); //PID,CLIENT_URL
            Dictionary<string, Tuple<bool, string>> url_Servers = new Dictionary<string, Tuple<bool, string>>(); //PID,SERVER_URL


            TcpChannel channel = new TcpChannel();
            ChannelServices.RegisterChannel(channel, false);

            void process(string[] input)
            {
                IPCS pcs = null;
                switch (input[0])
                {
                    case "StartClient":
                        if (url_Servers.Count == 0)
                        {
                            Console.WriteLine("Need to start one server first!");
                            break;
                        }
                        //StartClient PID PCS_URL CLIENT_URL MSEC_PER_ROUND NUM_PLAYERS [filename]  
                        if (input.Count() > 7)
                        {
                            Console.WriteLine("Invalid number of arguments to start server, expected 5/6 delimited by a space");
                            break;
                        }
                        if (!pid_Client.ContainsKey(input[1]))
                        {
                            try
                            {
                                pcs = (IPCS)Activator.GetObject(typeof(IPCS), input[2]);
                            }
                            catch (RemotingException)
                            {
                                Console.WriteLine("Problems getting Object IPCS interface from: " + input[2]);
                                break;

                            }
                            catch (ArgumentNullException)
                            {
                                Console.WriteLine("Url: " + input[2] + "or type is null.");
                                break;

                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Problems trying to get PCS, exception: ");
                                Console.WriteLine(e.ToString());
                                break;
                            }

                            string urlToSend = "";
                            foreach (Tuple<bool,string> url in url_Servers.Values)
                            {
                                if (!url.Item2.Equals(input[2]))
                                {
                                    urlToSend += url.Item2 + ";";
                                }
                            }
                            if (!url_Client.ContainsKey(input[1]))
                            {

                                if (input.Count() == 6)
                                    if (pcs.StartClient(input[1], input[3], Int32.Parse(input[4]), Int32.Parse(input[5]), urlToSend, "noFile"))
                                    {
                                        pid_Client.Add(input[1], input[2]);
                                        url_Client.Add(input[1], new Tuple<bool, string>(true, input[3]));
                                        Console.WriteLine("Client with PID: " + input[1] + " started.");
                                    }
                                if (input.Count() == 7)
                                    if (pcs.StartClient(input[1], input[3], Int32.Parse(input[4]), Int32.Parse(input[5]), urlToSend, input[6]))
                                    {
                                        pid_Client.Add(input[1], input[2]);
                                        url_Client.Add(input[1], new Tuple<bool, string>(true, input[3]));
                                        Console.WriteLine("Client with PID: " + input[1] + " started.");
                                    }
                            }
                            if (url_Client.ContainsKey(input[1]) && !url_Client[input[1]].Item1)
                            {
                                if (input.Count() == 6)
                                    if (pcs.StartClient(input[1], input[3], Int32.Parse(input[4]), Int32.Parse(input[5]), urlToSend, "noFile"))
                                    {
                                        pid_Client.Add(input[1], input[2]);
                                        url_Client[input[1]] = new Tuple<bool, string>(true, input[3]);
                                        Console.WriteLine("Client with PID: " + input[1] + " started.");
                                    }
                                if (input.Count() == 7)
                                    if (pcs.StartClient(input[1], input[3], Int32.Parse(input[4]), Int32.Parse(input[5]), urlToSend, input[6]))
                                    {
                                        pid_Client.Add(input[1], input[2]);
                                        url_Client[input[1]] = new Tuple<bool, string>(true, input[3]);
                                        Console.WriteLine("Client with PID: " + input[1] + " started.");
                                    }
                            }
                        }
                        else
                        {
                            Console.WriteLine("PID already exists!");
                        }
                        break;
                    case "StartServer":
                        //StartServer PID PCS_URL SERVER_URL MSEC_PER_ROUND NUM_PLAYERS  
                        if (input.Count() > 6)
                        {
                            Console.WriteLine("Invalid number of arguments to start server, expected 5 delimited by a space");
                            break;
                        }
                        if (!pid_Server.ContainsKey(input[1]))
                        {
                            if (url_Servers.Count == 0)
                            {
                                try
                                {
                                    pcs = (IPCS)Activator.GetObject(typeof(IPCS), input[2]);
                                }
                                catch (RemotingException)
                                {
                                    Console.WriteLine("Problems getting Object IPCS interface from: " + input[2]);
                                    break;

                                }
                                catch (ArgumentNullException)
                                {
                                    Console.WriteLine("Url: " + input[2] + "or type is null.");
                                    break;

                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Problems trying to get PCS, exception: ");
                                    Console.WriteLine(e.ToString());
                                    break;
                                }
                                    if (!url_Servers.ContainsKey(input[1]))
                                    {
                                        if (pcs.StartServer(input[1], input[3], Int32.Parse(input[4]), Int32.Parse(input[5]), ""))
                                        {
                                            pid_Server.Add(input[1], input[2]);
                                            url_Servers.Add(input[1], new Tuple<bool, string>(true, input[3]));
                                            Console.WriteLine("Server with PID: " + input[1] + " started.");
                                        }
                                    }
                                    if (url_Servers.ContainsKey(input[1]) && !url_Servers[input[1]].Item1)
                                    {
                                        if (pcs.StartServer(input[1], input[3], Int32.Parse(input[4]), Int32.Parse(input[5]), ""))
                                        {
                                            pid_Server.Add(input[1], input[2]);
                                            url_Servers[input[1]] = new Tuple<bool, string>(true, input[3]);
                                            Console.WriteLine("Server with PID: " + input[1] + " started.");
                                        }
                                    }
                            }
                            else
                            {
                                pcs = (IPCS)Activator.GetObject(typeof(IPCS), input[2]);
                                string urlToSend = "";
                                foreach(Tuple<bool,string> url in url_Servers.Values)
                                {
                                    if (!url.Item2.Equals(input[2]))
                                    {
                                        urlToSend += url.Item2 + ";";
                                    }
                                }
                                if (!url_Servers.ContainsKey(input[1]))
                                {
                                    if (pcs.StartServer(input[1], input[3], Int32.Parse(input[4]), Int32.Parse(input[5]), urlToSend))
                                    {
                                        pid_Server.Add(input[1], input[2]);
                                        url_Servers.Add(input[1], new Tuple<bool, string>(true, input[3]));
                                        Console.WriteLine("Server with PID: " + input[1] + " started.");
                                    }
                                }
                                if (url_Servers.ContainsKey(input[1]) && !url_Servers[input[1]].Item1)
                                {
                                    if (pcs.StartServer(input[1], input[3], Int32.Parse(input[4]), Int32.Parse(input[5]), urlToSend))
                                    {
                                        pid_Server.Add(input[1], input[2]);
                                        url_Servers[input[1]] = new Tuple<bool, string>(true, input[3]);
                                        Console.WriteLine("Server with PID: " + input[1] + " started.");
                                    }
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("PID already exists!");
                        }
                        break;
                    case "GlobalStatus":
                        Parallel.ForEach(url_Servers, (s) =>
                         {
                             try
                             {
                                 GameApi.IServerApi server = (GameApi.IServerApi)Activator.GetObject(
                                        typeof(GameApi.IServerApi), s.Value.Item2);
                                 Console.WriteLine(server.globalStatus());
                             }
                             catch
                             {
                                 Console.WriteLine("Not Connected to Server: " + s.Key);
                             }
                             Console.WriteLine();
                         });
                            Console.WriteLine("CLIENTS");
                            Console.WriteLine();

                        Parallel.ForEach(url_Client, (c) =>
                        {
                            try
                            {
                                GameApi.IClientApi client = (GameApi.IClientApi)Activator.GetObject(
                                            typeof(GameApi.IClientApi), c.Value.Item2);
                                Console.WriteLine(client.globalStatus());
                            }

                            catch
                            {
                                Console.WriteLine("Client with PID: " + c.Key + " crashed");
                            }
                            Console.WriteLine();
                        });
                        break;
                    case "Crash":
                        if (pid_Server.ContainsKey(input[1]))
                        {
                            try
                            {
                                GameApi.IServerApi server = (GameApi.IServerApi)Activator.GetObject(
                                   typeof(GameApi.IServerApi), url_Servers[input[1]].Item2);
                                if (server.crash()) Console.WriteLine("Problems trying to crash: " + input[1]);
                            }
                            catch
                            {
                                pid_Server.Remove(input[1]);
                                url_Servers[input[1]] = new Tuple<bool, string>(false,"");
                                Console.WriteLine("Server with PID: " + input[1] + " crashed");
                            }

                        }
                        else if (pid_Client.ContainsKey(input[1]))
                        {
                            try
                            {
                                GameApi.IClientApi client = (GameApi.IClientApi)Activator.GetObject(
                                            typeof(GameApi.IClientApi), url_Client[input[1]].Item2);
                                if (client.crash()) Console.WriteLine("Problems trying to crash: " + input[1]);
                            }

                            catch
                            {
                                pid_Client.Remove(input[1]);
                                url_Client[input[1]] = new Tuple<bool, string>(false,"");
                                Console.WriteLine("Client with PID: " + input[1] + " crashed");
                            }
                        }
                        else
                            Console.WriteLine("PID do not exists!");
                        break;
                    case "Freeze":
                        if (pid_Server.ContainsKey(input[1]))
                        {
                            GameApi.IServerApi server = (GameApi.IServerApi)Activator.GetObject(
                                    typeof(GameApi.IServerApi), url_Servers[input[1]].Item2);
                            server.freeze(true);

                        }
                        else if (pid_Client.ContainsKey(input[1]))
                        {
                            GameApi.IClientApi client = (GameApi.IClientApi)Activator.GetObject(
                                            typeof(GameApi.IClientApi), url_Client[input[1]].Item2);
                            client.freeze(true);
                        }
                        else
                            Console.WriteLine("PID do not exists!");
                        break;
                    case "Unfreeze":
                        if (pid_Server.ContainsKey(input[1]))
                        {
                            GameApi.IServerApi server = (GameApi.IServerApi)Activator.GetObject(
                                typeof(GameApi.IServerApi), url_Servers[input[1]].Item2);
                            server.freeze(false);
                        }
                        else if (pid_Client.ContainsKey(input[1]))
                        {
                            GameApi.IClientApi client = (GameApi.IClientApi)Activator.GetObject(
                                typeof(GameApi.IClientApi), url_Client[input[1]].Item2);
                            client.freeze(false);
                        }
                        else
                            Console.WriteLine("PID do not exists!");
                        break;
                    case "InjectDelay":
                        if (pid_Server.ContainsKey(input[1]))
                        {
                            GameApi.IServerApi server = (GameApi.IServerApi)Activator.GetObject(
                                typeof(GameApi.IServerApi), url_Servers[input[1]].Item2);
                            if(server.InjectDelay(input[1], input[2]))
                            {
                                Console.WriteLine("Delay injected between Server " + input[1] + " and PID: " + input[2] + " added");
                            }
                            else
                            {
                                Console.WriteLine("Delay injected between Server " + input[1] + " and PID: " + input[2] + "wasn't possible");
                            }
                        }
                        else if (pid_Client.ContainsKey(input[1]))
                        {
                            GameApi.IClientApi client = (GameApi.IClientApi)Activator.GetObject(
                                typeof(GameApi.IClientApi), url_Client[input[1]].Item2);
                            if (client.InjectDelay(pid_Server.Keys.First(), input[2]))
                            {
                                Console.WriteLine("Delay injected between Server " + input[1] + " and PID: " + input[2] + " added");
                            }
                            else
                            {
                                Console.WriteLine("Delay injected between Server " + input[1] + " and PID: " + input[2] + "wasn't possible");
                            }
                        }
                        break;
                    case "LocalState":
                        String state = "";
                        if (input.Count() > 3)
                        {
                            Console.WriteLine("Invalid number of arguments to LocalState, expected 2 delimited by a space");
                            break;
                        }                            
                        if (pid_Server.ContainsKey(input[1]))
                        {
                            GameApi.IServerApi server = (GameApi.IServerApi)Activator.GetObject(
                                typeof(GameApi.IServerApi), url_Servers[input[1]].Item2);
                            state = server.LocalState(Int32.Parse(input[2]));
                        }
                        else if (pid_Client.ContainsKey(input[1]))
                        {
                            GameApi.IClientApi client = (GameApi.IClientApi)Activator.GetObject(
                                typeof(GameApi.IClientApi), url_Client[input[1]].Item2);
                            state = client.LocalState(Int32.Parse(input[2]));
                        }
                        if (!state.Equals(""))
                        {
                            Console.Write(state);
                            System.IO.File.WriteAllText(Environment.CurrentDirectory + @"\..\..\LocalState-" + input[1] + "-" + input[2] + ".txt", state);
                        }
                        else
                        {
                            Console.WriteLine("Local state for that PID does not exist");
                        }
                        break;
                    case "Wait":
                        System.Threading.Thread.Sleep(Int32.Parse(input[1]));
                        break;
                    default:
                        Console.WriteLine("Command not allowed");
                        break;
                }

            }

            for (;;)
            {
                Console.Write("-> ");
                String command = Console.ReadLine();
                String[] words = command.Split(delimiterChars);

                if (words[0].Equals("ReadFromScript"))
                {
                    Thread t1 = new Thread(() => {
                        String[] commands = System.IO.File.ReadAllLines(words[1]);
                        foreach(string s in commands)
                        {
                            process(s.Split(delimiterChars));
                        }
                    });
                    t1.Start();
                }
                else
                {
                    process(words);
                }
                
            }
        }
    }
}
