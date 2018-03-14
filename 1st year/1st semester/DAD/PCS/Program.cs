using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;
using ProcessCreationService;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;

namespace PCS
{
    class Program
    {
        static void Main(string[] args)
        {
            CreateChannel();
            System.Console.WriteLine("Press <enter> to terminate PCS...");
            System.Console.ReadLine();
        }

        private static void CreateChannel()
        {
            TcpChannel channel = new TcpChannel(11000);
            ChannelServices.RegisterChannel(channel, false);
            RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(PCSService), "PCS",
                WellKnownObjectMode.Singleton);
        }
    }

    class PCSService : MarshalByRefObject, IPCS
    {
        public bool StartClient(string PID, string CLIENT_URL, int MSEC_PER_ROUND, int NUM_PLAYERS, string url_server, string filename)
        {
            try
            {
                Process.Start(GetLocationProgramToStart("pacman"), PID + " " + CLIENT_URL + " " + MSEC_PER_ROUND.ToString() + " " + NUM_PLAYERS.ToString() + " " + url_server + " " + filename);
                return true;
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("The client process has already been disposed");
                return false;
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("Client's program fileName or arguments is null");
                return false;
            }
            catch (Win32Exception)
            {
                Console.WriteLine("Error opening Client program");
                return false;
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("Client program file not found");
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine("Problems trying to launch client process, exception: ");
                Console.WriteLine(e.ToString());
                return false;
            }
           
        }

        public bool StartServer(string PID, string SERVER_URL, int MSEC_PER_ROUND, int NUM_PLAYERS, String serversToConnect)
        {
            try
            {
                Process.Start(GetLocationProgramToStart("Server"), PID + " " + SERVER_URL + " " + MSEC_PER_ROUND.ToString() + " " + NUM_PLAYERS.ToString() + " " + serversToConnect);
                return true;
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("The server process has already been disposed");
                return false;
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("Server's program fileName or arguments is null");
                return false;
            }
            catch (Win32Exception)
            {
                Console.WriteLine("Error opening Server program");
                return false;
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("Server program file not found");
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine("Problems trying to launch server process, exception: ");
                Console.WriteLine(e.ToString());
                return false;
            }

        }
       

        private string GetLocationProgramToStart(String nspace){
            return Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName + "\\" + nspace + "\\bin\\Debug\\" + nspace + ".exe";
        }
    }
}
