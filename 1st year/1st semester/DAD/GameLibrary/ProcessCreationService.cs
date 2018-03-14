using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessCreationService
{
    public interface IPCS
    {
        bool StartClient(String PID, String CLIENT_URL, int MSEC_PER_ROUND, int NUM_PLAYERS, string url_server, string filename);
        bool StartServer(String PID, String SERVER_URL, int MSEC_PER_ROUND, int NUM_PLAYERS, String serversToConnect);
    }
}
