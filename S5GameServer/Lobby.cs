using S5GameServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S5GameServer
{
    class Lobby
    {
        public string Name { get; protected set; }
        public int LobbyID { get; protected set; }

        public List<PlayerAccount> Players = new List<PlayerAccount>();

        protected Lobby(string name, int id)
        {
            Name = name;
            LobbyID = id;
        }

        public DNodeList LobbyInfo
        {
            get { return new DNodeList { "0", Name, LobbyID, Constants.LOBBY_SERVER_ID, "0", "2068", "0", "", "SETTLERSHOK", "SETTLERSHOK", new byte[0], "0", "500", "1" }; }
        }

        public event EventHandler<Message> LobbyMessage;

        public void Broadcast(Message msg)
        {
            LobbyMessage?.Invoke(this, msg);
        }
        

        #region static

        static int nextLobbyID = 870;

        static Dictionary<int, Lobby> lobbies = new Dictionary<int, Lobby>();

        public static IEnumerable<Lobby> AllLobbies { get { return lobbies.Values; } }

        public static Lobby Get(int id)
        {
            Lobby lb = null;
            lobbies.TryGetValue(id, out lb);
            return lb;
        }

        public static void AddLobbies(params string[] names)
        {
            foreach (var name in names)
            {
                lobbies.Add(nextLobbyID, new Lobby(name, nextLobbyID));
                nextLobbyID++;
            }
        }

        #endregion
    }

    class GameRoom
    {
        public string Name { get; protected set; }
        public int ID { get; protected set; }

        public List<PlayerAccount> Players = new List<PlayerAccount>();
        public PlayerAccount Host { get; protected set; }
        public byte[] GameInformation { get; protected set; }
        
        public DNodeList RoomInfo
        {
            get { return new DNodeList {  }; }
        }

        public event EventHandler<Message> RoomMessage;

        public void Broadcast(Message msg)
        {
            RoomMessage?.Invoke(this, msg);
        }
        
    }
}
