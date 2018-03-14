using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoundState;

namespace GameApi
{
    public interface IClientApi
    {
        void open();
        void StateRound(GameState state);
        void ReadytoStart(GameState startState);
        void ReceiveClientsUrls(Dictionary<String,String> clientsURLS);
        void receiveMessage(String pid, Dictionary<string,int> vector_clock, String msg);
        void freeze(bool freeze);
        bool crash();
        string LocalState(int round);
        string globalStatus();
        bool InjectDelay(string server, string PID);
        void ReplacePrimaryServer(string PID, string url);
    }
    public interface IServerApi
    {
        void open();
        bool RegisterClient(String PID, String url);
        void SendMovement(string PID, int roundId, String direction);
        void freeze(bool freeze);
        bool crash();
        string LocalState(int round);
        string globalStatus();
        bool InjectDelay(string server,string PID);
        bool ping(string pid);
        bool registerServer(String PID, String url);
        bool sendUpdates(GameState gs, bool started);
        void sendClientsList(List<KeyValuePair<String, String>> dic);
        void ChangeServer(string PID);
    }
}
