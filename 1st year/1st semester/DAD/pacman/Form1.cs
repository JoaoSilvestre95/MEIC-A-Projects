using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Net.Sockets;
using ProcessCreationService;
using GameApi;
using System.Threading;
using RoundState;

namespace pacman {//TODO Restore chat after pacman client crash

    delegate void MessageDelegate(String pid, Dictionary<String, int> vector, String s);
    delegate void changeServer(String url);
    delegate void updateDelegate();
    delegate void startDelegate();


    public partial class Form1 : Form {
        IServerApi server;
        public int round = -1;
        int TIMEDELAY = 1000;
        Dictionary<int, string> commandsDic = new Dictionary<int, string>();

        int TIMEMSG = 500;
        AutoResetEvent autoEvent;
        System.Threading.Timer msgTimer;
        //--------senderPid--------sender_VC----------msg---//
        List<Tuple<String, Dictionary<string, int>, String>> pendingMessages;
        public static List<String> url_servers_List = new List<string>();
        public static String PID_init = "";
        public static String CLIENT_URL = "";
        public static Dictionary<string, int> vector_clock = new Dictionary<string, int>();
        int MSEC_PER_ROUND = 0;
        int NUM_PLAYERS = 0;
        string urlFile = "";
        Dictionary<string, PictureBox> pacmanPicBox = new Dictionary<string, PictureBox>();

        private static System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        private static bool[] keys_down;
        private static Keys[] key_props;

        public bool ScreenStarted = false;

        public Form1(String PID, String CLIENT_URLs, int MSEC_PER_ROUND, int NUM_PLAYERS, string url_server, string filename) {
            this.MSEC_PER_ROUND = MSEC_PER_ROUND - 5;
            this.NUM_PLAYERS = NUM_PLAYERS;
            InitializeComponent();
            urlFile = filename;
            RemoteClient.form = this;
            label2.Visible = false;
            PID_init = PID;
            CLIENT_URL = CLIENT_URLs;

            url_servers_List = url_server.Split(';').Where(w => w != "").ToList();
            timer.Interval = this.MSEC_PER_ROUND;
            timer.Tick += tick;

            keys_down = new bool[4];
            key_props = new[] { Keys.Left, Keys.Right, Keys.Up, Keys.Down };

            KeyDown += key_down_event;
            KeyUp += key_up_event;

            if (!urlFile.Equals("noFile"))
            {
                readfile(urlFile);
            }
            

            pendingMessages = new List<Tuple<String, Dictionary<string, int>, String>>();
            autoEvent = new AutoResetEvent(false);
            msgTimer = new System.Threading.Timer(processMessages, autoEvent, 0, TIMEMSG);

            PublishClient(CLIENT_URLs);
            foreach (string url in url_servers_List)
            {
                if (RegistToServer(CLIENT_URLs, url))
                    break;
            }

            if (server == null)
                Environment.Exit(0);

        }

        private void tick(Object source, EventArgs e)
        {
            // Do this every timing interval.
            try
            {
                Thread t4 = new Thread(() =>
                {
                    int roundThread = round;
                    if (InjectDelay(RemoteClient.serverPID))
                    {
                        Thread.Sleep(TIMEDELAY);
                    }
                    if (keys_down[0])
                    {
                        server.SendMovement(PID_init, roundThread, "LEFT");
                    }
                    else if (keys_down[1])
                    {
                        server.SendMovement(PID_init, roundThread, "RIGHT");
                    }
                    else if (keys_down[2])
                    {
                        server.SendMovement(PID_init, roundThread, "UP");
                    }
                    else if (keys_down[3])
                    {
                        server.SendMovement(PID_init, roundThread, "DOWN");
                    }
                });
                t4.Start();
            }
            catch (Exception)
            {

            }

        }

        private void key_down_event(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == key_props[0])
                keys_down[0] = true;
            else if (e.KeyCode == key_props[1])
                keys_down[1] = true;
            else if (e.KeyCode == key_props[2])
                keys_down[2] = true;
            else if (e.KeyCode == key_props[3])
                keys_down[3] = true;
        }

        private void key_up_event(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == key_props[0])
                keys_down[0] = false;
            else if (e.KeyCode == key_props[1])
                keys_down[1] = false;
            else if (e.KeyCode == key_props[2])
                keys_down[2] = false;
            else if (e.KeyCode == key_props[3])
                keys_down[3] = false;
        }

        private void updatecoins()
        {
            int i = 1;
            int x = 8;
            int y = 40;
            foreach (Control pb in this.Controls)
            {
                // checking if the player hits the wall or the ghost, then game is over
                if (pb is PictureBox && pb.Tag == "coin")
                {
                    if (((x == 88 || x == 248) && y < 160))
                    {
                        y = 160;
                            // WALLS
                        }

                    else if (((x == 128 || x == 288) && y > 200))
                    {
                        x += 40;
                        y = 40;
                    }

                    pb.Name = i.ToString();
                    pb.Location = new System.Drawing.Point(x, y);

                    if (y == 320)
                    {
                        x += 40;
                        y = 40;
                    }
                    else { y += 40; }
                    i++;
                }
            }
        }

        public void readfile(string url)
        {
            String[] commands = System.IO.File.ReadAllLines(@url);

            foreach (string line in commands)
            {
                string[] split = line.Split(',');
                commandsDic.Add(Int32.Parse(split[0]), split[1]);
            }
        }
        private void keyisdown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Enter)
            {
                tbMsg.Enabled = true;
                tbMsg.Focus();
                return;
            }
        }

       private void updateGhostsPositions(GameState gs)
        {
            redGhost.Location = new Point(gs.getXRedGhost(), gs.getYRedGhost());
            yellowGhost.Location = new Point(gs.getXYellowGhost(), gs.getYYellowGhost());
            pinkGhost.Location = new Point(gs.getXPinkGhost(), gs.getYPinkGhost());
        }

        private void updateScore(GameState gs)
        {
            label1.Text = "Score: " + gs.getScore(PID_init);
        }

        private void updatePacmansPositions(GameState gs)
        {
            Parallel.ForEach(pacmanPicBox, (x) => {
                x.Value.Location = new Point(gs.getXPacman(x.Key), gs.getYPacman(x.Key));
                int direction = gs.getMovableGameObjectDirection(x.Key);

                if (direction == 0)
                {
                    x.Value.Image = Properties.Resources.Left;
                }
                else if (direction == 1)
                {
                    x.Value.Image = Properties.Resources.Right;
                }
                else if (direction == 2)
                {
                    x.Value.Image = Properties.Resources.Up;
                }
                else if (direction == 3)
                {
                    x.Value.Image = Properties.Resources.down;
                }
            });
        }

        private void checkCoins(GameState gs)
        {
            foreach (Control x in this.Controls)
            {
                if (x is PictureBox && x.Tag == "coin")
                {
                    if (!gs.getVisibleCoin(Int32.Parse(x.Name)))
                    {
                        this.Controls.Remove(x);
                    }
                }
            }
        }

        private void tbMsg_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Enter ) {
                if (!tbMsg.Text.Equals(""))
                {
                    String msg = PID_init + " : " + tbMsg.Text;
                    addMessageToTextBox(msg);
                    Dictionary<string, int> sender_clock;
                    lock (vector_clock)
                    {
                       sender_clock = new Dictionary<string, int>(vector_clock);
                    }
                    Task.Run(() => { sendMessageToAllClients(msg, sender_clock);});
;                    
                    lock (vector_clock)
                    {
                        vector_clock[PID_init] += 1;
                    }                    
                }
                tbMsg.Clear();
                tbMsg.Enabled = false;
                this.Focus();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        public void StartScreen()
        {
            if (pacmanPicBox.Count == 0)
            {
                this.SuspendLayout();
                for (int a = 0; a < NUM_PLAYERS; a++)
                {
                    PictureBox i = new System.Windows.Forms.PictureBox();
                    i.BackColor = System.Drawing.Color.Transparent;
                    i.Image = global::pacman.Properties.Resources.Left;
                    i.Location = new System.Drawing.Point(16, 77);
                    i.Margin = new System.Windows.Forms.Padding(0);
                    i.Name = "pacman";
                    i.Size = new System.Drawing.Size(50, 48);
                    i.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
                    i.TabIndex = 4;
                    i.TabStop = false;
                    this.Controls.Add(i);

                    this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);                    
                    pacmanPicBox.Add(RemoteClient.gameHistory.Last().Value.getPacmanPID(a), i);                    
                }
                this.tbChat.Size = new System.Drawing.Size(196, 487);
                this.ResumeLayout(false);
                updatecoins();
            }
            timer.Start();
            ScreenStarted = true;
        }


        public void ClientGameLoop()
        {
            SpinWait.SpinUntil(()=> ScreenStarted == true);
            GameState stateRound = RemoteClient.updateRound;
            new Thread(() => updateGhostsPositions(stateRound))
            {
                Priority = ThreadPriority.Highest
            }.Start();
            Task.Run(() => {
                checkCoins(stateRound);
                updateScore(stateRound);
            });
            new Thread(() => updatePacmansPositions(stateRound)).Start();
            checkWinLost(stateRound);
            round = stateRound.getRound() + 1;

            if (!urlFile.Equals("noFile") && commandsDic.ContainsKey(round))
            {
                Thread t5 = new Thread(() =>
                {
                    try
                    {
                        int roundThread = round;
                        if (InjectDelay(RemoteClient.serverPID))
                        {
                            Thread.Sleep(TIMEDELAY);
                        }
                        server.SendMovement(PID_init, roundThread, commandsDic[round]);
                    }
                    catch (Exception) { }
                });
                t5.Start();
            }
        }

        private void checkWinLost(GameState gs)
        {
            if (gs.getState(PID_init).Equals(1) && label2.Visible==false)
            {
                label2.Text = "GAME WON";
                label2.Visible = true;
            }
            else if (gs.getState(PID_init).Equals(-1) && label2.Visible == false)
            {
                label2.Text = "GAME OVER";
                label2.Visible = true;
            }
        }

        private void pacman_Click(object sender, EventArgs e)
        {

        }

        // METHODS

        private void PublishClient(String url)
        {
            string[] a = url.Split(':','/');

            try
            {
                TcpChannel channel = new TcpChannel(Int32.Parse(a[4]));
                ChannelServices.RegisterChannel(channel, false);

                RemotingConfiguration.RegisterWellKnownServiceType(
                    typeof(RemoteClient),
                    a[5],
                    WellKnownObjectMode.Singleton);
            }
            catch (RemotingException e)
            {
                Console.WriteLine("Problems Publish Client: " + url);
                Console.WriteLine(e.ToString());

            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine("Problems Publish Client: " + url + " arguments null.");
                Console.WriteLine(e.ToString());

            }
            catch (Exception e)
            {
                Console.WriteLine("Problems trying to regist client: " + url + " exception: ");
                Console.WriteLine(e.ToString());
            }
        }

        //conect to server and create a remote object of this client
        private bool RegistToServer(String url, String serverurl)
        {
            try
            {
                connectServer(serverurl);
                if (server.RegisterClient(PID_init, url)) return true;
            }
            catch (SocketException)
            {
                System.Console.WriteLine("Could not locate server");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return false;
        }

        public void addMessageToTextBox(String msg)
        {
            tbChat.Text += "\r\n" + msg;
        }

        public void receiveMessageFromClient(String senderPid, Dictionary<String, int> senderClock, String msg)
        {
            pendingMessages.Add(new Tuple<String, Dictionary<string, int>, String>(senderPid, senderClock, msg));
            msgTimer.Change(0, TIMEMSG);            
        }

        public void processMessages(object x) {
            if (!RemoteClient.isFroozen)
            {
                List<Tuple<String, Dictionary<string, int>, String>> removeMessage = new List<Tuple<String, Dictionary<string, int>, String>>();
                foreach (var message in pendingMessages) //iterates pending messages
                {
                    foreach (KeyValuePair<string,int> sender_vector in message.Item2) //iterates vector message
                    {
                        if (vector_clock[sender_vector.Key] >= sender_vector.Value) //See if all values are higher than local vector
                        {                            
                            if (removeMessage.Count == 0) //if it can process message, have to be the earliest deliver
                                removeMessage.Add(message);
                        }
                        else
                        {
                            removeMessage.Clear(); //cant process message, still delayed
                            break;
                        }
                    }
                    if (removeMessage.Count == 1)
                        break;
                }

                foreach (var rcvMsg in removeMessage)
                { //Writes the message, and removes from the pendent message set
                    addMessageToTextBox(rcvMsg.Item3);
                    vector_clock[rcvMsg.Item1] += 1;
                    pendingMessages.Remove(rcvMsg);
                }
                removeMessage.Clear();
            }
            if (pendingMessages.Count() == 0)            
                msgTimer.Change(Timeout.Infinite, Timeout.Infinite);              
            
        }

        private void sendMessageToAllClients(String msg, Dictionary<string,int> sender_clock)
        {            
            WaitHandle[] waitHandles = new WaitHandle[RemoteClient.clientsObject.Count];
            int i = 0;
            foreach (var client in RemoteClient.clientsObject)
            {
                var handle = new EventWaitHandle(false, EventResetMode.ManualReset);
                Task t1;
                if (InjectDelay(client.Key))
                {
                    t1 = new Task(async () => {
                        await Task.Delay(TIMEDELAY);
                        client.Value.receiveMessage(PID_init, sender_clock, msg);
                        handle.Set();
                    });
                }
                else
                {
                    t1 = new Task(() => {
                        client.Value.receiveMessage(PID_init, sender_clock, msg);
                        handle.Set();
                    });
                }
                waitHandles[i] = handle;
                i++;
                t1.Start();
            }
            WaitHandle.WaitAll(waitHandles);      

        }
        private static bool InjectDelay(string PID)
        {
            if (RemoteClient.delayPID.Contains(PID))
            {
                return true;
            }
            return false;
        }

        private void pictureBox5_Click(object sender, EventArgs e)
        {

        }

        private void tbChat_TextChanged(object sender, EventArgs e)
        {
            tbChat.SelectionStart = tbChat.Text.Length;
            tbChat.ScrollToCaret();
        }

        public void connectServer(string url)
        {
            server = (IServerApi)Activator.GetObject(typeof(IServerApi), url);
            Task.Run(() => server.open());
        }
    }

}
